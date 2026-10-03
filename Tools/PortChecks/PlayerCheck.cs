#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Fomoxa.Unity;
using Fomoxa.Unity.Sessions;
using RedRunner.Networking;
using RedRunner.TerrainGeneration;
using UnityEditor;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public abstract class PlayerCheck : MonoBehaviour
    {
        protected const float TimeoutSeconds = 30f;

        protected NetworkGame Game { get; private set; }

        protected NetworkLevel Level { get; private set; }

        protected abstract void Begin();

        protected virtual IEnumerator Verify() { yield break; }

        private IEnumerator Start()
        {
            Game = FindFirstObjectByType<NetworkGame>();
            Level = FindObjectsByType<NetworkLevel>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            if (Game == null || Level == null)
            {
                Finish(false, "the Play scene needs a NetworkGame and a NetworkLevel");
                yield break;
            }

            var collected = (NetworkPrefabList)typeof(NetworkManager).GetField("collectedPrefabs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(Game.Manager);
            Debug.Log($"PORT-CHECK diagnostics: collected prefabs {(collected == null ? "null" : collected.Prefabs.Count.ToString())}, registered {Game.Manager.Prefabs.Count}");
            Begin();
            float startedAt = Time.realtimeSinceStartup;
            TerrainGenerator generator = Level.Generator;
            int blocks = generator.Settings.StartBlocksCount + generator.Settings.MiddleBlocksCount + generator.Settings.EndBlocksCount;
            while (Game.Manager.ClientManager.State != ConnectionState.Started || !generator.IsBuilt || Game.LocalPlayer == null)
            {
                if (Time.realtimeSinceStartup - startedAt > TimeoutSeconds)
                {
                    Finish(false, $"client {Game.Manager.ClientManager.State}, seed {Level.Seed}, built {generator.IsBuilt}, local player {Game.LocalPlayer != null}");
                    yield break;
                }

                yield return null;
            }

            if (generator.BlockCount != blocks)
            {
                Finish(false, $"the client built {generator.BlockCount} blocks, expected {blocks}");
                yield break;
            }

            yield return new WaitForSecondsRealtime(3f);
            Vector3 position = Game.LocalPlayer.transform.position;
            if (Mathf.Abs(position.x - Game.SpawnPoint.x) > 1f || position.y <= 0f || position.y >= Game.SpawnPoint.y)
            {
                Finish(false, $"the local player is at {position}, expected to stand below the spawn point {Game.SpawnPoint}");
                yield break;
            }

            if (!Game.LocalPlayer.NetworkObject.IsOwner)
            {
                Finish(false, "the local player is not owned by this client");
                yield break;
            }

            yield return Verify();
            if (!finished)
            {
                Finish(true, $"seed {Level.Seed}, {generator.BlockCount} blocks, local player object {Game.LocalPlayer.NetworkObject.ObjectId} at {position}");
            }
        }

        private bool finished;

        protected void Finish(bool passed, string detail)
        {
            if (finished)
            {
                return;
            }

            finished = true;
            Debug.Log($"PORT-CHECK {GetType().Name}: {(passed ? "PASS" : "FAIL")} {detail}");
            EditorApplication.Exit(passed ? 0 : 1);
        }
    }
}
#endif
