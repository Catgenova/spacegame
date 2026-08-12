using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    public enum SlotType { High, Mid, Low }
    public enum ModuleKind { Miner, Weapon, ShieldBooster, Afterburner, Passive }
    public enum ObjKind { Sun, Planet, Belt, Station, Gate, Asteroid, Npc }

    public class OreDef
    {
        public string Id, Name;
        public float PricePerM3;
        public Color Color;
    }

    public class ShipDef
    {
        public string Id, Name, Class, Desc;
        public long Price;
        public float Cargo;
        public int HighSlots, MidSlots, LowSlots;
        public float Shield, Armor, Hull;
        public float Cap, CapRegen;
        public float Speed;       // units/s (1 unit = 100 m)
        public float Turn;        // deg/s
        public float MiningBonus; // multiplier on mining yield
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
        public float BoostAmount; // shield booster: hp/cycle
        public float SpeedMult;   // afterburner
        public float CargoBonus, ArmorBonus, CapBonus; // passives
    }

    public class NpcDef
    {
        public string Id, Name;
        public float Shield, Armor, Hull;
        public float Dmg, Cycle, Range, Engage, Speed, Orbit;
        public long Bounty;
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

        /// <summary>XP required to go from `level` to `level + 1`.</summary>
        public static float XpForLevel(int level) => 300f * Mathf.Pow(4f, level);

        public static readonly Dictionary<string, OreDef> Ores = new Dictionary<string, OreDef>();
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
                Desc = "Close-range plasma cannon. High damage, short reach.",
            };
            Modules["rail1"] = new ModuleDef
            {
                Id = "rail1", Name = "Light Railgun", Short = "RAIL", Slot = SlotType.High,
                Kind = ModuleKind.Weapon, Price = 17000, Cycle = 2.5f, Dmg = 11f, Range = 280f, CapUse = 4f,
                Desc = "Long-range kinetic sniper for keeping pirates honest.",
            };
            Modules["rail2"] = new ModuleDef
            {
                Id = "rail2", Name = "Medium Railgun", Short = "RAIL+", Slot = SlotType.High,
                Kind = ModuleKind.Weapon, Price = 68000, Cycle = 3f, Dmg = 24f, Range = 360f, CapUse = 7f,
                Desc = "Cruiser-grade railgun. Serious reach, serious holes.",
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

            Npcs["rookie"] = new NpcDef
            {
                Id = "rookie", Name = "Pirate Rookie",
                Shield = 90, Armor = 70, Hull = 70,
                Dmg = 7, Cycle = 2.5f, Range = 100f, Engage = 700f, Speed = 2.8f, Orbit = 60f,
                Bounty = 3500,
            };
            Npcs["marauder"] = new NpcDef
            {
                Id = "marauder", Name = "Pirate Marauder",
                Shield = 220, Armor = 180, Hull = 160,
                Dmg = 16, Cycle = 2.8f, Range = 160f, Engage = 900f, Speed = 2.6f, Orbit = 100f,
                Bounty = 11000,
            };
            Npcs["overlord"] = new NpcDef
            {
                Id = "overlord", Name = "Pirate Overlord",
                Shield = 500, Armor = 420, Hull = 380,
                Dmg = 34, Cycle = 3.2f, Range = 240f, Engage = 1200f, Speed = 2.2f, Orbit = 140f,
                Bounty = 38000,
            };

            Skill("mining", "Mining", "+5% mining laser yield per level.");
            Skill("gunnery", "Gunnery", "+5% turret damage per level.");
            Skill("engineering", "Engineering", "+5% capacitor amount and recharge per level.");
            Skill("navigation", "Navigation", "+5% max velocity per level.");
            Skill("trade", "Trade", "2% better market prices per level.");
        }

        static void Ore(string id, string name, float price, Color c)
            => Ores[id] = new OreDef { Id = id, Name = name, PricePerM3 = price, Color = c };

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
