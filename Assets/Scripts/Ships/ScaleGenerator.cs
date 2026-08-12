using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// The Scale ship line: battleship-weight gun platforms defined by
    /// 10-digit body hashes, reptilian where Talon is avian. Slow, heavily
    /// armored serpents of the line — four batteries behind ablative hide.
    /// Class 1 "Python": 4 turrets, 3 mids, 3 lows.
    /// Streams: scaledef / scalebody / scalepanels.
    /// </summary>
    public static class ScaleGenerator
    {
        public const string TypeId = "scale";
        public const int MaxClass = 1;

        class ScaleClass
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

        static readonly Dictionary<int, ScaleClass> Classes = new Dictionary<int, ScaleClass>
        {
            [1] = new ScaleClass
            {
                Label = "Scale-class Python (C1)",
                Doctrine = "Line battleship: four batteries behind a hide of ablative scales.",
                TurretSlots = 4, MidSlots = 3, LowSlots = 3,
                ShieldMin = 650, ShieldMax = 780, ArmorMin = 520, ArmorMax = 640,
                HullMin = 560, HullMax = 680,
                SpeedMin = 1.9f, SpeedMax = 2.4f, TurnMin = 40, TurnMax = 55,
                CapMin = 480, CapMax = 580, RegenMin = 20, RegenMax = 25,
                CargoMin = 400, CargoMax = 550,
                PriceMin = 1600000, PriceMax = 2000000,
                Fee = 400000,
                Materials = new Dictionary<string, float>
                {
                    ["tritanium"] = 6000f, ["pyerite"] = 3500f, ["mexallon"] = 1500f, ["isogen"] = 700f,
                },
            },
        };

        static readonly string[] NamePool =
            { "Python", "Boa", "Anaconda", "Taipan", "Mamba", "Krait", "Adder", "Viper" };

        static readonly string[] FeatureNames =
        {
            "Slither drive tuning (+8% velocity)",
            "Ablative scale plating (+15% shield)",
            "Cold-blood heat sinks (+12% capacitor)",
            "Serpentine gyros (+12% agility)",
            "Swallowed-whole cargo gut (+15% cargo)",
        };

        static readonly Dictionary<string, ShipDef> _cache = new Dictionary<string, ShipDef>();

        // ---------- ids ----------

        public static string IdFromHash(string hash, int cls) => TypeId + cls + "-" + hash;

        public static bool IsScaleId(string id)
            => id != null && id.Length > TypeId.Length + 2 && id.StartsWith(TypeId)
               && id[TypeId.Length] >= '1' && id[TypeId.Length] <= '9'
               && id[TypeId.Length + 1] == '-';

        public static int ClassFromId(string id) => id[TypeId.Length] - '0';

        public static string HashFromId(string id) => id.Substring(TypeId.Length + 2);

        static string DefKey(int cls, string hash)
            => cls == 1 ? "scaledef:" + hash : "scale" + cls + "def:" + hash;

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
                Desc = "One-off battleship hull, body " + hash + ". " + c.Doctrine,
                Role = c.Doctrine,
                BodyHash = hash,
                Price = (long)R(c.PriceMin, c.PriceMax),
                Cargo = Mathf.Round(R(c.CargoMin, c.CargoMax)),
                HighSlots = c.TurretSlots, MidSlots = c.MidSlots, LowSlots = c.LowSlots,
                WebSlots = 0, DisruptorSlots = 0, ClawSlots = 0, DroneSlots = 0,
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
