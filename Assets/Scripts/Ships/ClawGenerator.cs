using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// The Claw ship line: dedicated miners defined by 10-digit body hashes.
    /// No turrets — a Claw hull mounts hydraulic rock claws that only work
    /// at 0 km, so the doctrine is: sprint to the rock, latch on, strip it.
    /// Class 1 "Urchin": 1 claw hardpoint, 1 mid, 2 lows. Small and fast.
    /// Streams: clawdef / clawbody / clawpanels.
    /// </summary>
    public static class ClawGenerator
    {
        public const string TypeId = "claw";
        public const int MaxClass = 1;

        class ClawClass
        {
            public string Label, Doctrine;
            public int ClawSlots, MidSlots, LowSlots;
            public float ShieldMin, ShieldMax, ArmorMin, ArmorMax, HullMin, HullMax;
            public float SpeedMin, SpeedMax, TurnMin, TurnMax;
            public float CapMin, CapMax, RegenMin, RegenMax;
            public float CargoMin, CargoMax;
            public float PriceMin, PriceMax;
            public long Fee;
            public Dictionary<string, float> Materials;
        }

        static readonly Dictionary<int, ClawClass> Classes = new Dictionary<int, ClawClass>
        {
            [1] = new ClawClass
            {
                Label = "Claw-class Urchin (C1)",
                Doctrine = "Latch miner: sprint to the rock, clamp on at 0 km, strip it bare.",
                ClawSlots = 1, MidSlots = 1, LowSlots = 2,
                ShieldMin = 100, ShieldMax = 130, ArmorMin = 90, ArmorMax = 120,
                HullMin = 100, HullMax = 130,
                SpeedMin = 4.2f, SpeedMax = 4.8f, TurnMin = 130, TurnMax = 155,
                CapMin = 100, CapMax = 130, RegenMin = 8, RegenMax = 11,
                CargoMin = 200, CargoMax = 280,
                PriceMin = 70000, PriceMax = 95000,
                Fee = 22000,
                Materials = new Dictionary<string, float>
                {
                    ["tritanium"] = 380f, ["pyerite"] = 200f, ["mexallon"] = 80f, ["isogen"] = 30f,
                },
            },
        };

        static readonly string[] NamePool =
            { "Urchin", "Krill", "Fiddler", "Hermit", "Pincer", "Molt", "Barnacle", "Scuttle" };

        static readonly string[] FeatureNames =
        {
            "Overtuned escape thrusters (+8% velocity)",
            "Reinforced shell plating (+15% shield)",
            "High-torque capacitor (+12% capacitor)",
            "Counterweight gyros (+12% agility)",
            "Expanded ore hold (+15% cargo)",
        };

        static readonly Dictionary<string, ShipDef> _cache = new Dictionary<string, ShipDef>();

        // ---------- ids ----------

        public static string IdFromHash(string hash, int cls) => TypeId + cls + "-" + hash;

        public static bool IsClawId(string id)
            => id != null && id.Length > TypeId.Length + 2 && id.StartsWith(TypeId)
               && id[TypeId.Length] >= '1' && id[TypeId.Length] <= '9'
               && id[TypeId.Length + 1] == '-';

        public static int ClassFromId(string id) => id[TypeId.Length] - '0';

        public static string HashFromId(string id) => id.Substring(TypeId.Length + 2);

        static string DefKey(int cls, string hash)
            => cls == 1 ? "clawdef:" + hash : "claw" + cls + "def:" + hash;

        // ---------- blueprints ----------

        public static Blueprint RollBlueprint(string npcId)
        {
            float r = Random.value;
            int rarity = r < 0.6f ? 0 : r < 0.85f ? 1 : r < 0.97f ? 2 : 3;
            return new Blueprint
            {
                Hash = HiveGenerator.NewHash(),
                TypeId = TypeId,
                Class = 1,
                Rarity = rarity,
                RunsLeft = GameData.RarityRuns[rarity],
            };
        }

        // ---------- stat sheets ----------

        public static ShipDef Def(string hash) => Def(hash, 1);

        public static ShipDef Def(string hash, int cls)
        {
            cls = Mathf.Clamp(cls, 1, MaxClass);
            string key = cls + ":" + hash;
            if (_cache.TryGetValue(key, out var cached)) return cached;
            var c = Classes[cls];
            var rng = Rng.Stream(DefKey(cls, hash));
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);

            var def = new ShipDef
            {
                Id = IdFromHash(hash, cls),
                Name = NamePool[rng.Next(NamePool.Length)] + "-" + hash.Substring(0, 4),
                Class = c.Label,
                Desc = "One-off mining hull, body " + hash + ". " + c.Doctrine,
                Role = c.Doctrine,
                BodyHash = hash,
                Price = (long)R(c.PriceMin, c.PriceMax),
                Cargo = Mathf.Round(R(c.CargoMin, c.CargoMax)),
                HighSlots = 0, MidSlots = c.MidSlots, LowSlots = c.LowSlots,
                WebSlots = 0, DisruptorSlots = 0, ClawSlots = c.ClawSlots,
                TurretOnly = false,
                Shield = Mathf.Round(R(c.ShieldMin, c.ShieldMax)),
                Armor = Mathf.Round(R(c.ArmorMin, c.ArmorMax)),
                Hull = Mathf.Round(R(c.HullMin, c.HullMax)),
                Cap = Mathf.Round(R(c.CapMin, c.CapMax)),
                CapRegen = R(c.RegenMin, c.RegenMax),
                Speed = R(c.SpeedMin, c.SpeedMax),
                Turn = R(c.TurnMin, c.TurnMax),
                MiningBonus = 1.6f,
            };

            int f1 = rng.Next(FeatureNames.Length);
            int f2 = (f1 + 1 + rng.Next(FeatureNames.Length - 1)) % FeatureNames.Length;
            def.Features = new[] { FeatureNames[f1], FeatureNames[f2] };
            ApplyFeature(def, f1);
            ApplyFeature(def, f2);

            _cache[key] = def;
            return def;
        }

        static void ApplyFeature(ShipDef d, int idx)
        {
            switch (idx)
            {
                case 0: d.Speed *= 1.08f; break;
                case 1: d.Shield = Mathf.Round(d.Shield * 1.15f); break;
                case 2: d.Cap = Mathf.Round(d.Cap * 1.12f); break;
                case 3: d.Turn *= 1.12f; break;
                case 4: d.Cargo = Mathf.Round(d.Cargo * 1.15f); break;
            }
        }

        // ---------- manufacturing ----------

        public static Dictionary<string, float> MaterialCost(int shipClass)
            => Classes[Mathf.Clamp(shipClass, 1, MaxClass)].Materials;

        public static long Fee(int shipClass)
            => Classes[Mathf.Clamp(shipClass, 1, MaxClass)].Fee;
    }
}
