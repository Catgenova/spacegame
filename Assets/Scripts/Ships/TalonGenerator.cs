using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// The Talon ship line: cruiser-weight drone carriers defined by
    /// 10-digit body hashes, avian where Hive is wasp. Talons fight at
    /// arm's length — drones do the killing while the hull keeps station.
    /// Class 1 "Kestrel": 2 drone hardpoints, 1 turret, 2 mids, 2 lows.
    /// Streams: talondef / talonbody / talonpanels.
    /// </summary>
    public static class TalonGenerator
    {
        public const string TypeId = "talon";
        public const int MaxClass = 1;

        class TalonClass
        {
            public string Label, Doctrine;
            public int DroneSlots, TurretSlots, MidSlots, LowSlots;
            public float ShieldMin, ShieldMax, ArmorMin, ArmorMax, HullMin, HullMax;
            public float SpeedMin, SpeedMax, TurnMin, TurnMax;
            public float CapMin, CapMax, RegenMin, RegenMax;
            public float CargoMin, CargoMax;
            public float PriceMin, PriceMax;
            public long Fee;
            public Dictionary<string, float> Materials;
        }

        static readonly Dictionary<int, TalonClass> Classes = new Dictionary<int, TalonClass>
        {
            [1] = new TalonClass
            {
                Label = "Talon-class Kestrel (C1)",
                Doctrine = "Drone cruiser: loose the flock, hold station, let the talons feed.",
                DroneSlots = 2, TurretSlots = 1, MidSlots = 2, LowSlots = 2,
                ShieldMin = 240, ShieldMax = 300, ArmorMin = 170, ArmorMax = 220,
                HullMin = 180, HullMax = 230,
                SpeedMin = 3.0f, SpeedMax = 3.6f, TurnMin = 80, TurnMax = 105,
                CapMin = 200, CapMax = 260, RegenMin = 13, RegenMax = 16,
                CargoMin = 180, CargoMax = 260,
                PriceMin = 260000, PriceMax = 330000,
                Fee = 65000,
                Materials = new Dictionary<string, float>
                {
                    ["tritanium"] = 1000f, ["pyerite"] = 560f, ["mexallon"] = 220f, ["isogen"] = 95f,
                },
            },
        };

        static readonly string[] NamePool =
            { "Kestrel", "Peregrine", "Goshawk", "Harrier", "Shrike", "Osprey", "Merlin", "Gyr" };

        static readonly string[] FeatureNames =
        {
            "Hollow-bone airframe (+8% velocity)",
            "Layered plumage shielding (+15% shield)",
            "Raptor-eye capacitor bank (+12% capacitor)",
            "Tail-feather vanes (+12% agility)",
            "Crop storage bays (+15% cargo)",
        };

        static readonly Dictionary<string, ShipDef> _cache = new Dictionary<string, ShipDef>();

        // ---------- ids ----------

        public static string IdFromHash(string hash, int cls) => TypeId + cls + "-" + hash;

        public static bool IsTalonId(string id)
            => id != null && id.Length > TypeId.Length + 2 && id.StartsWith(TypeId)
               && id[TypeId.Length] >= '1' && id[TypeId.Length] <= '9'
               && id[TypeId.Length + 1] == '-';

        public static int ClassFromId(string id) => id[TypeId.Length] - '0';

        public static string HashFromId(string id) => id.Substring(TypeId.Length + 2);

        static string DefKey(int cls, string hash)
            => cls == 1 ? "talondef:" + hash : "talon" + cls + "def:" + hash;

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
                Desc = "One-off cruiser hull, body " + hash + ". " + c.Doctrine,
                Role = c.Doctrine,
                BodyHash = hash,
                Price = (long)R(c.PriceMin, c.PriceMax),
                Cargo = Mathf.Round(R(c.CargoMin, c.CargoMax)),
                HighSlots = c.TurretSlots, MidSlots = c.MidSlots, LowSlots = c.LowSlots,
                WebSlots = 0, DisruptorSlots = 0, ClawSlots = 0, DroneSlots = c.DroneSlots,
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
