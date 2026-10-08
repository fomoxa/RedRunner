using System.Collections.Generic;
using System.IO;
using Fomoxa.Networking.Messaging;
using Fomoxa.Networking.Objects;
using Fomoxa.Networking.Simulation;
using Fomoxa.Unity;
using Generated;
using RedRunner.Characters;
using RedRunner.Networking;
using RedRunner.Networking.Models;
using RedRunner.TerrainGeneration;
using RedRunner.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedRunner.Editor.Networking
{
    public static class LevelCatalogueExport
    {
        public const string CataloguePath = "Assets/Scripts/RedRunner/Networking/LevelCatalogue.bytes";
        public const string PlayScenePath = "Assets/Scenes/Play.unity";
        public const string PlayerPrefabPath = "Assets/Prefabs/RedRunner.prefab";

        [MenuItem("Tools/RedRunner/Export Level Catalogue")]
        public static void Export()
        {
            File.WriteAllBytes(CataloguePath, Encode(Describe()));
            AssetDatabase.ImportAsset(CataloguePath);
            Debug.Log($"[RedRunner] wrote {CataloguePath}");
        }

        public static byte[] Encode(LevelCatalogue catalogue) => LevelCatalogueNetAdapter.Instance.Encode(catalogue).ToArray();

        public static LevelCatalogue Describe()
        {
            Scene play = SceneManager.GetSceneByPath(PlayScenePath);
            bool opened = !play.isLoaded;
            if (opened)
            {
                play = EditorSceneManager.OpenScene(PlayScenePath, OpenSceneMode.Additive);
            }

            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                NetworkManager manager = Find<NetworkManager>(play);
                NetworkGame game = Find<NetworkGame>(play);
                NetworkLevel level = Find<NetworkLevel>(play);
                TerrainGenerationSettings settings = level.Generator.Settings;
                float tickDelta = 1f / new SerializedObject(manager).FindProperty("tickRate").intValue;
                var catalogue = new LevelCatalogue
                {
                    TickDelta = tickDelta,
                    StartCount = (uint)settings.StartBlocksCount,
                    MiddleCount = (uint)settings.MiddleBlocksCount,
                    EndCount = (uint)settings.EndBlocksCount,
                };
                Describe(settings.StartBlocks, catalogue.StartBlocks, preview, tickDelta);
                Describe(settings.MiddleBlocks, catalogue.MiddleBlocks, preview, tickDelta);
                Describe(settings.EndBlocks, catalogue.EndBlocks, preview, tickDelta);
                catalogue.Player = DescribePlayer(game.SpawnPoint, preview);
                return catalogue;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
                if (opened)
                {
                    EditorSceneManager.CloseScene(play, true);
                }
            }
        }

        private static void Describe(Block[] prefabs, List<CatalogueBlock> into, Scene preview, float tickDelta)
        {
            foreach (Block prefab in prefabs)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, preview);
                try
                {
                    instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    NetworkLevel.PrepareFollowers(instance);
                    Physics2D.SyncTransforms();
                    into.Add(DescribeBlock(prefab, instance, tickDelta));
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }

        private static CatalogueBlock DescribeBlock(Block prefab, GameObject instance, float tickDelta)
        {
            var block = new CatalogueBlock { Width = prefab.Width, Probability = prefab.Probability };
            var sources = new List<Collider2D>();
            var colliders = new List<ColliderDesc2D>();
            NetworkLevel.StaticSources(instance, sources);
            for (int index = 0; index < sources.Count; index++)
            {
                if (index > 0 && sources[index] == sources[index - 1])
                {
                    continue;
                }

                int before = colliders.Count;
                BodyDescriptions.DescribeStatic2D(sources[index], null, colliders);
                for (int piece = before; piece < colliders.Count; piece++)
                {
                    block.Statics.Add(SceneFileGeometry.ToFile(colliders[piece]));
                    block.StaticSurfaces.Add((uint)NetworkLevel.SurfaceOfSource(sources[index]));
                    block.StaticTops.Add(sources[index].bounds.max.y);
                }
            }

            foreach (Rigidbody2D root in NetworkLevel.BodyRoots(instance))
            {
                var bodySources = new List<Collider2D>();
                if (!BodyDescriptions.TryDescribe2D(root.gameObject, out BodyDesc2D desc, bodySources))
                {
                    continue;
                }

                var body = new CatalogueBody { X = desc.Position.X, Y = desc.Position.Y, Body = SceneFileGeometry.ToFile(desc) };
                foreach (Collider2D source in bodySources)
                {
                    body.Surfaces.Add((uint)NetworkLevel.SurfaceOfSource(source));
                    body.Tops.Add(source.bounds.max.y);
                }

                if (root.TryGetComponent(out PathFollower follower))
                {
                    foreach (System.Numerics.Vector3 point in follower.CycleAt(tickDelta))
                    {
                        body.Cycle.Add(point.X);
                        body.Cycle.Add(point.Y);
                        body.Cycle.Add(point.Z);
                    }
                }

                block.Bodies.Add(body);
            }

            return block;
        }

        private static CataloguePlayer DescribePlayer(Vector2 spawn, Scene preview)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
            try
            {
                instance.transform.SetPositionAndRotation(new Vector3(spawn.x, spawn.y, 0f), Quaternion.identity);
                var character = instance.GetComponent<RedCharacter>();
                var player = instance.GetComponent<NetworkPlayer>();
                BoxCollider2D sensor = NetworkPlayer.AddGroundSensor(character);
                var sources = new List<Collider2D>();
                BodyDescriptions.TryDescribe2D(instance, out BodyDesc2D desc, sources);
                var capsule = (CapsuleCollider2D)character.Collider2D;
                RedRunner.Simulation.RunnerSettings motion = character.Settings;
                return new CataloguePlayer
                {
                    Body = SceneFileGeometry.ToFile(desc),
                    SensorIndex = (uint)sources.IndexOf(sensor),
                    BottomOffset = capsule.offset.y - capsule.size.y * 0.5f,
                    RunSpeed = motion.RunSpeed,
                    MaxRunSpeed = motion.MaxRunSpeed,
                    RunSmoothTime = motion.RunSmoothTime,
                    JumpStrength = motion.JumpStrength,
                    RollForce = motion.RollForce,
                    SpawnX = spawn.x,
                    SpawnY = spawn.y,
                    RespawnDelay = player.RespawnDelay,
                    MaxRepeatedTicks = (uint)player.MaxRepeatedTicks,
                };
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static T Find<T>(Scene scene)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            throw new System.InvalidOperationException($"{scene.path} has no {typeof(T).Name}");
        }
    }
}
