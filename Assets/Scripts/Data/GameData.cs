using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    public enum SlotType { High, Mid, Low, Web, Disruptor }
    public enum ModuleKind { Miner, Weapon, ShieldBooster, Afterburner, Passive, Web, Disruptor }

    /// <summary>All fittable slot categories, in display/rack order.</summary>
    public static class Slots
    {
        public static readonly SlotType[] All = { SlotType.High, SlotType.Mid, SlotType.Low, SlotType.Web, SlotType.Disruptor };
    }
    public enum ObjKind { Sun, Planet, Belt, Station, Gate, Asteroid, Npc, Wreck }

    /// <summary>A tradable commodity: raw ore (refinable) or a mineral.</summary>
    public class OreDef
    {
        public string Id, Name;
        public float PricePerM3;
        public Color Color;
        public Dictionary<string, float> RefineInto; // mineral id -> fraction (ores only)
    }

    public class LootTable
    {
        public float Chance;   // probability the wreck contains anything
        public int MaxItems;
        public string[] Pool;  // module ids
    }

    public class ShipDef
    {
        public string Id, Name, Class, Desc;
        public long Price;
        public float Cargo;
        public int HighSlots, MidSlots, LowSlots, WebSlots, DisruptorSlots;
        public float Shield, Armor, Hull;
        public float Cap, CapRegen;
        public float Speed;        // units/s (1 unit = 100 m)
        public float Turn;         // deg/s
        public float MiningBonus;  // multiplier on mining yield
        public bool TurretOnly;    // high slots accept weapons only (Hive hardpoints)
        public string Role;        // generated ships: doctrine line
        public string[] Features;  // generated ships: rolled trait descriptions
        public string BodyHash;    // generated ships: the 10-digit body identity
    }

    /// <summary>
    /// A lootable ship blueprint: a 10-digit hash that deterministically
    /// defines one unique body (stats + generated mesh). Rarity sets how many
    /// hulls it can produce; when the runs are spent, the design is gone
    /// forever.
    /// </summary>
    public class Blueprint
    {
        public string Hash;   // 10 digits — the body's identity
        public string TypeId; // "hive"
        public int Class;     // 1..n
        public int Rarity;    // index into RarityNames/RarityRuns
        public int RunsLeft;
    }

    public class ModuleDef
    {
        public string Id, Name, Short, Desc;
        public SlotType Slot;
        public ModuleKind Kind;
        public long Price;
        public float Cycle, Range, CapUse;
        public float Yield;       // miner: m3/cycle
        public float Dmg;         // weapon: damage/cycle
        public float Tracking;    // weapon: rad/s of target motion it can follow
        public float BoostAmount; // shield booster: hp/cycle
        public float SpeedMult;   // afterburner
        public float CargoBonus, ArmorBonus, CapBonus; // passives
    }

    public class NpcDef
    {
        public string Id, Name;
        public float Shield, Armor, Hull;
        public float Dmg, Cycle, Range, Engage, Speed, Orbit;
        public float Tracking;     // rad/s — orbit fast and close to make big guns miss
        public long Bounty;
        public float StandingGain; // faction standing awarded per kill
        public bool NeverFlees;    // overlords fight to the death
        public float BpChance;     // chance a wreck contains a ship blueprint
    }

    public class SkillDef
    {
        public string Id, Name, Desc;
    }

    /// <summary>
    /// All static game data, defined in code so the project needs no serialized
    /// assets. World scale: 1 Unity unit = 100 m (so 150 units = 15 km).
    /// </summary>
    public static class GameData
    {
        public const int SkillMaxLevel = 5;
        public const float SkillXpRate = 5f;    // xp/second of training
        public const float DockRange = 400f;    // 40 km
        public const float JumpRange = 400f;
        public const float MinWarpDist = 1500f;
        public const float UnitsToKm = 0.1f;
        public const float UnitsToMs = 100f;    // units/s -> m/s
        public const float ModuleCargoVolume = 5f; // m3 a salvaged module occupies
        public const float BaseRefineYield = 0.66f;
        public const float RefineYieldPerLevel = 0.045f;

        /// <summary>XP required to go from `level` to `level + 1`.</summary>
        public static float XpForLevel(int level) => 300f * Mathf.Pow(4f, level);

        public static readonly Dictionary<string, OreDef> Ores = new Dictionary<string, OreDef>();
        public static readonly Dictionary<string, OreDef> Minerals = new Dictionary<string, OreDef>();
        public static readonly Dictionary<string, LootTable> Loot = new Dictionary<string, LootTable>();
        public static readonly Dictionary<string, ShipDef> Ships = new Dictionary<string, ShipDef>();
        public static readonly Dictionary<string, ModuleDef> Modules = new Dictionary<string, ModuleDef>();
        public static readonly Dictionary<string, NpcDef> Npcs = new Dictionary<string, NpcDef>();
        public static readonly Dictionary<string, SkillDef> Skills = new Dictionary<string, SkillDef>();

        static GameData()
        {
            Ore("veldspar", "Veldspar", 12f, new Color(0.69f, 0.63f, 0.54f));
            Ore("scordite", "Scordite", 18f, new Color(0.56f, 0.64f, 0.69f));
            Ore("plagioclase", "Plagioclase", 27f, new Color(0.50f, 0.69f, 0.54f));
            Ore("kernite", "Kernite", 42f, new Color(0.69f, 0.50f, 0.66f));
            Ore("omber", "Omber", 65f, new Color(0.82f, 0.70f, 0.42f));

            Mineral("tritanium", "Tritanium", 28f, new Color(0.75f, 0.78f, 0.82f));
            Mineral("pyerite", "Pyerite", 44f, new Color(0.85f, 0.55f, 0.4f));
            Mineral("mexallon", "Mexallon", 72f, new Color(0.45f, 0.75f, 0.8f));
            Mineral("isogen", "Isogen", 120f, new Color(0.55f, 0.9f, 0.55f));

            Ores["veldspar"].RefineInto = new Dictionary<string, float> { { "tritanium", 1f } };
            Ores["scordite"].RefineInto = new Dictionary<string, float> { { "tritanium", 0.65f }, { "pyerite", 0.35f } };
            Ores["plagioclase"].RefineInto = new Dictionary<string, float> { { "tritanium", 0.3f }, { "pyerite", 0.45f }, { "mexallon", 0.25f } };
            Ores["kernite"].RefineInto = new Dictionary<string, float> { { "pyerite", 0.35f }, { "mexallon", 0.45f }, { "isogen", 0.2f } };
            Ores["omber"].RefineInto = new Dictionary<string, float> { { "pyerite", 0.2f }, { "mexallon", 0.3f }, { "isogen", 0.5f } };

            Loot["rookie"] = new LootTable { Chance = 0.45f, MaxItems = 1, Pool = new[] { "blaster1", "miner1", "afterburner1" } };
            Loot["marauder"] = new LootTable { Chance = 0.75f, MaxItems = 1, Pool = new[] { "rail1", "shieldboost1", "plate1", "cargo1" } };
            Loot["overlord"] = new LootTable { Chance = 1f, MaxItems = 2, Pool = new[] { "rail2", "miner2", "capbattery1", "plate1", "shieldboost1" } };
            Loot["convoyhauler"] = new LootTable { Chance = 1f, MaxItems = 3, Pool = new[] { "rail2", "miner2", "shieldboost1", "capbattery1", "cargo1" } };

            Ships["wasp"] = new ShipDef
            {
                Id = "wasp", Name = "Wasp", Class = "Rookie Frigate", Price = 8000,
                Desc = "Issued free to capsuleers who lose everything.",
                Cargo = 120, HighSlots = 2, MidSlots = 1, LowSlots = 1,
                Shield = 150, Armor = 110, Hull = 130, Cap = 120, CapRegen = 6,
                Speed = 3.2f, Turn = 90f, MiningBonus = 1f,
            };
            Ships["prospector"] = new ShipDef
            {
                Id = "prospector", Name = "Prospector", Class = "Mining Frigate", Price = 52000,
                Desc = "Purpose-built ore harvester. +60% mining yield.",
                Cargo = 450, HighSlots = 2, MidSlots = 2, LowSlots = 2,
                Shield = 220, Armor = 160, Hull = 200, Cap = 180, CapRegen = 8,
                Speed = 2.5f, Turn = 70f, MiningBonus = 1.6f,
            };
            Ships["talon"] = new ShipDef
            {
                Id = "talon", Name = "Talon", Class = "Destroyer", Price = 140000,
                Desc = "A gun platform with an ego. Four hardpoints and a grudge.",
                Cargo = 250, HighSlots = 4, MidSlots = 2, LowSlots = 2,
                Shield = 350, Armor = 300, Hull = 320, Cap = 240, CapRegen = 10,
                Speed = 3.0f, Turn = 80f, MiningBonus = 1f,
            };
            Ships["mule"] = new ShipDef
            {
                Id = "mule", Name = "Mule", Class = "Hauler", Price = 300000,
                Desc = "A warehouse that reluctantly agreed to fly.",
                Cargo = 3200, HighSlots = 1, MidSlots = 2, LowSlots = 3,
                Shield = 400, Armor = 450, Hull = 600, Cap = 200, CapRegen = 8,
                Speed = 1.6f, Turn = 50f, MiningBonus = 1f,
            };
            Ships["aurora"] = new ShipDef
            {
                Id = "aurora", Name = "Aurora", Class = "Cruiser", Price = 950000,
                Desc = "Top of the line. The pirates of Abyss know its silhouette.",
                Cargo = 600, HighSlots = 5, MidSlots = 3, LowSlots = 3,
                Shield = 900, Armor = 800, Hull = 850, Cap = 500, CapRegen = 20,
                Speed = 2.8f, Turn = 65f, MiningBonus = 1.2f,
            };

            Modules["miner1"] = new ModuleDef
            {
                Id = "miner1", Name = "Miner I", Short = "MIN I", Slot = SlotType.High,
                Kind = ModuleKind.Miner, Price = 9000, Cycle = 3f, Yield = 9f, Range = 150f, CapUse = 4f,
                Desc = "Basic mining laser. 9 m3 per 3s cycle, 15 km range.",
            };
            Modules["miner2"] = new ModuleDef
            {
                Id = "miner2", Name = "Miner II", Short = "MIN II", Slot = SlotType.High,
                Kind = ModuleKind.Miner, Price = 46000, Cycle = 3f, Yield = 16f, Range = 180f, CapUse = 6f,
                Desc = "Improved mining laser. 16 m3 per 3s cycle, 18 km range.",
            };
            Modules["blaster1"] = new ModuleDef
            {
                Id = "blaster1", Name = "Light Blaster", Short = "BLAS", Slot = SlotType.High,
                Kind = ModuleKind.Weapon, Price = 13000, Cycle = 2f, Dmg = 15f, Range = 70f, CapUse = 3f,
                Tracking = 0.40f,
                Desc = "Close-range plasma cannon. High damage, excellent tracking.",
            };
            Modules["rail1"] = new ModuleDef
            {
                Id = "rail1", Name = "Light Railgun", Short = "RAIL", Slot = SlotType.High,
                Kind = ModuleKind.Weapon, Price = 17000, Cycle = 2.5f, Dmg = 11f, Range = 280f, CapUse = 4f,
                Tracking = 0.07f,
                Desc = "Long-range sniper. Struggles against fast close orbiters.",
            };
            Modules["rail2"] = new ModuleDef
            {
                Id = "rail2", Name = "Medium Railgun", Short = "RAIL+", Slot = SlotType.High,
                Kind = ModuleKind.Weapon, Price = 68000, Cycle = 3f, Dmg = 24f, Range = 360f, CapUse = 7f,
                Tracking = 0.045f,
                Desc = "Cruiser-grade railgun. Serious reach, poor tracking.",
            };
            Modules["shieldboost1"] = new ModuleDef
            {
                Id = "shieldboost1", Name = "Shield Booster I", Short = "SBST", Slot = SlotType.Mid,
                Kind = ModuleKind.ShieldBooster, Price = 22000, Cycle = 3f, BoostAmount = 30f, CapUse = 10f,
                Desc = "Active shield repair. 30 shield per 3s cycle.",
            };
            Modules["afterburner1"] = new ModuleDef
            {
                Id = "afterburner1", Name = "Afterburner I", Short = "AB", Slot = SlotType.Mid,
                Kind = ModuleKind.Afterburner, Price = 16000, Cycle = 1f, SpeedMult = 1.8f, CapUse = 2f,
                Desc = "While running: +80% max speed. Drains capacitor.",
            };
            Modules["cargo1"] = new ModuleDef
            {
                Id = "cargo1", Name = "Cargo Expander I", Short = "CRG+", Slot = SlotType.Low,
                Kind = ModuleKind.Passive, Price = 11000, CargoBonus = 160f,
                Desc = "Passive: +160 m3 cargo capacity.",
            };
            Modules["plate1"] = new ModuleDef
            {
                Id = "plate1", Name = "Armor Plate I", Short = "ARM+", Slot = SlotType.Low,
                Kind = ModuleKind.Passive, Price = 19000, ArmorBonus = 220f,
                Desc = "Passive: +220 armor HP.",
            };
            Modules["capbattery1"] = new ModuleDef
            {
                Id = "capbattery1", Name = "Cap Battery I", Short = "CAP+", Slot = SlotType.Low,
                Kind = ModuleKind.Passive, Price = 17000, CapBonus = 70f,
                Desc = "Passive: +70 capacitor.",
            };
            Modules["web1"] = new ModuleDef
            {
                Id = "web1", Name = "Stasis Webifier I", Short = "WEB", Slot = SlotType.Web,
                Kind = ModuleKind.Web, Price = 14000, Cycle = 2f, Range = 110f, CapUse = 3f,
                Desc = "Halves the target's velocity while active. Fleeing pirates hate it. Requires a web slot (Hive hulls).",
            };
            Modules["disrupt1"] = new ModuleDef
            {
                Id = "disrupt1", Name = "Warp Disruptor I", Short = "DISR", Slot = SlotType.Disruptor,
                Kind = ModuleKind.Disruptor, Price = 26000, Cycle = 2f, Range = 130f, CapUse = 4f,
                Desc = "Jams the target's warp drive — a pointed pirate aligns out but can never jump. Requires a disruptor slot (Hive interdictors).",
            };

            Npcs["rookie"] = new NpcDef
            {
                Id = "rookie", Name = "Pirate Rookie",
                Shield = 90, Armor = 70, Hull = 70,
                Dmg = 7, Cycle = 2.5f, Range = 100f, Engage = 700f, Speed = 2.8f, Orbit = 60f,
                Tracking = 0.30f, Bounty = 3500, StandingGain = 0.04f, BpChance = 0.04f,
            };
            Npcs["marauder"] = new NpcDef
            {
                Id = "marauder", Name = "Pirate Marauder",
                Shield = 220, Armor = 180, Hull = 160,
                Dmg = 16, Cycle = 2.8f, Range = 160f, Engage = 900f, Speed = 2.6f, Orbit = 100f,
                Tracking = 0.13f, Bounty = 11000, StandingGain = 0.1f, BpChance = 0.1f,
            };
            Npcs["overlord"] = new NpcDef
            {
                Id = "overlord", Name = "Pirate Overlord",
                Shield = 500, Armor = 420, Hull = 380,
                Dmg = 34, Cycle = 3.2f, Range = 240f, Engage = 1200f, Speed = 2.2f, Orbit = 140f,
                Tracking = 0.055f, Bounty = 38000, StandingGain = 0.25f, NeverFlees = true,
                BpChance = 0.25f,
            };
            Npcs["convoyhauler"] = new NpcDef
            {
                Id = "convoyhauler", Name = "Convoy Hauler",
                Shield = 700, Armor = 800, Hull = 900,
                Dmg = 8, Cycle = 3f, Range = 90f, Engage = 500f, Speed = 1.2f, Orbit = 220f,
                Tracking = 0.2f, Bounty = 60000, StandingGain = 0.3f, BpChance = 0.6f,
            };

            Skill("mining", "Mining", "+5% mining laser yield per level.");
            Skill("gunnery", "Gunnery", "+5% turret damage per level.");
            Skill("engineering", "Engineering", "+5% capacitor amount and recharge per level.");
            Skill("navigation", "Navigation", "+5% max velocity per level.");
            Skill("trade", "Trade", "2% better market prices per level.");
            Skill("refining", "Refining", "+4.5% refinery yield per level (base 66%).");
        }

        static void Ore(string id, string name, float price, Color c)
            => Ores[id] = new OreDef { Id = id, Name = name, PricePerM3 = price, Color = c };

        static void Mineral(string id, string name, float price, Color c)
            => Minerals[id] = new OreDef { Id = id, Name = name, PricePerM3 = price, Color = c };

        // ---- blueprints ----

        public static readonly string[] RarityNames = { "Common", "Uncommon", "Rare", "Pristine" };
        public static readonly int[] RarityRuns = { 1, 2, 3, 5 };

        /// <summary>Resolve any hull id: the static catalog or a generated body.</summary>
        public static ShipDef ResolveShip(string id)
        {
            if (Ships.TryGetValue(id, out var def)) return def;
            return ShipGen.ResolveGenerated(id);
        }

        public static bool ShipExists(string id)
            => Ships.ContainsKey(id) || ShipGen.IsGeneratedId(id);

        // ---- faction standing (with the Frontier Authority) ----
        // Kills raise it; tiers grant better mission pay and cheaper repairs.

        public static string StandingTier(float s)
            => s >= 6f ? "Legend" : s >= 3f ? "Honored" : s >= 1f ? "Trusted" : "Neutral";

        public static float StandingRewardBonus(float s)
            => s >= 6f ? 0.15f : s >= 3f ? 0.10f : s >= 1f ? 0.05f : 0f;

        public static float StandingRepairDiscount(float s)
            => s >= 6f ? 0.5f : s >= 3f ? 0.35f : s >= 1f ? 0.2f : 0f;

        public static string FmtTime(float seconds)
        {
            int t = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return (t / 60) + ":" + (t % 60).ToString("00");
        }

        /// <summary>Look up any tradable commodity (ore or mineral).</summary>
        public static OreDef Commodity(string id)
            => Ores.TryGetValue(id, out var o) ? o : Minerals[id];

        public static bool CommodityExists(string id)
            => Ores.ContainsKey(id) || Minerals.ContainsKey(id);

        static void Skill(string id, string name, string desc)
            => Skills[id] = new SkillDef { Id = id, Name = name, Desc = desc };

        public static string FmtCredits(long n) => n.ToString("N0") + " cr";

        public static string FmtDist(float units)
        {
            float km = units * UnitsToKm;
            if (km < 1f) return Mathf.RoundToInt(km * 1000f) + " m";
            if (km < 10000f) return km.ToString("0.0") + " km";
            return (km / 1000f).ToString("0.0") + " Mm";
        }
    }
}
