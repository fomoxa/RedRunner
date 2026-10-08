using System;
using System.Collections.Generic;

namespace RedRunner.Simulation
{
    public enum LevelPart
    {
        Start,
        Middle,
        End,
    }

    public readonly struct BlockChoice
    {
        public BlockChoice(float width, float probability)
        {
            Width = width;
            Probability = probability;
        }

        public float Width { get; }

        public float Probability { get; }
    }

    public readonly struct PlacedBlock
    {
        public PlacedBlock(LevelPart part, int choice, float x)
        {
            Part = part;
            Choice = choice;
            X = x;
        }

        public LevelPart Part { get; }

        public int Choice { get; }

        public float X { get; }
    }

    public static class LevelLayout
    {
        public static List<PlacedBlock> Lay(int seed, IReadOnlyList<BlockChoice> start, int startCount, IReadOnlyList<BlockChoice> middle, int middleCount, IReadOnlyList<BlockChoice> end, int endCount)
        {
            if (startCount <= 0 || middleCount <= 0 || endCount <= 0)
            {
                throw new InvalidOperationException("a seeded level needs positive start, middle and end block counts");
            }

            var random = new SeededRandom(seed);
            var layout = new List<PlacedBlock>();
            float x = 0f;
            Add(layout, start, startCount, LevelPart.Start, random, ref x);
            Add(layout, middle, middleCount, LevelPart.Middle, random, ref x);
            Add(layout, end, endCount, LevelPart.End, random, ref x);
            return layout;
        }

        public static int Choose(IReadOnlyList<BlockChoice> choices, float value)
        {
            if (choices.Count <= 0)
            {
                return -1;
            }

            float total = 0f;
            for (int index = 0; index < choices.Count; index++)
            {
                total += choices[index].Probability;
            }

            float point = value * total;
            for (int index = 0; index < choices.Count; index++)
            {
                if (point < choices[index].Probability)
                {
                    return index;
                }

                point -= choices[index].Probability;
            }

            return choices.Count - 1;
        }

        private static void Add(List<PlacedBlock> layout, IReadOnlyList<BlockChoice> choices, int count, LevelPart part, SeededRandom random, ref float x)
        {
            for (int placed = 0; placed < count; placed++)
            {
                int choice = Choose(choices, random.NextFloat());
                if (choice < 0)
                {
                    throw new InvalidOperationException("no " + part + " block to choose from");
                }

                layout.Add(new PlacedBlock(part, choice, x));
                x += choices[choice].Width;
            }
        }
    }
}
