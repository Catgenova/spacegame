namespace SpaceGame
{
    /// <summary>Deterministic seeding helpers for universe generation and market prices.</summary>
    public static class Rng
    {
        public static int Hash(string s)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (char c in s)
                {
                    h ^= c;
                    h *= 16777619;
                }
                return (int)h;
            }
        }

        public static System.Random Seeded(string s) => new System.Random(Hash(s));

        public static float Range(System.Random r, float min, float max)
            => min + (float)r.NextDouble() * (max - min);

        /// <summary>FNV-1a as an unsigned hash. Public because callers that need
        /// a stable non-negative bucket (which of N mesh variants a rock uses)
        /// want the raw value, not System.Random.</summary>
        public static uint HashU(string s)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (char c in s)
                {
                    h ^= c;
                    h *= 16777619;
                }
                return h;
            }
        }

        /// <summary>
        /// Strict deterministic stream (mulberry32) for ship generation:
        /// NextDouble is guaranteed in [0, 1), unlike System.Random whose
        /// Knuth mixing can very rarely dip below zero for large seeds —
        /// which would push generated stats outside their design envelopes.
        /// Trivially portable (pure uint32 ops), so external tools can
        /// reproduce bodies exactly.
        /// </summary>
        public class Roll
        {
            uint _s;
            public Roll(uint seed) { _s = seed; }

            public double NextDouble()
            {
                unchecked
                {
                    _s += 0x6D2B79F5u;
                    uint t = _s;
                    t = (t ^ (t >> 15)) * (1u | t);
                    t = (t + ((t ^ (t >> 7)) * (61u | t))) ^ t;
                    return (t ^ (t >> 14)) / 4294967296.0;
                }
            }

            public int Next(int max) => (int)(NextDouble() * max);
        }

        public static Roll Stream(string key) => new Roll(HashU(key));
    }
}
