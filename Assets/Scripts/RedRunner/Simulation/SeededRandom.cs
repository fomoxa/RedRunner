namespace RedRunner.Simulation
{
    public sealed class SeededRandom
    {
        private const ulong Golden = 0x9E3779B97F4A7C15UL;
        private const float FloatUnit = 1f / 16777216f;

        private ulong state;

        public SeededRandom(int seed)
        {
            state = (ulong)(uint)seed;
        }

        public ulong NextBits()
        {
            state += Golden;
            ulong mixed = state;
            mixed = (mixed ^ (mixed >> 30)) * 0xBF58476D1CE4E5B9UL;
            mixed = (mixed ^ (mixed >> 27)) * 0x94D049BB133111EBUL;
            return mixed ^ (mixed >> 31);
        }

        public float NextFloat() => (NextBits() >> 40) * FloatUnit;
    }
}
