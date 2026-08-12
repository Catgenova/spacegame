using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// The Trail ship line: exploration scouts defined by 10-digit body
    /// hashes, fennec where Scale is serpent. No guns at all — a Trail
    /// hears salvage before anyone else sees it, sweeps it up, and runs.
    /// Class 1 "Fennec": 1 sensor hardpoint, 1 collector hardpoint,
    /// 3 mids, 2 lows.
    /// Class 2 "Otocyon": 2 sensors, 2 collectors, 3 mids, 3 lows — the
    /// bat-eared dark-space explorer.
    /// Class 3 "Nanook": 3 sensors, 2 collectors, 4 mids, 3 lows — the
    /// arctic surveyor that noses out everything in the deep.
    /// Streams: traildef / trailbody / trailpanels.
    /// </summary>
    public static class TrailGenerator
    {
        public const string TypeId = "trail";
        public const int MaxClass = 3;

        class TrailClass
        {
            public string Label, Doctrine;
            public string[] Names;
            public int SensorSlots, CollectorSlots, MidSlots, LowSlots;
            public float ShieldMin, ShieldMax, ArmorMin, ArmorMax, HullMin, HullMax;
            public float SpeedMin, SpeedMax, TurnMin, TurnMax;
            public float CapMin, CapMax, RegenMin, RegenMax;
            public float CargoMin, CargoMax;
            public float PriceMin, PriceMax;
            public long Fee;
            public Dictionary<string, float> Materials;
        }

        static readonly Dictionary<int, TrailClass> Classes = new Dictionary<int, TrailClass>
        {
            [1] = new TrailClass
            {
                Label = "Trail-class Fennec (C1)",
                Doctrine = "Pathfinder: big ears, no guns — it hears salvage before anyone sees it.",
                SensorSlots = 1, CollectorSlots = 1, MidSlots = 3, LowSlots = 2,
                ShieldMin = 160, ShieldMax = 200, ArmorMin = 90, ArmorMax = 120,
                HullMin = 100, HullMax = 130,
                SpeedMin = 4.4f, SpeedMax = 5.0f, TurnMin = 140, TurnMax = 165,
                CapMin = 240, CapMax = 300, RegenMin = 14, RegenMax = 18,
                CargoMin = 260, CargoMax = 360,
                PriceMin = 240000, PriceMax = 300000,
                Fee = 60000,
                Materials = new Dictionary<string, float>
                {
                    ["iron"] = 900f, ["aluminium"] = 500f, ["titanium"] = 210f, ["beryllium"] = 90f,
                },
            },
            [2] = new TrailClass
            {
                Label = "Trail-class Otocyon (C2)",
                Doctrine = "Dark-space explorer: twin ears spread wide, it maps what the charts missed.",
                Names = new[] { "Otocyon", "Batear", "Culpeo", "Pampas", "Bengal", "Tibetan", "Blanford", "Ruppell" },
                SensorSlots = 2, CollectorSlots = 2, MidSlots = 3, LowSlots = 3,
                ShieldMin = 260, ShieldMax = 320, ArmorMin = 150, ArmorMax = 190,
                HullMin = 160, HullMax = 200,
                SpeedMin = 3.8f, SpeedMax = 4.4f, TurnMin = 110, TurnMax = 135,
                CapMin = 380, CapMax = 460, RegenMin = 18, RegenMax = 22,
                CargoMin = 420, CargoMax = 560,
                PriceMin = 520000, PriceMax = 640000,
                Fee = 130000,
                Materials = new Dictionary<string, float>
                {
                    ["iron"] = 2000f, ["aluminium"] = 1150f, ["titanium"] = 460f, ["beryllium"] = 200f,
                },
            },
            [3] = new TrailClass
            {
                Label = "Trail-class Nanook (C3)",
                Doctrine = "Arctic surveyor: slow, patient, unarmed — nothing drifts past its nose.",
                Names = new[] { "Nanook", "Ursus", "Maritimus", "Kodiak", "Polaris", "Bruin", "Isbjorn", "Grizzled" },
                SensorSlots = 3, CollectorSlots = 2, MidSlots = 4, LowSlots = 3,
                ShieldMin = 400, ShieldMax = 480, ArmorMin = 260, ArmorMax = 320,
                HullMin = 280, HullMax = 340,
                SpeedMin = 3.0f, SpeedMax = 3.6f, TurnMin = 80, TurnMax = 100,
                CapMin = 560, CapMax = 680, RegenMin = 22, RegenMax = 27,
                CargoMin = 700, CargoMax = 900,
                PriceMin = 1100000, PriceMax = 1350000,
                Fee = 280000,
                Materials = new Dictionary<string, float>
                {
                    ["iron"] = 4200f, ["aluminium"] = 2400f, ["titanium"] = 1000f, ["beryllium"] = 450f,
                },
            },
        };

        static readonly string[] NamePool =
            { "Fennec", "Vulpes", "Kit", "Corsac", "Swift", "Reynard", "Zerda", "Tod" };

        static readonly string[] FeatureNames =
        {
            "Featherweight chassis (+8% velocity)",
            "Dune-runner shielding (+15% shield)",
            "Big-ear capacitor coils (+12% capacitor)",
            "Fennec reflex gyros (+12% agility)",
            "Saddlebag holds (+15% cargo)",
        };

        static readonly Dictionary<string, ShipDef> _cache = new Dictionary<string, ShipDef>();

        // ---------- ids ----------

        public static string IdFromHash(string hash, int cls) => TypeId + cls + "-" + hash;

        public static bool IsTrailId(string id)
            => id != null && id.Length > TypeId.Length + 2 && id.StartsWith(TypeId)
               && id[TypeId.Length] >= '1' && id[TypeId.Length] <= '9'
               && id[TypeId.Length + 1] == '-';

        public static int ClassFromId(string id) => id[TypeId.Length] - '0';

        public static string HashFromId(string id) => id.Substring(TypeId.Length + 2);

        static string DefKey(int cls, string hash)
            => cls == 1 ? "traildef:" + hash : "trail" + cls + "def:" + hash;

        // ---------- blueprints ----------

        /// <summary>Higher classes drop from more dangerous wrecks.</summary>
        static int RollClass(string npcId)
        {
            float c3, c2;
            switch (npcId)
            {
                case "convoyhauler": c3 = 0.10f; c2 = 0.24f; break;
                case "overlord": c3 = 0.07f; c2 = 0.18f; break;
                case "marauder": c3 = 0.025f; c2 = 0.07f; break;
                default: c3 = 0.006f; c2 = 0.022f; break;
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
                Desc = "One-off scout hull, body " + hash + ". " + c.Doctrine,
                Role = c.Doctrine,
                BodyHash = hash,
                Price = (long)R(c.PriceMin, c.PriceMax),
                Cargo = Mathf.Round(R(c.CargoMin, c.CargoMax)),
                HighSlots = 0, MidSlots = c.MidSlots, LowSlots = c.LowSlots,
                WebSlots = 0, DisruptorSlots = 0, ClawSlots = 0, DroneSlots = 0,
                SensorSlots = c.SensorSlots, CollectorSlots = c.CollectorSlots,
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
