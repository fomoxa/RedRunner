using System;
using System.Collections.Generic;
using System.Numerics;
using Fomoxa.Networking.Messaging;
using Fomoxa.Networking.Objects;
using Fomoxa.Networking.Rapier;
using Fomoxa.Networking.Simulation;
using RedRunner.Networking.Models;

namespace RedRunner.Simulation
{
    public interface ILevelPhysics
    {
        RapierWorld2D World { get; }

        StaticGroup AddStatics(IReadOnlyList<ColliderDesc2D> colliders);

        PhysicsBody2D AddBody(in BodyDesc2D body);
    }

    public readonly struct LevelMover
    {
        public LevelMover(int block, int body, PhysicsBody2D physicsBody, IReadOnlyList<Vector3> cycle, Vector3 offset)
        {
            Block = block;
            Body = body;
            PhysicsBody = physicsBody;
            Cycle = cycle;
            Offset = offset;
        }

        public int Block { get; }

        public int Body { get; }

        public PhysicsBody2D PhysicsBody { get; }

        public IReadOnlyList<Vector3> Cycle { get; }

        public Vector3 Offset { get; }

        public Vector3 PositionAt(uint tick) => PathCycle.PositionAt(Cycle, tick) + Offset;
    }

    public sealed class BuiltLevel : ISurfaces
    {
        private const float TickTolerance = 1e-6f;

        private readonly List<(Surface Surface, float Top)> statics = new List<(Surface Surface, float Top)>();
        private readonly Dictionary<int, (Surface Surface, float Top)[]> bodies = new Dictionary<int, (Surface Surface, float Top)[]>();
        private readonly List<LevelMover> movers = new List<LevelMover>();

        private BuiltLevel(RapierWorld2D world, IReadOnlyList<PlacedBlock> layout, float endX)
        {
            World = world;
            Layout = layout;
            EndX = endX;
        }

        public RapierWorld2D World { get; }

        public IReadOnlyList<PlacedBlock> Layout { get; }

        public float EndX { get; }

        public IReadOnlyList<LevelMover> Movers => movers;

        public static BuiltLevel Build(LevelCatalogue catalogue, int seed, float tickDelta, ILevelPhysics physics)
        {
            if (Math.Abs(catalogue.TickDelta - tickDelta) > TickTolerance)
            {
                throw new InvalidOperationException($"the level catalogue was exported for a tick of {catalogue.TickDelta} s, the game ticks every {tickDelta} s; export the level catalogue again");
            }

            List<PlacedBlock> layout = LevelLayout.Lay(seed, Choices(catalogue.StartBlocks), (int)catalogue.StartCount, Choices(catalogue.MiddleBlocks), (int)catalogue.MiddleCount, Choices(catalogue.EndBlocks), (int)catalogue.EndCount);
            float endX = 0f;
            foreach (PlacedBlock placed in layout)
            {
                if (placed.Part == LevelPart.End)
                {
                    endX = placed.X;
                    break;
                }
            }

            var level = new BuiltLevel(physics.World, layout, endX);
            for (int index = 0; index < layout.Count; index++)
            {
                level.AddBlock(index, BlockOf(catalogue, layout[index]), layout[index].X, physics);
            }

            return level;
        }

        public static CatalogueBlock BlockOf(LevelCatalogue catalogue, PlacedBlock placed)
        {
            List<CatalogueBlock> blocks = placed.Part == LevelPart.Start ? catalogue.StartBlocks : placed.Part == LevelPart.Middle ? catalogue.MiddleBlocks : catalogue.EndBlocks;
            return blocks[placed.Choice];
        }

        public void Move(uint tick)
        {
            foreach (LevelMover mover in movers)
            {
                if (mover.Cycle.Count == 0 || !mover.PhysicsBody.IsValid)
                {
                    continue;
                }

                Vector3 position = mover.PositionAt(tick);
                PhysicsBody2D body = mover.PhysicsBody;
                BodyState2D state = body.State;
                state.Position = new Vector2(position.X, position.Y);
                body.State = state;
            }
        }

        public Surface SurfaceOf(RapierCollider collider, out float top)
        {
            (Surface Surface, float Top) found = default;
            if (collider.IsStatic)
            {
                if (collider.Index < statics.Count)
                {
                    found = statics[collider.Index];
                }
            }
            else if (bodies.TryGetValue(collider.Body.Value, out (Surface Surface, float Top)[] colliders) && collider.Index < colliders.Length)
            {
                found = colliders[collider.Index];
            }

            top = found.Top;
            return found.Surface;
        }

        private static List<BlockChoice> Choices(List<CatalogueBlock> blocks)
        {
            var choices = new List<BlockChoice>(blocks.Count);
            foreach (CatalogueBlock block in blocks)
            {
                choices.Add(new BlockChoice(block.Width, block.Probability));
            }

            return choices;
        }

        private void AddBlock(int blockIndex, CatalogueBlock block, float x, ILevelPhysics physics)
        {
            var colliders = new List<ColliderDesc2D>(block.Statics.Count);
            foreach (SceneFileCollider2D file in block.Statics)
            {
                ColliderDesc2D local = SceneFileGeometry.ToDesc(file);
                colliders.Add(new ColliderDesc2D(local.Shape, local.Position + new Vector2(x, 0f), local.Rotation, local.Material, local.Layer, local.IsTrigger));
            }

            StaticGroup group = physics.AddStatics(colliders);
            while (statics.Count < group.FirstIndex + colliders.Count)
            {
                statics.Add(default);
            }

            for (int index = 0; index < colliders.Count; index++)
            {
                statics[group.FirstIndex + index] = ((Surface)block.StaticSurfaces[index], block.StaticTops[index]);
            }

            for (int bodyIndex = 0; bodyIndex < block.Bodies.Count; bodyIndex++)
            {
                CatalogueBody entry = block.Bodies[bodyIndex];
                var placed = new SceneFileObject { Pose = new SceneFilePose { PositionX = x + entry.X, PositionY = entry.Y, RotationW = 1f, ScaleX = 1f, ScaleY = 1f, ScaleZ = 1f }, Body2D = entry.Body };
                SceneFileGeometry.TryGetBody2D(placed, out BodyDesc2D desc);
                PhysicsBody2D body = physics.AddBody(desc);
                var roles = new (Surface Surface, float Top)[entry.Surfaces.Count];
                for (int index = 0; index < roles.Length; index++)
                {
                    roles[index] = ((Surface)entry.Surfaces[index], entry.Tops[index]);
                }

                bodies[body.Handle.Value] = roles;
                var cycle = new List<Vector3>(entry.Cycle.Count / 3);
                for (int point = 0; point + 2 < entry.Cycle.Count; point += 3)
                {
                    cycle.Add(new Vector3(entry.Cycle[point], entry.Cycle[point + 1], entry.Cycle[point + 2]));
                }

                movers.Add(new LevelMover(blockIndex, bodyIndex, body, cycle, new Vector3(x, 0f, 0f)));
            }
        }
    }
}
