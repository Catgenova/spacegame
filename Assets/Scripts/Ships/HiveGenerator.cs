using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// The Hive ship line: small, darty swarm ships — scouting, agile
    /// overwhelming, interdiction. Every body is defined by a 10-digit hash:
    /// the same hash always yields the same name, stats, rolled features, and
    /// generated mesh (see HiveShipMesh). Class definitions fix the slot
    /// doctrine; the hash rolls everything inside the class envelope.
    /// </summary>
    public static class HiveGenerator
    {
        public const string TypeId = "hive";

        class HiveClass
        {
            public int TurretSlots, WebSlots, LowSlots;
            public float ShieldMin, ShieldMax, ArmorMin, ArmorMax, HullMin, HullMax;
            public float SpeedMin, SpeedMax, TurnMin, TurnMax;
            public float CapMin, CapMax, RegenMin, RegenMax;
            public float CargoMin, CargoMax;
            public string Doctrine;
        }

        // Class envelopes. Class 1 per spec: 1 turret hardpoint, 1 web slot.
        static readonly Dictionary<int, HiveClass> Classes = new Dictionary<int, HiveClass>
        {
            [1] = new HiveClass
            {
                TurretSlots = 1, WebSlots = 1, LowSlots = 1,
                ShieldMin = 90, ShieldMax = 125, ArmorMin = 55, ArmorMax = 80,
                HullMin = 65, HullMax = 90,
                SpeedMin = 4.1f, SpeedMax = 4.8f, TurnMin = 150, TurnMax = 175,
                CapMin = 85, CapMax = 110, RegenMin = 7, RegenMax = 9.5f,
                CargoMin = 80, CargoMax = 130,
                Doctrine = "Swarm scout: dart in, web the target, let the swarm feed.",
            },
        };

        static readonly string[] NamePool =
            { "Hornet", "Vespa", "Stinger", "Dart", "Mantis", "Widow", "Naja", "Sicaria" };

        // Rolled traits: description + a stat mutation. Two per body.
        static readonly string[] FeatureNames =
        {
            "Overtuned thrusters (+8% velocity)",
            "Waxed chitin plating (+15% shield)",
            "Expanded capacitor coils (+12% capacitor)",
            "Gyroscopic thorax frame (+12% agility)",
            "Hollowed wing spars (+15% cargo)",
        };

        static readonly Dictionary<string, ShipDef> _cache = new Dictionary<string, ShipDef>();

        public static string IdFromHash(string hash) => TypeId + "1-" + hash;
        public static bool IsHiveId(string id) => id != null && id.StartsWith(TypeId + "1-");
        public static string HashFromId(string id) => id.Substring(TypeId.Length + 2);

        /// <summary>A fresh 10-digit body hash (leading zeros allowed).</summary>
        public static string NewHash()
        {
            var s = "";
            for (int i = 0; i < 10; i++) s += Random.Range(0, 10).ToString();
            return s;
        }

        public static Blueprint RollBlueprint()
        {
            float r = Random.value;
            int rarity = r < 0.6f ? 0 : r < 0.85f ? 1 : r < 0.97f ? 2 : 3;
            return new Blueprint
            {
                Hash = NewHash(),
                TypeId = TypeId,
                Class = 1,
                Rarity = rarity,
                RunsLeft = GameData.RarityRuns[rarity],
            };
        }

        /// <summary>Deterministic stat sheet for a body hash (cached).</summary>
        public static ShipDef Def(string hash)
        {
            if (_cache.TryGetValue(hash, out var cached)) return cached;
            var c = Classes[1];
            var rng = Rng.Stream("hivedef:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);

            var def = new ShipDef
            {
                Id = IdFromHash(hash),
                Name = NamePool[rng.Next(NamePool.Length)] + "-" + hash.Substring(0, 4),
                Class = "Hive-class Scout (C1)",
                Desc = "One-off swarm hull, body " + hash + ". " + Classes[1].Doctrine,
                Role = c.Doctrine,
                BodyHash = hash,
                Price = (long)R(55000, 78000), // trade-in valuation
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
                MiningBonus = 0.5f, // wasps do not mine well
            };

            // Two distinct rolled features mutate the sheet.
            int f1 = rng.Next(FeatureNames.Length);
            int f2 = (f1 + 1 + rng.Next(FeatureNames.Length - 1)) % FeatureNames.Length;
            def.Features = new[] { FeatureNames[f1], FeatureNames[f2] };
            ApplyFeature(def, f1);
            ApplyFeature(def, f2);

            _cache[hash] = def;
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

        /// <summary>Minerals (m3) + credit fee to manufacture one run.</summary>
        public static Dictionary<string, float> MaterialCost(int shipClass)
            => new Dictionary<string, float>
            {
                ["tritanium"] = 320f,
                ["pyerite"] = 180f,
                ["mexallon"] = 60f,
                ["isogen"] = 25f,
            };

        public const long ManufactureFee = 20000;

        public static string DescribeBlueprint(Blueprint bp)
        {
            var def = Def(bp.Hash);
            return def.Name + "  [" + GameData.RarityNames[bp.Rarity] + ", "
                + bp.RunsLeft + " run" + (bp.RunsLeft == 1 ? "" : "s") + " left]";
        }
    }
}
