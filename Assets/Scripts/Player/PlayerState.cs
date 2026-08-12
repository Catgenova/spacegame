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
    /// Everything the player owns and is: credits, skills, hangar, and the
    /// current ship (hull, fitting, cargo, HP pools). Plain data — no Unity
    /// scene objects — so it can be saved/loaded wholesale.
    /// </summary>
    public class PlayerState
    {
        public long Credits;
        public string ActiveSkill = "mining";
        public readonly Dictionary<string, SkillState> Skills = new Dictionary<string, SkillState>();
        public readonly List<string> Hangar = new List<string>();

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
