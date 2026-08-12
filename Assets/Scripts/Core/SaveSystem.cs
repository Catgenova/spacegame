using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// JSON save/load via PlayerPrefs. JsonUtility can't serialize
    /// dictionaries, so state is flattened into lists first.
    /// </summary>
    public static class SaveSystem
    {
        const string Key = "spacegame_save_v1";

        [System.Serializable] class SkillSave { public string Id; public int Level; public float Xp; }
        [System.Serializable] class OreSave { public string Id; public float Amount; }
        [System.Serializable] class FitSave { public int Slot; public int Index; public string ModId; }

        [System.Serializable]
        class SaveData
        {
            public int V = 2;
            public long Credits;
            public string ActiveSkill;
            public List<SkillSave> Skills = new List<SkillSave>();
            public List<string> Hangar = new List<string>();
            public string SystemId;
            public bool Docked;
            public string StationId;
            public string HullId;
            public List<OreSave> Cargo = new List<OreSave>();
            public List<FitSave> Fitting = new List<FitSave>();
            public float Shield, Armor, HullHp, Cap;
            public Vector3 Pos;

            // v2: missions + salvage
            public int MissionCounter;
            public float ExtraCargo;
            public List<string> CargoMods = new List<string>();
            public string MsnType = "";
            public string MsnTitle, MsnDesc;
            public string MsnOriginStation, MsnOriginSystem;
            public string MsnTargetSystem;
            public int MsnKillsRequired, MsnKillsDone;
            public string MsnOre;
            public float MsnOreAmount;
            public string MsnDestStation, MsnDestSystem;
            public float MsnPackageM3;
            public long MsnReward;
        }

        public static void Save(GameManager gm)
        {
            var p = gm.Player;
            var d = new SaveData
            {
                Credits = p.Credits,
                ActiveSkill = p.ActiveSkill,
                SystemId = gm.SystemId,
                Docked = gm.Docked,
                StationId = gm.StationId,
                HullId = p.HullId,
                Shield = p.Shield, Armor = p.Armor, HullHp = p.HullHp, Cap = p.Cap,
                Pos = gm.Ship != null ? gm.Ship.transform.position : Vector3.zero,
                MissionCounter = gm.MissionCounter,
                ExtraCargo = p.ExtraCargo,
            };
            var m = gm.ActiveMission;
            if (m != null)
            {
                d.MsnType = m.Type;
                d.MsnTitle = m.Title;
                d.MsnDesc = m.Desc;
                d.MsnOriginStation = m.OriginStationId;
                d.MsnOriginSystem = m.OriginSystemId;
                d.MsnTargetSystem = m.TargetSystemId;
                d.MsnKillsRequired = m.KillsRequired;
                d.MsnKillsDone = m.KillsDone;
                d.MsnOre = m.OreId;
                d.MsnOreAmount = m.OreAmount;
                d.MsnDestStation = m.DestStationId;
                d.MsnDestSystem = m.DestSystemId;
                d.MsnPackageM3 = m.PackageM3;
                d.MsnReward = m.Reward;
            }
            foreach (var kv in p.Skills)
                d.Skills.Add(new SkillSave { Id = kv.Key, Level = kv.Value.Level, Xp = kv.Value.Xp });
            d.Hangar.AddRange(p.Hangar);
            d.CargoMods.AddRange(p.CargoModules);
            foreach (var kv in p.Cargo)
                d.Cargo.Add(new OreSave { Id = kv.Key, Amount = kv.Value });
            foreach (var slot in new[] { SlotType.High, SlotType.Mid, SlotType.Low })
            {
                var arr = p.Fitting[slot];
                for (int i = 0; i < arr.Length; i++)
                    if (!string.IsNullOrEmpty(arr[i]))
                        d.Fitting.Add(new FitSave { Slot = (int)slot, Index = i, ModId = arr[i] });
            }
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(d));
            PlayerPrefs.Save();
        }

        public static bool TryLoad(GameManager gm)
        {
            if (!PlayerPrefs.HasKey(Key)) return false;
            try
            {
                var d = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
                if (d == null || d.V < 1 || d.V > 2) return false;
                if (!GameData.Ships.ContainsKey(d.HullId)) return false;
                if (!gm.Universe.Systems.ContainsKey(d.SystemId)) return false;

                var p = new PlayerState { Credits = d.Credits, ActiveSkill = d.ActiveSkill };
                foreach (var id in GameData.Skills.Keys) p.Skills[id] = new SkillState();
                foreach (var s in d.Skills)
                    if (p.Skills.ContainsKey(s.Id))
                        p.Skills[s.Id] = new SkillState { Level = s.Level, Xp = s.Xp };
                p.SetHull(d.HullId);
                foreach (var f in d.Fitting)
                {
                    var slot = (SlotType)f.Slot;
                    if (GameData.Modules.ContainsKey(f.ModId)
                        && f.Index >= 0 && f.Index < p.Fitting[slot].Length)
                        p.Fitting[slot][f.Index] = f.ModId;
                }
                foreach (var modId in d.Hangar)
                    if (GameData.Modules.ContainsKey(modId)) p.Hangar.Add(modId);
                if (d.CargoMods != null)
                    foreach (var modId in d.CargoMods)
                        if (GameData.Modules.ContainsKey(modId)) p.CargoModules.Add(modId);
                foreach (var o in d.Cargo)
                    if (GameData.CommodityExists(o.Id)) p.Cargo[o.Id] = o.Amount;

                var st = p.ComputeStats();
                p.Shield = Mathf.Clamp(d.Shield, 0f, st.MaxShield);
                p.Armor = Mathf.Clamp(d.Armor, 0f, st.MaxArmor);
                p.HullHp = Mathf.Clamp(d.HullHp, 1f, st.MaxHull);
                p.Cap = Mathf.Clamp(d.Cap, 0f, st.MaxCap);

                p.ExtraCargo = d.ExtraCargo;
                gm.Player = p;
                gm.SystemId = d.SystemId;
                gm.Docked = d.Docked;
                gm.StationId = d.StationId;
                gm.MissionCounter = d.MissionCounter;
                if (!string.IsNullOrEmpty(d.MsnType))
                {
                    gm.ActiveMission = new Mission
                    {
                        Type = d.MsnType,
                        Title = d.MsnTitle,
                        Desc = d.MsnDesc,
                        OriginStationId = d.MsnOriginStation,
                        OriginSystemId = d.MsnOriginSystem,
                        TargetSystemId = d.MsnTargetSystem,
                        KillsRequired = d.MsnKillsRequired,
                        KillsDone = d.MsnKillsDone,
                        OreId = d.MsnOre,
                        OreAmount = d.MsnOreAmount,
                        DestStationId = d.MsnDestStation,
                        DestSystemId = d.MsnDestSystem,
                        PackageM3 = d.MsnPackageM3,
                        Reward = d.MsnReward,
                    };
                }
                if (!d.Docked && gm.Ship != null) gm.Ship.transform.position = d.Pos;
                gm.Log("Save loaded. Welcome back, capsuleer.");
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public static void Wipe() => PlayerPrefs.DeleteKey(Key);
    }
}
