using System;
using System.Collections.Generic;
using RedRunner.Simulation;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public static class PortLayout
    {
        private static readonly (int Seed, ulong Hash)[] Expected =
        {
            (1, 0x54EE435BED94DEBFUL),
            (42, 0x9BC0A40A4F162277UL),
            (123456789, 0x18302D64F47B74EEUL),
            (2147483646, 0x1CFE48E600F0B094UL),
            (948272347, 0xAC3E2D0F9DC46DF2UL),
        };

        public static void Seeds()
        {
            var choices = new List<BlockChoice> { new BlockChoice(25.6f, 1f), new BlockChoice(30.72f, 0.5f), new BlockChoice(20.48f, 2f), new BlockChoice(35.84f, 0.25f) };
            foreach ((int seed, ulong expected) in Expected)
            {
                ulong hash = 1469598103934665603UL;
                foreach (PlacedBlock placed in LevelLayout.Lay(seed, choices, 1, choices, 20, choices, 1))
                {
                    hash = (hash ^ (ulong)placed.Choice ^ ((ulong)BitConverter.SingleToInt32Bits(placed.X) << 8)) * 1099511628211UL;
                }

                if (hash != expected)
                {
                    Debug.Log($"PORT-CHECK layout seeds: FAIL seed {seed} hashes to {hash:X16}, expected {expected:X16}");
                    return;
                }
            }

            Debug.Log($"PORT-CHECK layout seeds: PASS {Expected.Length} seeds lay out the same blocks as under .NET 8");
        }
    }
}
