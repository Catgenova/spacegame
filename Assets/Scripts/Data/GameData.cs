using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    public enum SlotType { High, Mid, Low, Web, Disruptor, Claw, Drone, Sensor, Collector }
    public enum ModuleKind { Miner, Weapon, ShieldBooster, Afterburner, Passive, Web, Disruptor, Claw, Drone, Sensor, Collector }

    /// <summary>All fittable slot categories, in display/rack order.</summary>
    public static class Slots
    {
        public static readonly SlotType[] All = { SlotType.High, SlotType.Mid, SlotType.Low, SlotType.Web, SlotType.Disruptor, SlotType.Claw, SlotType.Drone, SlotType.Sensor, SlotType.Collector };
    }
    public enum ObjKind { Sun, Planet, Belt, Station, Gate, Asteroid, Npc, Wreck, Site, Container }

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
        public int HighSlots, MidSlots, LowSlots, WebSlots, DisruptorSlots, ClawSlots, DroneSlots, SensorSlots, CollectorSlots;
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
        public string TypeId; // "hive" ... or "mod" for a module blueprint
        public int Class;     // 1..n (ships only)
        public int Rarity;    // index into RarityNames
        public int RunsLeft;
        /// <summary>Set only on module blueprints: the base module printed.</summary>
        public string ModuleId;

        public bool IsModule => !string.IsNullOrEmpty(ModuleId);
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
        public int ScrapClass;     // 1..3 — grade of scrap this hull leaves
        public float ScrapMin, ScrapMax; // m3 of scrap in the wreck
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
        /// <summary>Combat salvage, graded by the class of hull it came off.
        /// Sells well but takes hold space — the reason to loot a kill.</summary>
        public static readonly Dictionary<string, OreDef> Scraps = new Dictionary<string, OreDef>();
        /// <summary>Exotic metals found only in deep-space anomalies. They cannot
        /// be mined or refined from anything — exploration is the sole supply, and
        /// the best blueprints will not run without them.</summary>
        public static readonly Dictionary<string, OreDef> Exotics = new Dictionary<string, OreDef>();
        public static readonly Dictionary<string, LootTable> Loot = new Dictionary<string, LootTable>();
        public static readonly Dictionary<string, ShipDef> Ships = new Dictionary<string, ShipDef>();
        public static readonly Dictionary<string, ModuleDef> Modules = new Dictionary<string, ModuleDef>();
        public static readonly Dictionary<string, NpcDef> Npcs = new Dictionary<string, NpcDef>();
        public static readonly Dictionary<string, SkillDef> Skills = new Dictionary<string, SkillDef>();

        static GameData()
        {
            // Ores are real rock, coloured as they actually look in hand, and
            // each refines into the metals its real chemistry contains.
            Ore("hematite", "Hematite", 12f, new Color(0.62f, 0.36f, 0.30f));
            Ore("pyroxene", "Pyroxene", 18f, new Color(0.42f, 0.48f, 0.44f));
            Ore("plagioclase", "Plagioclase", 27f, new Color(0.78f, 0.76f, 0.72f));
            Ore("ilmenite", "Ilmenite", 42f, new Color(0.32f, 0.33f, 0.36f));
            Ore("beryl", "Beryl", 65f, new Color(0.36f, 0.72f, 0.56f));

            // Refined metals, in the order a real spaceframe uses them:
            // steel structure, aluminium airframe, titanium pressure hulls,
            // beryllium for precision optics and stiff lightweight structure.
            Mineral("iron", "Iron", 28f, new Color(0.72f, 0.74f, 0.78f));
            Mineral("aluminium", "Aluminium", 44f, new Color(0.86f, 0.88f, 0.92f));
            Mineral("titanium", "Titanium", 72f, new Color(0.45f, 0.70f, 0.80f));
            Mineral("beryllium", "Beryllium", 120f, new Color(0.74f, 0.79f, 0.72f));

            // Wreck salvage. Class N scrap comes off a Class N hull, so the
            // grade you recover tracks how hard the target was to kill.
            Scrap("scrap1", "Class 1 Scrap", 45f, new Color(0.62f, 0.60f, 0.56f));
            Scrap("scrap2", "Class 2 Scrap", 130f, new Color(0.66f, 0.58f, 0.44f));
            Scrap("scrap3", "Class 3 Scrap", 380f, new Color(0.70f, 0.56f, 0.32f));

            // Scrap is compacted hulk: one m3 of it mills out into several m3 of
            // usable stock, so hauling scrap is far denser than hauling metal.
            // Ratios are set so refining roughly matches the raw sale value at
            // base skill and clearly beats it once trained — the same deal ore
            // gets. Crucially this is the only route to titanium and beryllium
            // that does not involve mining low-sec belts.
            Scraps["scrap1"].RefineInto = new Dictionary<string, float>
                { { "iron", 1.00f }, { "aluminium", 0.90f } };
            Scraps["scrap2"].RefineInto = new Dictionary<string, float>
                { { "iron", 1.40f }, { "aluminium", 1.50f }, { "titanium", 1.20f } };
            Scraps["scrap3"].RefineInto = new Dictionary<string, float>
                { { "iron", 2.00f }, { "aluminium", 2.20f }, { "titanium", 2.40f }, { "beryllium", 1.80f } };

            // Anomaly exotics, in the order real aerospace reaches for them:
            // tantalum for capacitor foil, hafnium for superalloys and control
            // surfaces, rhenium for rocket-nozzle throats. All three are real
            // metals used in genuinely hard spacecraft engineering, and none can
            // be had by mining — you fly out and take them.
            // Priced so a cleared field pays about what a comparable combat
            // target does. The prize is access, not resale: nothing else in the
            // game can supply these at all.
            Exotic("tantalum", "Tantalum", 240f, new Color(0.55f, 0.56f, 0.62f));
            Exotic("hafnium", "Hafnium", 520f, new Color(0.62f, 0.66f, 0.72f));
            Exotic("rhenium", "Rhenium", 1100f, new Color(0.78f, 0.80f, 0.86f));

            Ores["hematite"].RefineInto = new Dictionary<string, float> { { "iron", 1f } };
            Ores["pyroxene"].RefineInto = new Dictionary<string, float> { { "iron", 0.65f }, { "aluminium", 0.35f } };
            Ores["plagioclase"].RefineInto = new Dictionary<string, float> { { "iron", 0.3f }, { "aluminium", 0.45f }, { "titanium", 0.25f } };
            Ores["ilmenite"].RefineInto = new Dictionary<string, float> { { "aluminium", 0.35f }, { "titanium", 0.45f }, { "beryllium", 0.2f } };
            Ores["beryl"].RefineInto = new Dictionary<string, float> { { "aluminium", 0.2f }, { "titanium", 0.3f }, { "beryllium", 0.5f } };

            // Pirates never carry salvageable gear — their wrecks yield graded
            // scrap and, rarely, a blueprint chip. Modules come from drifting
            // caches turned up by a sensor sweep, or from the market.
            // Drifting caches are the only place fittable gear is simply found.
            // Since no station sells modules, the pool has to cover the basics —
            // a mining laser and a gun — or a pilot who loses a fit has no way
            // back that does not depend on a blueprint drop.
            Loot["cache"] = new LootTable
            {
                Chance = 1f, MaxItems = 2,
                Pool = new[] { "miner1", "blaster1", "rail1", "shieldboost1",
                               "afterburner1", "cargo1", "plate1", "capbattery1" },
            };

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
            Modules["claw1"] = new ModuleDef
            {
                Id = "claw1", Name = "Mining Claw I", Short = "CLAW", Slot = SlotType.Claw,
                Kind = ModuleKind.Claw, Price = 21000, Cycle = 3f, Yield = 26f, Range = 10f, CapUse = 5f,
                Desc = "Hydraulic rock claw. 26 m3 per 3s cycle, but you must be ON the rock (0 km). Requires a claw hardpoint (Claw hulls).",
            };
            Modules["sensor1"] = new ModuleDef
            {
                Id = "sensor1", Name = "Pathfinder Array I", Short = "SNSR", Slot = SlotType.Sensor,
                Kind = ModuleKind.Sensor, Price = 28000, Cycle = 10f, CapUse = 12f,
                Desc = "Deep-space sweep. Every 10s cycle listens for salvage signatures — sometimes a drifting cache turns up nearby. Requires a sensor hardpoint (Trail hulls).",
            };
            Modules["collector1"] = new ModuleDef
            {
                Id = "collector1", Name = "Salvage Collector I", Short = "COLL", Slot = SlotType.Collector,
                Kind = ModuleKind.Collector, Price = 23000, Cycle = 4f, Range = 160f, CapUse = 5f,
                Desc = "Tractor scoop. Reels salvage out of a targeted wreck from 16 km, one piece per 4s cycle. Requires a collector hardpoint (Trail hulls).",
            };
            // ---- Tech II tier: printed from blueprints, not stocked cheap ----
            Modules["claw2"] = new ModuleDef
            {
                Id = "claw2", Name = "Mining Claw II", Short = "CLAW II", Slot = SlotType.Claw,
                Kind = ModuleKind.Claw, Price = 74000, Cycle = 3f, Yield = 44f, Range = 14f, CapUse = 7f,
                Desc = "Heavy hydraulic rock claw. 44 m3 per 3s cycle at point-blank range. Requires a claw hardpoint.",
            };
            Modules["web2"] = new ModuleDef
            {
                Id = "web2", Name = "Stasis Webifier II", Short = "WEB II", Slot = SlotType.Web,
                Kind = ModuleKind.Web, Price = 52000, Cycle = 2f, Range = 165f, CapUse = 4f,
                Desc = "Long-reach stasis field. Halves target velocity from 16.5 km. Requires a web slot.",
            };
            Modules["disrupt2"] = new ModuleDef
            {
                Id = "disrupt2", Name = "Warp Disruptor II", Short = "DISR II", Slot = SlotType.Disruptor,
                Kind = ModuleKind.Disruptor, Price = 88000, Cycle = 2f, Range = 200f, CapUse = 5f,
                Desc = "Extended warp jammer — nothing inside 20 km leaves the field. Requires a disruptor slot.",
            };
            Modules["sensor2"] = new ModuleDef
            {
                Id = "sensor2", Name = "Pathfinder Array II", Short = "SNSR II", Slot = SlotType.Sensor,
                Kind = ModuleKind.Sensor, Price = 96000, Cycle = 7f, CapUse = 14f,
                Desc = "Deep-space sweep on a 7s cycle — finds drifting caches far faster. Requires a sensor hardpoint.",
            };
            Modules["collector2"] = new ModuleDef
            {
                Id = "collector2", Name = "Salvage Collector II", Short = "COLL II", Slot = SlotType.Collector,
                Kind = ModuleKind.Collector, Price = 78000, Cycle = 2.5f, Range = 240f, CapUse = 6f,
                Desc = "Wide-aperture tractor scoop. Strips a wreck from 24 km on a 2.5s cycle. Requires a collector hardpoint.",
            };
            Modules["drone2"] = new ModuleDef
            {
                Id = "drone2", Name = "Drone Controller II", Short = "DRN II", Slot = SlotType.Drone,
                Kind = ModuleKind.Drone, Price = 84000, Cycle = 2f, Dmg = 22f, Range = 280f, CapUse = 3f,
                Tracking = 3f,
                Desc = "Heavier autonomous drone with a longer leash. Requires a drone hardpoint.",
            };
            Modules["shieldboost2"] = new ModuleDef
            {
                Id = "shieldboost2", Name = "Shield Booster II", Short = "SBST II", Slot = SlotType.Mid,
                Kind = ModuleKind.ShieldBooster, Price = 79000, Cycle = 2.5f, BoostAmount = 58f, CapUse = 15f,
                Desc = "Active shield repair. 58 shield per 2.5s cycle — thirsty but decisive.",
            };
            Modules["afterburner2"] = new ModuleDef
            {
                Id = "afterburner2", Name = "Afterburner II", Short = "AB II", Slot = SlotType.Mid,
                Kind = ModuleKind.Afterburner, Price = 58000, Cycle = 1f, SpeedMult = 2.3f, CapUse = 3f,
                Desc = "While running: +130% max speed.",
            };
            Modules["cargo2"] = new ModuleDef
            {
                Id = "cargo2", Name = "Cargo Expander II", Short = "CRG++", Slot = SlotType.Low,
                Kind = ModuleKind.Passive, Price = 44000, CargoBonus = 400f,
                Desc = "Passive: +400 m3 cargo capacity.",
            };
            Modules["plate2"] = new ModuleDef
            {
                Id = "plate2", Name = "Armor Plate II", Short = "ARM++", Slot = SlotType.Low,
                Kind = ModuleKind.Passive, Price = 68000, ArmorBonus = 560f,
                Desc = "Passive: +560 armor HP.",
            };
            Modules["capbattery2"] = new ModuleDef
            {
                Id = "capbattery2", Name = "Cap Battery II", Short = "CAP++", Slot = SlotType.Low,
                Kind = ModuleKind.Passive, Price = 61000, CapBonus = 185f,
                Desc = "Passive: +185 capacitor.",
            };

            Modules["drone1"] = new ModuleDef
            {
                Id = "drone1", Name = "Drone Controller I", Short = "DRN", Slot = SlotType.Drone,
                Kind = ModuleKind.Drone, Price = 24000, Cycle = 2.5f, Dmg = 12f, Range = 200f, CapUse = 2f,
                Tracking = 3f,
                Desc = "Launches an attack drone that hunts your locked target on its own — steady damage that ignores transversal. Requires a drone hardpoint (Talon hulls).",
            };

            Npcs["rookie"] = new NpcDef
            {
                Id = "rookie", Name = "Pirate Rookie",
                Shield = 90, Armor = 70, Hull = 70,
                Dmg = 7, Cycle = 2.5f, Range = 100f, Engage = 700f, Speed = 2.8f, Orbit = 60f,
                Tracking = 0.30f, Bounty = 3500, StandingGain = 0.04f, BpChance = 0.04f,
                ScrapClass = 1, ScrapMin = 6f, ScrapMax = 12f,
            };
            Npcs["marauder"] = new NpcDef
            {
                Id = "marauder", Name = "Pirate Marauder",
                Shield = 220, Armor = 180, Hull = 160,
                Dmg = 16, Cycle = 2.8f, Range = 160f, Engage = 900f, Speed = 2.6f, Orbit = 100f,
                Tracking = 0.13f, Bounty = 11000, StandingGain = 0.1f, BpChance = 0.1f,
                ScrapClass = 2, ScrapMin = 10f, ScrapMax = 20f,
            };
            Npcs["overlord"] = new NpcDef
            {
                Id = "overlord", Name = "Pirate Overlord",
                Shield = 500, Armor = 420, Hull = 380,
                Dmg = 34, Cycle = 3.2f, Range = 240f, Engage = 1200f, Speed = 2.2f, Orbit = 140f,
                Tracking = 0.055f, Bounty = 38000, StandingGain = 0.25f, NeverFlees = true,
                ScrapClass = 3, ScrapMin = 18f, ScrapMax = 32f,
                BpChance = 0.25f,
            };
            Npcs["convoyhauler"] = new NpcDef
            {
                Id = "convoyhauler", Name = "Convoy Hauler",
                Shield = 700, Armor = 800, Hull = 900,
                Dmg = 8, Cycle = 3f, Range = 90f, Engage = 500f, Speed = 1.2f, Orbit = 220f,
                Tracking = 0.2f, Bounty = 60000, StandingGain = 0.3f, BpChance = 0.6f,
                ScrapClass = 3, ScrapMin = 30f, ScrapMax = 50f,
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

        // Module blueprints run the other way round: a Common print is a
        // production line of plain gear, a Pristine print is one masterwork.
        public static readonly int[] ModBpRuns = { 5, 3, 2, 1 };
        public static readonly int[] ModBpMods = { 0, 1, 3, 5 };
        public static readonly int[] ModBpMatMult = { 1, 2, 3, 5 };

        /// <summary>Resolve any hull id: the static catalog or a generated body.</summary>
        public static ShipDef ResolveShip(string id)
        {
            if (Ships.TryGetValue(id, out var def)) return def;
            return ShipGen.ResolveGenerated(id);
        }

        public static bool ShipExists(string id)
            => Ships.ContainsKey(id) || ShipGen.IsGeneratedId(id);

        /// <summary>Look up a module by id, resolving crafted modules (which
        /// carry rolled modifiers) as well as the catalogue ones.</summary>
        public static ModuleDef ResolveModule(string id)
        {
            if (Modules.TryGetValue(id, out var def)) return def;
            return ModGen.Resolve(id);
        }

        public static bool ModuleExists(string id)
            => Modules.ContainsKey(id) || ModGen.Resolve(id) != null;

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
            => Ores.TryGetValue(id, out var o) ? o
             : Minerals.TryGetValue(id, out var m) ? m
             : Scraps.TryGetValue(id, out var s) ? s : Exotics[id];

        /// <summary>Anything that can be put through a station refinery: raw
        /// ore, or recovered scrap. Minerals are already refined.</summary>
        public static bool TryRefinable(string id, out OreDef def)
        {
            if (Ores.TryGetValue(id, out def) && def.RefineInto != null) return true;
            if (Scraps.TryGetValue(id, out def) && def.RefineInto != null) return true;
            def = null;
            return false;
        }

        public static bool CommodityExists(string id)
            => Ores.ContainsKey(id) || Minerals.ContainsKey(id)
               || Scraps.ContainsKey(id) || Exotics.ContainsKey(id);

        static void Exotic(string id, string name, float price, Color c)
            => Exotics[id] = new OreDef { Id = id, Name = name, PricePerM3 = price, Color = c };

        /// <summary>Which exotic an anomaly of this tier yields.</summary>
        public static string ExoticIdForTier(int tier)
            => tier >= 3 ? "rhenium" : tier == 2 ? "hafnium" : "tantalum";

        static void Scrap(string id, string name, float price, Color c)
            => Scraps[id] = new OreDef { Id = id, Name = name, PricePerM3 = price, Color = c };

        public static string ScrapIdForClass(int cls)
            => "scrap" + Mathf.Clamp(cls, 1, 3);

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
