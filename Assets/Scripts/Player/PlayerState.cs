using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    public class SkillState
    {
        public int Level;
        public float Xp;
    }

    public struct ShipStats
    {
        public float MaxShield, MaxArmor, MaxHull, MaxCap, CapRegen, CargoCap, Speed, Turn;
    }

    /// <summary>
    /// One station's storage bay. Capacity is unlimited, but the contents are
    /// only reachable while docked at that station — haul it or leave it.
    /// </summary>
    public class StationStore
    {
        public readonly Dictionary<string, float> Cargo = new Dictionary<string, float>();
        public readonly List<string> Modules = new List<string>();
        public readonly List<Blueprint> Blueprints = new List<Blueprint>();

        public bool IsEmpty => Cargo.Count == 0 && Modules.Count == 0 && Blueprints.Count == 0;

        public float TotalM3()
        {
            float sum = 0f;
            foreach (var v in Cargo.Values) sum += v;
            return sum + Modules.Count * GameData.ModuleCargoVolume;
        }

        public void AddCargo(string id, float m3)
        {
            if (m3 <= 0f) return;
            Cargo.TryGetValue(id, out var had);
            Cargo[id] = had + m3;
        }
    }

    /// <summary>
    /// Everything the player owns and is: credits, skills, per-station storage,
    /// and the current ship (hull, fitting, cargo, HP pools). Plain data — no
    /// Unity scene objects — so it can be saved/loaded wholesale.
    /// </summary>
    public class PlayerState
    {
        public long Credits;
        public string ActiveSkill = "mining";
        public readonly Dictionary<string, SkillState> Skills = new Dictionary<string, SkillState>();
        /// <summary>Storage bay per station id. Created on first use.</summary>
        public readonly Dictionary<string, StationStore> Stations = new Dictionary<string, StationStore>();

        public StationStore StoreAt(string stationId)
        {
            if (string.IsNullOrEmpty(stationId)) stationId = "solara_prime";
            if (!Stations.TryGetValue(stationId, out var s))
            {
                s = new StationStore();
                Stations[stationId] = s;
            }
            return s;
        }

        public string HullId;
        public readonly Dictionary<string, float> Cargo = new Dictionary<string, float>();
        public readonly List<string> CargoModules = new List<string>(); // salvaged modules in the hold
        public readonly List<Blueprint> Blueprints = new List<Blueprint>(); // looted ship blueprints
        public readonly Dictionary<SlotType, string[]> Fitting = new Dictionary<SlotType, string[]>();
        public float Shield, Armor, HullHp, Cap;
        public float ExtraCargo; // e.g. a courier mission package occupying the hold
        public float Standing;   // Frontier Authority faction standing (0..10)
        public bool ArcDone;     // "The Abyss Job" story arc completed

        public ShipDef Hull => GameData.ResolveShip(HullId);

        public static PlayerState NewGame()
        {
            var p = new PlayerState { Credits = 5000 };
            foreach (var id in GameData.Skills.Keys)
                p.Skills[id] = new SkillState();
            p.SetHull("wasp");
            p.Fitting[SlotType.High][0] = "miner1";
            p.Fitting[SlotType.High][1] = "blaster1";
            return p;
        }

        /// <summary>Switch hulls: resets fitting slots and refills HP/cap.</summary>
        public void SetHull(string hullId)
        {
            HullId = hullId;
            var def = GameData.ResolveShip(hullId);
            Fitting[SlotType.High] = new string[def.HighSlots];
            Fitting[SlotType.Mid] = new string[def.MidSlots];
            Fitting[SlotType.Low] = new string[def.LowSlots];
            Fitting[SlotType.Web] = new string[def.WebSlots];
            Fitting[SlotType.Disruptor] = new string[def.DisruptorSlots];
            Fitting[SlotType.Claw] = new string[def.ClawSlots];
            Fitting[SlotType.Drone] = new string[def.DroneSlots];
            Fitting[SlotType.Sensor] = new string[def.SensorSlots];
            Fitting[SlotType.Collector] = new string[def.CollectorSlots];
            var st = ComputeStats();
            Shield = st.MaxShield;
            Armor = st.MaxArmor;
            HullHp = st.MaxHull;
            Cap = st.MaxCap;
        }

        /// <summary>Derived stats from hull + low-slot passives + skills.</summary>
        public ShipStats ComputeStats()
        {
            var def = Hull;
            var st = new ShipStats
            {
                MaxShield = def.Shield, MaxArmor = def.Armor, MaxHull = def.Hull,
                MaxCap = def.Cap, CapRegen = def.CapRegen,
                CargoCap = def.Cargo, Speed = def.Speed, Turn = def.Turn,
            };
            foreach (var modId in Fitting[SlotType.Low])
            {
                if (string.IsNullOrEmpty(modId)) continue;
                var m = GameData.Modules[modId];
                st.MaxArmor += m.ArmorBonus;
                st.MaxCap += m.CapBonus;
                st.CargoCap += m.CargoBonus;
            }
            st.MaxCap *= 1f + 0.05f * SkillLevel("engineering");
            st.CapRegen *= 1f + 0.05f * SkillLevel("engineering");
            st.Speed *= 1f + 0.05f * SkillLevel("navigation");
            return st;
        }

        public float CargoUsed()
        {
            float sum = ExtraCargo + CargoModules.Count * GameData.ModuleCargoVolume;
            foreach (var v in Cargo.Values) sum += v;
            return sum;
        }

        public int SkillLevel(string id)
            => Skills.TryGetValue(id, out var s) ? s.Level : 0;

        /// <summary>Add xp to a skill; returns levels gained (usually 0).</summary>
        public int AddSkillXp(string id, float amount)
        {
            if (!Skills.TryGetValue(id, out var s) || s.Level >= GameData.SkillMaxLevel) return 0;
            s.Xp += amount;
            int gained = 0;
            while (s.Level < GameData.SkillMaxLevel && s.Xp >= GameData.XpForLevel(s.Level))
            {
                s.Xp -= GameData.XpForLevel(s.Level);
                s.Level++;
                gained++;
            }
            if (s.Level >= GameData.SkillMaxLevel) s.Xp = 0f;
            return gained;
        }

        public float SkillProgress(string id)
        {
            if (!Skills.TryGetValue(id, out var s) || s.Level >= GameData.SkillMaxLevel) return 1f;
            return Mathf.Clamp01(s.Xp / GameData.XpForLevel(s.Level));
        }
    }
}
