using System.Collections.Generic;
using System.Linq;
using Fomoxa.Networking;
using Fomoxa.Networking.Rapier;
using Fomoxa.Networking.Simulation;
using Fomoxa.Unity;
using Fomoxa.Unity.Rapier;
using Generated;
using RedRunner.Collectables;
using RedRunner.Enemies;
using RedRunner.Networking.Models;
using RedRunner.Simulation;
using RedRunner.TerrainGeneration;
using RedRunner.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedRunner.Networking
{
    public sealed class NetworkLevel : NetworkBehaviour, ISurfaces
    {
        private const string GroundName = "Ground";

        [SerializeField]
        private TerrainGenerator m_Generator;

        [SerializeField]
        private TextAsset m_Catalogue;

        private readonly LevelState m_State = new LevelState();
        private PathFollower[] m_Followers = new PathFollower[0];
        private NetworkGame m_Game;
        private BuiltLevel m_Built;

        public uint Seed => m_State.Seed;

        public TerrainGenerator Generator => m_Generator;

        public PathFollower[] Followers => m_Followers;

        public RapierWorld2D World => m_Built?.World;

        private void Awake()
        {
            m_Game = FindFirstObjectByType<NetworkGame>();
        }

        private void OnDestroy()
        {
            if (m_Game != null && m_Game.Manager != null && m_Game.Manager.TimeManager != null)
            {
                m_Game.Manager.TimeManager.OnPreTick -= MoveFollowers;
            }
        }

        public static LevelCatalogue Read(byte[] bytes)
        {
            var catalogue = new LevelCatalogue();
            LevelCatalogueNetAdapter.Instance.Decode(bytes, ref catalogue);
            return catalogue;
        }

        public static void PrepareFollowers(GameObject block)
        {
            foreach (PathFollower follower in block.GetComponentsInChildren<PathFollower>())
            {
                if (!follower.TryGetComponent(out Rigidbody2D _))
                {
                    follower.gameObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                }
            }
        }

        public static void StaticSources(GameObject block, List<Collider2D> into)
        {
            BodyDescriptions.StaticColliders2D(block, into);
            into.RemoveAll(source => source.GetComponentInParent<Collectable>() != null);
        }

        public static List<Rigidbody2D> BodyRoots(GameObject block) =>
            block.GetComponentsInChildren<Rigidbody2D>()
                .Where(rigidbody => rigidbody.bodyType != RigidbodyType2D.Dynamic && rigidbody.GetComponentInParent<Collectable>() == null)
                .ToList();

        public static Surface SurfaceOfSource(Collider2D source)
        {
            switch (source.GetComponentInParent<Enemy>())
            {
                case Water _:
                    return Surface.Water;
                case Saw _:
                    return Surface.Saw;
                case Spike _:
                    return Surface.Spike;
                case Mace _:
                    return Surface.Mace;
            }

            return source.gameObject.layer == LayerMask.NameToLayer(GroundName) && source.CompareTag(GroundName) ? Surface.Ground : Surface.None;
        }

        public Surface SurfaceOf(RapierCollider collider, out float top)
        {
            if (m_Built == null)
            {
                top = 0f;
                return Surface.None;
            }

            return m_Built.SurfaceOf(collider, out top);
        }

        protected override void OnRegisterState(NetworkState state)
        {
            state.Use(LevelStateNetAdapter.Instance, m_State, previous => Build());
        }

        public override void OnStartServer()
        {
            m_State.Seed = (uint)new System.Random().Next(1, int.MaxValue);
            Debug.Log($"[RedRunner] level seed {m_State.Seed}");
            Build();
        }

        public override void OnStartClient()
        {
            Build();
        }

        private void Build()
        {
            if (m_State.Seed == 0 || m_Generator.IsBuilt)
            {
                return;
            }

            m_Generator.Build((int)m_State.Seed);
            m_Followers = FindObjectsByType<PathFollower>(FindObjectsSortMode.None);
            if (m_Game.Manager.TryGetComponent(out RapierPhysics rapier))
            {
                AddToRapier(rapier);
            }

            m_Game.Manager.TimeManager.OnPreTick += MoveFollowers;
        }

        private void AddToRapier(RapierPhysics rapier)
        {
            var physics = new RapierLevelPhysics(rapier, gameObject.scene, m_Game.Manager.Scenes.IdOf(gameObject.scene.path));
            m_Built = BuiltLevel.Build(Read(m_Catalogue.bytes), (int)m_State.Seed, (float)m_Game.Manager.TimeManager.TickDelta, physics);
            Block[] blocks = FindObjectsByType<Block>(FindObjectsSortMode.None).OrderBy(block => block.transform.position.x).ToArray();
            foreach (LevelMover mover in m_Built.Movers)
            {
                if (mover.Cycle.Count == 0 || mover.Block >= blocks.Length)
                {
                    continue;
                }

                List<Rigidbody2D> roots = BodyRoots(blocks[mover.Block].gameObject);
                if (mover.Body < roots.Count && roots[mover.Body].TryGetComponent(out PathFollower follower))
                {
                    LevelMover driven = mover;
                    follower.Drive(tick =>
                    {
                        System.Numerics.Vector3 position = driven.PositionAt(tick);
                        return new Vector3(position.X, position.Y, position.Z);
                    }, mover.Cycle.Count);
                }
            }
        }

        private void MoveFollowers()
        {
            TimeManager time = m_Game.Manager.TimeManager;
            uint tick = time.PredictionTick;
            float tickDelta = (float)time.TickDelta;
            m_Built?.Move(tick);
            foreach (PathFollower follower in m_Followers)
            {
                follower.FollowTick(tick, tickDelta);
            }
        }

        private sealed class RapierLevelPhysics : ILevelPhysics
        {
            private readonly RapierPhysics rapier;
            private readonly Scene scene;
            private readonly uint sceneId;

            public RapierLevelPhysics(RapierPhysics rapier, Scene scene, uint sceneId)
            {
                this.rapier = rapier;
                this.scene = scene;
                this.sceneId = sceneId;
            }

            public RapierWorld2D World => rapier.Scenes.WorldOf2D(sceneId);

            public StaticGroup AddStatics(IReadOnlyList<ColliderDesc2D> colliders) => rapier.AddStatic2D(scene, colliders);

            public PhysicsBody2D AddBody(in BodyDesc2D body) => rapier.AddBody2D(scene, body);
        }
    }
}
