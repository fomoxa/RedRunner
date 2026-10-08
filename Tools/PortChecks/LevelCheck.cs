#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fomoxa.Networking;
using Fomoxa.Unity;
using RedRunner.Networking;
using RedRunner.TerrainGeneration;
using UnityEditor;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public sealed class LevelCheck : MonoBehaviour
    {
        private IEnumerator Start()
        {
            NetworkLevel level = FindObjectsByType<NetworkLevel>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            NetworkManager server = FindObjectsByType<NetworkManager>(FindObjectsSortMode.None).FirstOrDefault();
            if (level == null || server == null)
            {
                Finish(false, "the Play scene needs a NetworkLevel and a NetworkManager");
                yield break;
            }

            TerrainGenerator generator = level.Generator;
            yield return null;
            if (generator.IsBuilt || Foreground().Count != 0)
            {
                Finish(false, $"the level exists before the server starts: {Foreground().Count} blocks");
                yield break;
            }

            server.ServerManager.StartConnection(0);
            yield return null;
            if (level.Seed == 0 || !generator.IsBuilt)
            {
                Finish(false, $"the server did not build the level: seed {level.Seed}, built {generator.IsBuilt}");
                yield break;
            }

            List<LevelBlock> layout = generator.Settings.Layout((int)level.Seed);
            List<Block> blocks = Foreground();
            int expected = generator.Settings.StartBlocksCount + generator.Settings.MiddleBlocksCount + generator.Settings.EndBlocksCount;
            if (layout.Count != expected || blocks.Count != expected)
            {
                Finish(false, $"expected {expected} blocks, layout has {layout.Count}, scene has {blocks.Count}");
                yield break;
            }

            for (int i = 0; i < expected; i++)
            {
                string name = blocks[i].name.Replace("(Clone)", "");
                if (name != layout[i].Prefab.name || !Mathf.Approximately(blocks[i].transform.position.x, layout[i].X))
                {
                    Finish(false, $"block {i} is {name} at {blocks[i].transform.position.x}, the layout says {layout[i].Prefab.name} at {layout[i].X}");
                    yield break;
                }
            }

            if (layout[0].Part != LevelPart.Start || layout[expected - 1].Part != LevelPart.End || !Mathf.Approximately(generator.EndX, layout[expected - 1].X))
            {
                Finish(false, $"start or end block out of place, end x {generator.EndX}");
                yield break;
            }

            string again = Signature(generator.Settings.Layout((int)level.Seed));
            string other = Signature(generator.Settings.Layout((int)level.Seed + 1));
            if (again != Signature(layout) || other == again)
            {
                Finish(false, "the layout does not depend on the seed alone");
                yield break;
            }

            generator.Build((int)level.Seed + 1);
            if (Foreground().Count != expected)
            {
                Finish(false, "a second Build added blocks");
                yield break;
            }

            Finish(true, $"seed {level.Seed}, {expected} blocks, end at x {generator.EndX}, layout {Signature(layout).GetHashCode():X8}");
        }

        private static List<Block> Foreground() =>
            FindObjectsByType<Block>(FindObjectsSortMode.None)
                .Where(block => !(block is BackgroundBlock))
                .OrderBy(block => block.transform.position.x)
                .ToList();

        private static string Signature(List<LevelBlock> layout) =>
            string.Join(";", layout.Select(block => block.Prefab.name + "@" + block.X.ToString("F2")));

        private static void Finish(bool passed, string detail)
        {
            Debug.Log($"PORT-CHECK {nameof(LevelCheck)}: {(passed ? "PASS" : "FAIL")} {detail}");
            EditorApplication.Exit(passed ? 0 : 1);
        }
    }
}
#endif
