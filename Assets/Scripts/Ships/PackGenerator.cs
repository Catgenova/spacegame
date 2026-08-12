using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// The Pack ship line: long-range freighters defined by 10-digit body
    /// hashes, camel where Trail is fox. One gun for honor, a few slots
    /// for the crew, and more cargo than anything else that flies.
    /// Class 1 "Bactrian": 1 turret, 2 mids, 4 lows.
    /// Class 2 "Tusker": 1 turret, 3 mids, 5 lows — the elephant bulk
    /// freighter, a trunk to load with and four legs of hold.
    /// Class 3 "Testudo": 1 turret, 3 mids, 6 lows — the tortoise
    /// super-heavy carrier, half shell and half warehouse.
    /// Streams: packdef / packbody / packpanels.
    /// </summary>
    public static class PackGenerator
    {
        public const string TypeId = "pack";
        public const int MaxClass = 3;

        class PackClass
        {
            public string Label, Doctrine;
            public string[] Names;
            public int TurretSlots, MidSlots, LowSlots;
            public float ShieldMin, ShieldMax, ArmorMin, ArmorMax, HullMin, HullMax;
            public float SpeedMin, SpeedMax, TurnMin, TurnMax;
            public float CapMin, CapMax, RegenMin, RegenMax;
            public float CargoMin, CargoMax;
            public float PriceMin, PriceMax;
            public long Fee;
            public Dictionary<string, float> Materials;
        }

        static readonly Dictionary<int, PackClass> Classes = new Dictionary<int, PackClass>
        {
            [1] = new PackClass
            {
                Label = "Pack-class Bactrian (C1)",
                Doctrine = "Long-range freighter: two humps full of cargo and a long way still to go.",
                TurretSlots = 1, MidSlots = 2, LowSlots = 4,
                ShieldMin = 220, ShieldMax = 270, ArmorMin = 200, ArmorMax = 250,
                HullMin = 260, HullMax = 320,
                SpeedMin = 2.6f, SpeedMax = 3.2f, TurnMin = 60, TurnMax = 80,
                CapMin = 200, CapMax = 260, RegenMin = 12, RegenMax = 15,
                CargoMin = 1200, CargoMax = 1500,
                PriceMin = 450000, PriceMax = 550000,
                Fee = 110000,
                Materials = new Dictionary<string, float>
                {
                    ["tritanium"] = 1800f, ["pyerite"] = 1000f, ["mexallon"] = 400f, ["isogen"] = 170f,
                },
            },
            [2] = new PackClass
            {
                Label = "Pack-class Tusker (C2)",
                Doctrine = "Bulk freighter: a trunk to load with and four legs of hold.",
                Names = new[] { "Tusker", "Loxodonta", "Elephas", "Mammoth", "Howdah", "Savanna", "Matriarch", "Ivory" },
                TurretSlots = 1, MidSlots = 3, LowSlots = 5,
                ShieldMin = 320, ShieldMax = 390, ArmorMin = 320, ArmorMax = 390,
                HullMin = 420, HullMax = 500,
                SpeedMin = 2.1f, SpeedMax = 2.6f, TurnMin = 45, TurnMax = 60,
                CapMin = 260, CapMax = 330, RegenMin = 14, RegenMax = 17,
                CargoMin = 2400, CargoMax = 3000,
                PriceMin = 1050000, PriceMax = 1250000,
                Fee = 260000,
                Materials = new Dictionary<string, float>
                {
                    ["tritanium"] = 4000f, ["pyerite"] = 2300f, ["mexallon"] = 950f, ["isogen"] = 420f,
                },
            },
            [3] = new PackClass
            {
                Label = "Pack-class Testudo (C3)",
                Doctrine = "Super-heavy carrier: half shell, half warehouse — it arrives when it arrives.",
                Names = new[] { "Testudo", "Galapagos", "Aldabra", "Carapace", "Archelon", "Meiolania", "Gopherus", "Terrapin" },
                TurretSlots = 1, MidSlots = 3, LowSlots = 6,
                ShieldMin = 450, ShieldMax = 540, ArmorMin = 520, ArmorMax = 620,
                HullMin = 700, HullMax = 820,
                SpeedMin = 1.6f, SpeedMax = 2.0f, TurnMin = 32, TurnMax = 45,
                CapMin = 320, CapMax = 400, RegenMin = 16, RegenMax = 20,
                CargoMin = 4800, CargoMax = 5800,
                PriceMin = 2400000, PriceMax = 2900000,
                Fee = 600000,
                Materials = new Dictionary<string, float>
                {
                    ["tritanium"] = 9000f, ["pyerite"] = 5200f, ["mexallon"] = 2200f, ["isogen"] = 950f,
                },
            },
        };

        static readonly string[] NamePool =
            { "Bactrian", "Dromedary", "Camelus", "Ferus", "Sahara", "Gobi", "Caravan", "Sirocco" };

        static readonly string[] FeatureNames =
        {
            "Caravan drive tuning (+8% velocity)",
            "Sandstorm shielding (+15% shield)",
            "Waterback capacitor tanks (+12% capacitor)",
            "Sure-footed gyros (+12% agility)",
            "Double-hump holds (+15% cargo)",
        };

        static readonly Dictionary<string, ShipDef> _cache = new Dictionary<string, ShipDef>();

        // ---------- ids ----------

        public static string IdFromHash(string hash, int cls) => TypeId + cls + "-" + hash;

        public static bool IsPackId(string id)
            => id != null && id.Length > TypeId.Length + 2 && id.StartsWith(TypeId)
               && id[TypeId.Length] >= '1' && id[TypeId.Length] <= '9'
               && id[TypeId.Length + 1] == '-';

        public static int ClassFromId(string id) => id[TypeId.Length] - '0';

        public static string HashFromId(string id) => id.Substring(TypeId.Length + 2);

        static string DefKey(int cls, string hash)
            => cls == 1 ? "packdef:" + hash : "pack" + cls + "def:" + hash;

        // ---------- blueprints ----------

        /// <summary>Higher classes drop from more dangerous wrecks.</summary>
        static int RollClass(string npcId)
        {
            float c3, c2;
            switch (npcId)
            {
                case "convoyhauler": c3 = 0.12f; c2 = 0.26f; break;
                case "overlord": c3 = 0.08f; c2 = 0.19f; break;
                case "marauder": c3 = 0.03f; c2 = 0.075f; break;
                default: c3 = 0.008f; c2 = 0.022f; break;
            }
            float r2 = Random.value;
            return r2 < c3 ? 3 : r2 < c3 + c2 ? 2 : 1;
        }

        public static Blueprint RollBlueprint(string npcId)
        {
            float r = Random.value;
            int rarity = r < 0.6f ? 0 : r < 0.85f ? 1 : r < 0.97f ? 2 : 3;
            return new Blueprint
            {
                Hash = HiveGenerator.NewHash(),
                TypeId = TypeId,
                Class = RollClass(npcId),
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

            var pool = c.Names ?? NamePool;
            var def = new ShipDef
            {
                Id = IdFromHash(hash, cls),
                Name = pool[rng.Next(pool.Length)] + "-" + hash.Substring(0, 4),
                Class = c.Label,
                Desc = "One-off freighter hull, body " + hash + ". " + c.Doctrine,
                Role = c.Doctrine,
                BodyHash = hash,
                Price = (long)R(c.PriceMin, c.PriceMax),
                Cargo = Mathf.Round(R(c.CargoMin, c.CargoMax)),
                HighSlots = c.TurretSlots, MidSlots = c.MidSlots, LowSlots = c.LowSlots,
                WebSlots = 0, DisruptorSlots = 0, ClawSlots = 0, DroneSlots = 0,
                SensorSlots = 0, CollectorSlots = 0,
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
    }
}
