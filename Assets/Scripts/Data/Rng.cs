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
    }
}
