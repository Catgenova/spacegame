using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// The Fin ship line: destroyer-weight gun platforms defined by 10-digit
    /// body hashes, aquatic where Hive is wasp. No webs or disruptors —
    /// Fins are cheap, turret-heavy fleet ships that hunt in packs.
    /// Class 1 "Shark": 2 turrets, 2 mids, 2 lows.
    /// Class 2 "Manta": 3 turrets, 2 mids, 2 lows — a flying gun wing.
    /// Class 3 "Stingray": 4 turrets, 2 mids, 1 low — the fleet's fang.
    /// Streams: findef / finbody / finpanels.
    /// </summary>
    public static class FinGenerator
    {
        public const string TypeId = "fin";
        public const int MaxClass = 3;

        class FinClass
        {
            public string Label, Doctrine;
            public int TurretSlots, MidSlots, LowSlots;
            public float ShieldMin, ShieldMax, ArmorMin, ArmorMax, HullMin, HullMax;
            public float SpeedMin, SpeedMax, TurnMin, TurnMax;
            public float CapMin, CapMax, RegenMin, RegenMax;
            public float CargoMin, CargoMax;
            public float PriceMin, PriceMax;
            public long Fee;
            public Dictionary<string, float> Materials;
        }

        static readonly Dictionary<int, FinClass> Classes = new Dictionary<int, FinClass>
        {
            [1] = new FinClass
            {
                Label = "Fin-class Shark (C1)",
                Doctrine = "Fleet destroyer: twin guns, cheap to field, lethal in numbers.",
                TurretSlots = 2, MidSlots = 2, LowSlots = 2,
                ShieldMin = 140, ShieldMax = 180, ArmorMin = 100, ArmorMax = 135,
                HullMin = 110, HullMax = 150,
                SpeedMin = 3.4f, SpeedMax = 4.0f, TurnMin = 100, TurnMax = 125,
                CapMin = 120, CapMax = 160, RegenMin = 9, RegenMax = 12,
                CargoMin = 120, CargoMax = 180,
                PriceMin = 95000, PriceMax = 125000,
                Fee = 30000,
                Materials = new Dictionary<string, float>
                {
                    ["iron"] = 480f, ["aluminium"] = 260f, ["titanium"] = 100f, ["beryllium"] = 40f,
                },
            },
            [2] = new FinClass
            {
                Label = "Fin-class Manta (C2)",
                Doctrine = "Wing of guns: three hardpoints on a hull that is mostly wing.",
                TurretSlots = 3, MidSlots = 2, LowSlots = 2,
                ShieldMin = 200, ShieldMax = 250, ArmorMin = 140, ArmorMax = 180,
                HullMin = 150, HullMax = 195,
                SpeedMin = 3.2f, SpeedMax = 3.8f, TurnMin = 85, TurnMax = 110,
                CapMin = 170, CapMax = 220, RegenMin = 11, RegenMax = 14,
                CargoMin = 170, CargoMax = 240,
                PriceMin = 210000, PriceMax = 260000,
                Fee = 55000,
                Materials = new Dictionary<string, float>
                {
                    ["iron"] = 800f, ["aluminium"] = 450f, ["titanium"] = 170f, ["beryllium"] = 70f,
                },
            },
            [3] = new FinClass
            {
                Label = "Fin-class Stingray (C3)",
                Doctrine = "Fleet fang: four hardpoints on a hull built to sting first.",
                TurretSlots = 4, MidSlots = 2, LowSlots = 1,
                ShieldMin = 260, ShieldMax = 320, ArmorMin = 180, ArmorMax = 230,
                HullMin = 190, HullMax = 240,
                SpeedMin = 3.0f, SpeedMax = 3.6f, TurnMin = 75, TurnMax = 100,
                CapMin = 220, CapMax = 280, RegenMin = 13, RegenMax = 16,
                CargoMin = 200, CargoMax = 280,
                PriceMin = 380000, PriceMax = 470000,
                Fee = 95000,
                Materials = new Dictionary<string, float>
                {
                    ["iron"] = 1500f, ["aluminium"] = 850f, ["titanium"] = 340f, ["beryllium"] = 150f,
                },
            },
        };

        static readonly string[] NamePool =
            { "Mako", "Thresher", "Hammer", "Blacktip", "Tiburon", "Reef", "Carcharo", "Lamnid" };

        static readonly string[] FeatureNames =
        {
            "Hydrodynamic baffles (+8% velocity)",
            "Laminar armor skin (+15% shield)",
            "Deep-cycle capacitor (+12% capacitor)",
            "Caudal gyro frame (+12% agility)",
            "Flooded hold spars (+15% cargo)",
        };

        static readonly Dictionary<string, ShipDef> _cache = new Dictionary<string, ShipDef>();

        // ---------- ids ----------

        public static string IdFromHash(string hash, int cls) => TypeId + cls + "-" + hash;

        public static bool IsFinId(string id)
            => id != null && id.Length > TypeId.Length + 2 && id.StartsWith(TypeId)
               && id[TypeId.Length] >= '1' && id[TypeId.Length] <= '9'
               && id[TypeId.Length + 1] == '-';

        public static int ClassFromId(string id) => id[TypeId.Length] - '0';

        public static string HashFromId(string id) => id.Substring(TypeId.Length + 2);

        static string DefKey(int cls, string hash)
            => cls == 1 ? "findef:" + hash : "fin" + cls + "def:" + hash;

        // ---------- blueprints ----------

        /// <summary>Higher classes drop from more dangerous wrecks.</summary>
        static int RollClass(string npcId)
        {
            float c3, c2;
            switch (npcId)
            {
                case "convoyhauler": c3 = 0.12f; c2 = 0.35f; break;
                case "overlord": c3 = 0.08f; c2 = 0.25f; break;
                case "marauder": c3 = 0.03f; c2 = 0.11f; break;
                default: c3 = 0.008f; c2 = 0.04f; break;
            }
            float r2 = Random.value;
            return r2 < c3 ? 3 : r2 < c3 + c2 ? 2 : 1;
        }

        public static Blueprint RollBlueprint(string npcId)
        {
            float r = Random.value;
            int rarity = r < 0.6f ? 0 : r < 0.85f ? 1 : r < 0.97f ? 2 : 3;
            int cls = RollClass(npcId);
            return new Blueprint
            {
                Hash = HiveGenerator.NewHash(),
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
                Desc = "One-off destroyer hull, body " + hash + ". " + c.Doctrine,
                Role = c.Doctrine,
                BodyHash = hash,
                Price = (long)R(c.PriceMin, c.PriceMax),
                Cargo = Mathf.Round(R(c.CargoMin, c.CargoMax)),
                HighSlots = c.TurretSlots, MidSlots = c.MidSlots, LowSlots = c.LowSlots,
                WebSlots = 0, DisruptorSlots = 0,
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
