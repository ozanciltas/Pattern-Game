using System;

namespace PatternGame.Core.Randomness
{
    public sealed class DeterministicRandom
    {
        const uint FallbackState = 2463534242u;

        readonly int seed;

        uint state;

        public DeterministicRandom(int seed)
        {
            this.seed = seed;
            state = seed == 0 ? FallbackState : unchecked((uint)seed);
        }

        public int Seed => seed;

        public uint NextUInt()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state;
        }

        public int NextInt(int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveMaximum),
                    $"Exclusive maximum must be positive but was {exclusiveMaximum}.");
            }

            uint bound = (uint)exclusiveMaximum;
            uint rejectionThreshold = (uint.MaxValue % bound + 1u) % bound;

            uint value;

            do
            {
                value = NextUInt();
            }
            while (value < rejectionThreshold);

            return (int)(value % bound);
        }

        public int NextInt(int inclusiveMinimum, int exclusiveMaximum)
        {
            if (exclusiveMaximum <= inclusiveMinimum)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveMaximum),
                    $"Range [{inclusiveMinimum}, {exclusiveMaximum}) is empty.");
            }

            return inclusiveMinimum + NextInt(exclusiveMaximum - inclusiveMinimum);
        }

        public float NextFloat()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        public bool NextChance(float probability)
        {
            return NextFloat() < probability;
        }
    }
}
