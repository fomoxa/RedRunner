using System.IO;
using System.Linq;
using RedRunner.Editor.Networking;
using RedRunner.Networking;
using RedRunner.Networking.Models;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public static class PortCatalogue
    {
        public static void Matches()
        {
            byte[] committed = File.ReadAllBytes(LevelCatalogueExport.CataloguePath);
            byte[] exported = LevelCatalogueExport.Encode(LevelCatalogueExport.Describe());
            LevelCatalogue read = NetworkLevel.Read(committed);
            int statics = read.StartBlocks.Concat(read.MiddleBlocks).Concat(read.EndBlocks).Sum(block => block.Statics.Count);
            int bodies = read.StartBlocks.Concat(read.MiddleBlocks).Concat(read.EndBlocks).Sum(block => block.Bodies.Count);
            int moving = read.StartBlocks.Concat(read.MiddleBlocks).Concat(read.EndBlocks).Sum(block => block.Bodies.Count(body => body.Cycle.Count > 0));
            bool same = committed.SequenceEqual(exported);
            Debug.Log($"PORT-CHECK catalogue: {(same ? "PASS" : "FAIL")} {committed.Length} bytes, {read.StartBlocks.Count}/{read.MiddleBlocks.Count}/{read.EndBlocks.Count} blocks, {statics} static colliders, {bodies} bodies ({moving} moving), player sensor {read.Player.SensorIndex}, tick {read.TickDelta}{(same ? "" : ", the prefabs no longer match the committed catalogue; run Tools > RedRunner > Export Level Catalogue")}");
        }
    }
}
