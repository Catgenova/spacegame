using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// The Hive ship line: swarm ships defined by 10-digit body hashes.
    /// Class 1 "Scout": 1 turret, 1 web, 1 low — dart hull.
    /// Class 2 "Striker": 2 turrets, 1 web, 2 lows — twin-cannon fighter.
    /// A hash + class fully determines name, stats, traits, and mesh.
    /// C1 keeps its historical stream keys ("hivedef:"/"hivebody:"); higher
    /// classes use "hive&lt;c&gt;def:" etc, so classes roll independently.
    /// </summary>
    public static class HiveGenerator
    {
        public const string TypeId = "hive";
        public const int MaxClass = 2;

        class HiveClass
        {
            public string Label, Doctrine;
            public int TurretSlots, WebSlots, LowSlots;
            public float ShieldMin, ShieldMax, ArmorMin, ArmorMax, HullMin, HullMax;
            public float SpeedMin, SpeedMax, TurnMin, TurnMax;
            public float CapMin, CapMax, RegenMin, RegenMax;
            public float CargoMin, CargoMax;
            public float PriceMin, PriceMax;
            public long Fee;
            public Dictionary<string, float> Materials;
        }

        static readonly Dictionary<int, HiveClass> Classes = new Dictionary<int, HiveClass>
        {
            [1] = new HiveClass
            {
                Label = "Hive-class Scout (C1)",
                Doctrine = "Swarm scout: dart in, web the target, let the swarm feed.",
                TurretSlots = 1, WebSlots = 1, LowSlots = 1,
                ShieldMin = 90, ShieldMax = 125, ArmorMin = 55, ArmorMax = 80,
                HullMin = 65, HullMax = 90,
                SpeedMin = 4.1f, SpeedMax = 4.8f, TurnMin = 150, TurnMax = 175,
                CapMin = 85, CapMax = 110, RegenMin = 7, RegenMax = 9.5f,
                CargoMin = 80, CargoMax = 130,
                PriceMin = 55000, PriceMax = 78000,
                Fee = 20000,
                Materials = new Dictionary<string, float>
                {
                    ["tritanium"] = 320f, ["pyerite"] = 180f, ["mexallon"] = 60f, ["isogen"] = 25f,
                },
            },
            [2] = new HiveClass
            {
                Label = "Hive-class Striker (C2)",
                Doctrine = "Swarm striker: twin guns and a web — pin the target, saw it down.",
                TurretSlots = 2, WebSlots = 1, LowSlots = 2,
                ShieldMin = 170, ShieldMax = 220, ArmorMin = 110, ArmorMax = 150,
                HullMin = 120, HullMax = 160,
                SpeedMin = 3.6f, SpeedMax = 4.2f, TurnMin = 110, TurnMax = 135,
                CapMin = 140, CapMax = 180, RegenMin = 10, RegenMax = 13,
                CargoMin = 140, CargoMax = 220,
                PriceMin = 140000, PriceMax = 185000,
                Fee = 45000,
                Materials = new Dictionary<string, float>
                {
                    ["tritanium"] = 640f, ["pyerite"] = 360f, ["mexallon"] = 140f, ["isogen"] = 60f,
                },
            },
        };

        static readonly string[] NamePool =
            { "Hornet", "Vespa", "Stinger", "Dart", "Mantis", "Widow", "Naja", "Sicaria" };

        static readonly string[] FeatureNames =
        {
            "Overtuned thrusters (+8% velocity)",
            "Waxed chitin plating (+15% shield)",
            "Expanded capacitor coils (+12% capacitor)",
            "Gyroscopic thorax frame (+12% agility)",
            "Hollowed wing spars (+15% cargo)",
        };

        static readonly Dictionary<string, ShipDef> _cache = new Dictionary<string, ShipDef>();

        // ---------- ids ----------

        public static string IdFromHash(string hash, int cls) => TypeId + cls + "-" + hash;

        public static bool IsHiveId(string id)
            => id != null && id.Length > TypeId.Length + 2 && id.StartsWith(TypeId)
               && id[TypeId.Length] >= '1' && id[TypeId.Length] <= '9'
               && id[TypeId.Length + 1] == '-';

        public static int ClassFromId(string id) => id[TypeId.Length] - '0';

        public static string HashFromId(string id) => id.Substring(TypeId.Length + 2);

        static string DefKey(int cls, string hash)
            => cls == 1 ? "hivedef:" + hash : "hive" + cls + "def:" + hash;

        // ---------- blueprints ----------

        public static string NewHash()
        {
            var s = "";
            for (int i = 0; i < 10; i++) s += Random.Range(0, 10).ToString();
            return s;
        }

        /// <summary>Class-2 chance scales with how dangerous the source was.</summary>
        static float C2Chance(string npcId)
        {
            switch (npcId)
            {
                case "convoyhauler": return 0.50f;
                case "overlord": return 0.35f;
                case "marauder": return 0.15f;
                default: return 0.05f;
            }
        }

        public static Blueprint RollBlueprint(string npcId)
        {
            float r = Random.value;
            int rarity = r < 0.6f ? 0 : r < 0.85f ? 1 : r < 0.97f ? 2 : 3;
            int cls = Random.value < C2Chance(npcId) ? 2 : 1;
            return new Blueprint
            {
                Hash = NewHash(),
                TypeId = TypeId,
                Class = cls,
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
                Desc = "One-off swarm hull, body " + hash + ". " + c.Doctrine,
                Role = c.Doctrine,
                BodyHash = hash,
                Price = (long)R(c.PriceMin, c.PriceMax),
                Cargo = Mathf.Round(R(c.CargoMin, c.CargoMax)),
                HighSlots = c.TurretSlots, MidSlots = 0, LowSlots = c.LowSlots, WebSlots = c.WebSlots,
                TurretOnly = true,
                Shield = Mathf.Round(R(c.ShieldMin, c.ShieldMax)),
                Armor = Mathf.Round(R(c.ArmorMin, c.ArmorMax)),
                Hull = Mathf.Round(R(c.HullMin, c.HullMax)),
                Cap = Mathf.Round(R(c.CapMin, c.CapMax)),
                CapRegen = R(c.RegenMin, c.RegenMax),
                Speed = R(c.SpeedMin, c.SpeedMax),
                Turn = R(c.TurnMin, c.TurnMax),
                MiningBonus = 0.5f,
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

        public static string DescribeBlueprint(Blueprint bp)
        {
            var def = Def(bp.Hash, bp.Class);
            return def.Name + " (C" + bp.Class + ")  [" + GameData.RarityNames[bp.Rarity] + ", "
                + bp.RunsLeft + " run" + (bp.RunsLeft == 1 ? "" : "s") + " left]";
        }
    }
}
