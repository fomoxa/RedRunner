using System.Collections.Generic;
using Fomoxa;
using Fomoxa.Networking.Messaging;

namespace RedRunner.Networking.Models
{
    [Network]
    [Codec("net")]
    public class LevelCatalogue
    {
        [Network("f32")]
        [Codec("net")]
        public float TickDelta { get; set; }

        [Network("u32")]
        [Codec("net")]
        public uint StartCount { get; set; }

        [Network("u32")]
        [Codec("net")]
        public uint MiddleCount { get; set; }

        [Network("u32")]
        [Codec("net")]
        public uint EndCount { get; set; }

        [Network("Array<CatalogueBlock>")]
        [Codec("net")]
        public List<CatalogueBlock> StartBlocks { get; set; } = new List<CatalogueBlock>();

        [Network("Array<CatalogueBlock>")]
        [Codec("net")]
        public List<CatalogueBlock> MiddleBlocks { get; set; } = new List<CatalogueBlock>();

        [Network("Array<CatalogueBlock>")]
        [Codec("net")]
        public List<CatalogueBlock> EndBlocks { get; set; } = new List<CatalogueBlock>();

        [Network("CataloguePlayer")]
        [Codec("net")]
        public CataloguePlayer Player { get; set; } = new CataloguePlayer();
    }

    [Network]
    [Codec("net")]
    public class CatalogueBlock
    {
        [Network("f32")]
        [Codec("net")]
        public float Width { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float Probability { get; set; }

        [Network("Array<SceneFileCollider2D>")]
        [Codec("net")]
        public List<SceneFileCollider2D> Statics { get; set; } = new List<SceneFileCollider2D>();

        [Network("Array<u32>")]
        [Codec("net")]
        public List<uint> StaticSurfaces { get; set; } = new List<uint>();

        [Network("Array<f32>")]
        [Codec("net")]
        public List<float> StaticTops { get; set; } = new List<float>();

        [Network("Array<CatalogueBody>")]
        [Codec("net")]
        public List<CatalogueBody> Bodies { get; set; } = new List<CatalogueBody>();
    }

    [Network]
    [Codec("net")]
    public class CatalogueBody
    {
        [Network("f32")]
        [Codec("net")]
        public float X { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float Y { get; set; }

        [Network("SceneFileBody2D")]
        [Codec("net")]
        public SceneFileBody2D Body { get; set; } = new SceneFileBody2D();

        [Network("Array<u32>")]
        [Codec("net")]
        public List<uint> Surfaces { get; set; } = new List<uint>();

        [Network("Array<f32>")]
        [Codec("net")]
        public List<float> Tops { get; set; } = new List<float>();

        [Network("Array<f32>")]
        [Codec("net")]
        public List<float> Cycle { get; set; } = new List<float>();
    }

    [Network]
    [Codec("net")]
    public class CataloguePlayer
    {
        [Network("SceneFileBody2D")]
        [Codec("net")]
        public SceneFileBody2D Body { get; set; } = new SceneFileBody2D();

        [Network("u32")]
        [Codec("net")]
        public uint SensorIndex { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float BottomOffset { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float RunSpeed { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float MaxRunSpeed { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float RunSmoothTime { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float JumpStrength { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float RollForce { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float SpawnX { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float SpawnY { get; set; }

        [Network("f32")]
        [Codec("net")]
        public float RespawnDelay { get; set; }

        [Network("u32")]
        [Codec("net")]
        public uint MaxRepeatedTicks { get; set; }
    }
}
