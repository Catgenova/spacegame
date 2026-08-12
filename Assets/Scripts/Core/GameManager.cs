using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Central game state: the universe, the player, the current system,
    /// docking, selection, economy actions, skill training, NPC spawning,
    /// belt respawns, and autosave.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public const string HomeSystem = "solara";
        public const string HomeStation = "solara_station";

        public UniverseData Universe;
        public string SystemId;
        public bool Docked;
        public string StationId;
        public PlayerState Player;
        public SystemView View;
        public ShipController Ship;
        public SpaceObject Selected;
        public bool Ready { get; private set; }

        // Target lock (EVE-style: select, wait for lock, then modules work).
        public const float LockRange = 600f;
        public bool Locked;
        public float LockProgress;

        // Agent missions.
        public Mission ActiveMission;
        public int MissionCounter;
        List<Mission> _offerCache;
        string _offerKey;

        public readonly List<string> MessageLog = new List<string>();

        float _npcTimer;
        float _saveTimer;

        public StarSystemData System => Universe.Systems[SystemId];
        public Celestial Station => StationId != null ? System.Find(StationId) : null;

        void Awake()
        {
            I = this;
        }

        /// <summary>Called by GameBootstrap once the view and ship exist.</summary>
        public void Init(SystemView view, ShipController ship)
        {
            View = view;
            Ship = ship;
            Universe = UniverseGenerator.Build();

            if (!SaveSystem.TryLoad(this))
            {
                Player = PlayerState.NewGame();
                SystemId = HomeSystem;
                Docked = true;
                StationId = HomeStation;
                Log("Welcome to the frontier, capsuleer. Undock when ready.");
            }

            LoadSystem(SystemId);
            PlaceAtStation();
            Ship.RefreshRack();
            Ship.RebuildVisual();
            Ready = true;
        }

        public void Log(string text)
        {
            MessageLog.Add(text);
            if (MessageLog.Count > 60) MessageLog.RemoveAt(0);
        }

        void Update()
        {
            if (!Ready) return;
            float dt = Time.deltaTime;

            // Passive skill training, always on.
            int gained = Player.AddSkillXp(Player.ActiveSkill, GameData.SkillXpRate * dt);
            if (gained > 0)
                Log(GameData.Skills[Player.ActiveSkill].Name + " trained to level "
                    + Player.Skills[Player.ActiveSkill].Level + ".");

            _saveTimer += dt;
            if (_saveTimer > 20f) { _saveTimer = 0f; SaveSystem.Save(this); }

            if (Docked)
            {
                Locked = false;
                LockProgress = 0f;
                return;
            }

            UpdateLock(dt);
            UpdateNpcSpawns(dt);
            UpdateBeltRespawn(dt);
        }

        void UpdateLock(float dt)
        {
            bool lockable = Selected is AsteroidBody || Selected is NpcPirate;
            if (!lockable)
            {
                Locked = false;
                LockProgress = 0f;
                return;
            }
            if (DistTo(Selected) > LockRange)
            {
                Locked = false;
                LockProgress = 0f;
                return;
            }
            if (Locked) return;
            float lockTime = Selected is NpcPirate ? 2.2f : 1.2f;
            LockProgress += dt / lockTime;
            if (LockProgress >= 1f)
            {
                LockProgress = 1f;
                Locked = true;
                Log("Target locked: " + Selected.DisplayName + ".");
            }
        }

        // ---------- system / travel ----------

        public void LoadSystem(string id)
        {
            SystemId = id;
            Selected = null;
            View.LoadSystem(System);
            _npcTimer = 5f;
            SpawnInitialNpcs();
        }

        public void PlaceAtStation()
        {
            var st = Station ?? System.Celestials[0];
            StationId = st.Id;
            Ship.transform.position = st.Pos + new Vector3(120f, 30f, 60f);
            Ship.ResetMotion();
        }

        public void Select(SpaceObject obj)
        {
            if (Selected == obj) return;
            Selected = obj;
            Locked = false;
            LockProgress = 0f;
        }

        public float DistTo(SpaceObject obj)
            => Vector3.Distance(Ship.transform.position, obj.transform.position);

        public void WarpToSelected()
        {
            if (Selected == null) return;
            Ship.WarpTo(Selected.transform.position, Selected.DisplayName);
        }

        public void Dock()
        {
            var st = Selected as CelestialBody;
            if (st == null || st.Kind != ObjKind.Station || Docked) return;
            if (DistTo(st) > GameData.DockRange)
            {
                Log("Too far to dock. Get within " + GameData.FmtDist(GameData.DockRange) + ".");
                return;
            }
            Docked = true;
            StationId = st.Id;
            Ship.ResetMotion();
            Log("Docked at " + st.DisplayName + ".");
            SaveSystem.Save(this);
        }

        public void Undock()
        {
            if (!Docked) return;
            Docked = false;
            PlaceAtStation();
            Ship.RefreshRack();
            Log("Undocked from " + Station.Name + ". Fly safe.");
        }

        public void Jump()
        {
            var gate = Selected as CelestialBody;
            if (gate == null || gate.Kind != ObjKind.Gate || Docked) return;
            if (DistTo(gate) > GameData.JumpRange)
            {
                Log("Too far from the gate to jump.");
                return;
            }
            string destId = gate.Data.GateTo;
            string fromId = SystemId;
            LoadSystem(destId);
            var back = System.Celestials.Find(c => c.Kind == ObjKind.Gate && c.GateTo == fromId);
            Ship.transform.position = (back != null ? back.Pos : Vector3.zero) + new Vector3(150f, 20f, 100f);
            Ship.ResetMotion();
            Log("Jumped to " + System.Name + " (" + System.Sec.ToString("0.0") + " sec).");
            SaveSystem.Save(this);
        }

        // ---------- combat ----------

        public void DamagePlayer(float dmg, NpcPirate from)
        {
            float d = dmg;
            if (Player.Shield > 0f) { float a = Mathf.Min(Player.Shield, d); Player.Shield -= a; d -= a; }
            if (d > 0f && Player.Armor > 0f) { float a = Mathf.Min(Player.Armor, d); Player.Armor -= a; d -= a; }
            if (d > 0f) Player.HullHp -= d;
            if (Player.HullHp <= 0f) PlayerDeath(from);
        }

        void PlayerDeath(NpcPirate killer)
        {
            Log("Your ship was destroyed by a " + killer.Def.Name + ". Cargo lost.");
            if (ActiveMission != null && ActiveMission.Type == "courier")
            {
                Log("The courier package was destroyed with your ship. Mission failed.");
                ActiveMission = null;
            }
            Player.ExtraCargo = 0f;
            Player.Cargo.Clear();
            Player.SetHull("wasp");
            Player.Fitting[SlotType.High][0] = "miner1";
            SystemId = HomeSystem;
            StationId = HomeStation;
            Docked = true;
            LoadSystem(SystemId);
            PlaceAtStation();
            Ship.RefreshRack();
            Ship.RebuildVisual();
            Log("You wake up in a fresh clone at Solara Prime, in a loaner Wasp.");
            SaveSystem.Save(this);
        }

        public void NpcKilled(NpcPirate npc)
        {
            Player.Credits += npc.Def.Bounty;
            Log(npc.Def.Name + " destroyed. Bounty: " + GameData.FmtCredits(npc.Def.Bounty) + ".");
            if (Selected == npc) Selected = null;
            View.RemoveObject(npc);

            var m = ActiveMission;
            if (m != null && m.Type == "bounty" && SystemId == m.TargetSystemId
                && m.KillsDone < m.KillsRequired)
            {
                m.KillsDone++;
                Log("Mission: " + m.KillsDone + "/" + m.KillsRequired + " pirates destroyed"
                    + (m.KillsDone >= m.KillsRequired ? " — return to the agent!" : "."));
                SaveSystem.Save(this);
            }
        }

        public void RemoveAsteroid(AsteroidBody rock)
        {
            System.Asteroids.Remove(rock.Data);
            if (Selected == rock) Selected = null;
            View.RemoveObject(rock);
        }

        // ---------- NPC + belt lifecycle ----------

        void SpawnInitialNpcs()
        {
            var cfg = System.Pirates;
            if (cfg == null) return;
            int n = Mathf.Min(cfg.Max, 1 + Random.Range(0, cfg.Max));
            for (int i = 0; i < n; i++) SpawnNpc();
        }

        void SpawnNpc()
        {
            var cfg = System.Pirates;
            if (cfg == null) return;
            var belts = System.Celestials.FindAll(c => c.Kind == ObjKind.Belt);
            if (belts.Count == 0) return;
            var belt = belts[Random.Range(0, belts.Count)];
            string type = cfg.Types[Random.Range(0, cfg.Types.Length)];
            var offset = Random.insideUnitSphere * 800f;
            offset.y *= 0.2f;
            View.SpawnNpc(type, belt.Pos + offset + Vector3.one * 300f);
        }

        void UpdateNpcSpawns(float dt)
        {
            var cfg = System.Pirates;
            if (cfg == null || View.Npcs.Count >= cfg.Max) return;
            _npcTimer -= dt;
            if (_npcTimer <= 0f)
            {
                _npcTimer = cfg.Interval;
                SpawnNpc();
            }
        }

        void UpdateBeltRespawn(float dt)
        {
            var sys = System;
            foreach (var belt in sys.Celestials)
            {
                if (belt.Kind != ObjKind.Belt) continue;
                bool alive = sys.Asteroids.Exists(a => a.BeltId == belt.Id);
                if (alive) { sys.BeltRespawn.Remove(belt.Id); continue; }
                if (!sys.BeltRespawn.ContainsKey(belt.Id)) sys.BeltRespawn[belt.Id] = 120f;
                sys.BeltRespawn[belt.Id] -= dt;
                if (sys.BeltRespawn[belt.Id] <= 0f)
                {
                    sys.BeltRespawn.Remove(belt.Id);
                    foreach (var a in UniverseGenerator.SpawnBeltAsteroids(sys, belt))
                        View.SpawnAsteroid(a);
                    Log(belt.Name + " has replenished.");
                }
            }
        }

        // ---------- economy (station UI) ----------

        int TradeLevel => Player.SkillLevel("trade");

        public void SellOre(string oreId)
        {
            if (!Docked || !Player.Cargo.TryGetValue(oreId, out float qty) || qty <= 0f) return;
            long unit = Market.ApplyTradeSkill(Market.OreSellPrice(StationId, oreId), TradeLevel, true);
            long total = (long)Mathf.Round(unit * qty);
            Player.Credits += total;
            Player.Cargo.Remove(oreId);
            Log("Sold " + Mathf.Round(qty) + " m3 " + GameData.Ores[oreId].Name
                + " for " + GameData.FmtCredits(total) + ".");
            SaveSystem.Save(this);
        }

        public void BuyModule(string modId)
        {
            if (!Docked) return;
            long price = Market.ApplyTradeSkill(Market.ModuleBuyPrice(StationId, modId), TradeLevel, false);
            if (Player.Credits < price) { Log("Not enough credits."); return; }
            Player.Credits -= price;
            Player.Hangar.Add(modId);
            Log("Bought " + GameData.Modules[modId].Name + " for " + GameData.FmtCredits(price) + " (in hangar).");
            SaveSystem.Save(this);
        }

        public void BuyShip(string shipId)
        {
            if (!Docked || shipId == Player.HullId) return;
            long price = Market.ApplyTradeSkill(Market.ShipBuyPrice(StationId, shipId), TradeLevel, false);
            long tradeIn = Market.ShipTradeInValue(StationId, Player.HullId);
            long cost = price - tradeIn;
            if (Player.Credits < cost) { Log("Not enough credits (even with trade-in)."); return; }
            if (Player.CargoUsed() > GameData.Ships[shipId].Cargo)
            {
                Log("Your cargo will not fit in the new ship. Sell some ore first.");
                return;
            }
            Player.Credits -= cost;
            foreach (var slot in new[] { SlotType.High, SlotType.Mid, SlotType.Low })
                foreach (var modId in Player.Fitting[slot])
                    if (!string.IsNullOrEmpty(modId)) Player.Hangar.Add(modId);
            Player.SetHull(shipId);
            Ship.RebuildVisual();
            Log("Now flying a " + GameData.Ships[shipId].Name + ". Net cost "
                + GameData.FmtCredits(cost) + " after trade-in.");
            SaveSystem.Save(this);
        }

        public void FitModule(int hangarIndex)
        {
            if (!Docked || hangarIndex < 0 || hangarIndex >= Player.Hangar.Count) return;
            string modId = Player.Hangar[hangarIndex];
            var m = GameData.Modules[modId];
            var arr = Player.Fitting[m.Slot];
            int free = -1;
            for (int i = 0; i < arr.Length; i++)
                if (string.IsNullOrEmpty(arr[i])) { free = i; break; }
            if (free == -1) { Log("No free " + m.Slot + " slot."); return; }
            arr[free] = modId;
            Player.Hangar.RemoveAt(hangarIndex);
            Log("Fitted " + m.Name + ".");
            SaveSystem.Save(this);
        }

        public void UnfitModule(SlotType slot, int idx)
        {
            if (!Docked) return;
            string modId = Player.Fitting[slot][idx];
            if (string.IsNullOrEmpty(modId)) return;
            Player.Fitting[slot][idx] = null;
            Player.Hangar.Add(modId);
            Log("Unfitted " + GameData.Modules[modId].Name + ".");
            SaveSystem.Save(this);
        }

        public long RepairCost()
        {
            var st = Player.ComputeStats();
            float missing = (st.MaxArmor - Player.Armor) + (st.MaxHull - Player.HullHp);
            return (long)Mathf.Round(missing * 2f);
        }

        public void Repair()
        {
            if (!Docked) return;
            long cost = RepairCost();
            if (cost <= 0) { Log("No repairs needed."); return; }
            if (Player.Credits < cost) { Log("Not enough credits for repairs."); return; }
            Player.Credits -= cost;
            var st = Player.ComputeStats();
            Player.Armor = st.MaxArmor;
            Player.HullHp = st.MaxHull;
            Log("Ship repaired for " + GameData.FmtCredits(cost) + ".");
            SaveSystem.Save(this);
        }

        // ---------- missions ----------

        public List<Mission> StationOffers()
        {
            string key = StationId + "#" + MissionCounter;
            if (_offerKey != key)
            {
                _offerCache = Missions.GenerateOffers(Universe, StationId, SystemId, MissionCounter);
                _offerKey = key;
            }
            return _offerCache;
        }

        public void AcceptMission(Mission m)
        {
            if (!Docked || ActiveMission != null || m == null) return;
            if (m.Type == "courier")
            {
                var st = Player.ComputeStats();
                if (st.CargoCap - Player.CargoUsed() < m.PackageM3)
                {
                    Log("Not enough free cargo space for the package ("
                        + m.PackageM3 + " m3 needed).");
                    return;
                }
                Player.ExtraCargo = m.PackageM3;
            }
            ActiveMission = m;
            MissionCounter++;
            Log("Mission accepted: " + m.Title + ".");
            SaveSystem.Save(this);
        }

        /// <summary>Whether the active mission can be turned in at the current station.</summary>
        public bool CanTurnInMission()
        {
            var m = ActiveMission;
            if (m == null || !Docked) return false;
            switch (m.Type)
            {
                case "bounty":
                    return StationId == m.OriginStationId && m.KillsDone >= m.KillsRequired;
                case "mining":
                    return StationId == m.OriginStationId
                        && Player.Cargo.TryGetValue(m.OreId, out float have) && have >= m.OreAmount;
                case "courier":
                    return StationId == m.DestStationId;
                default:
                    return false;
            }
        }

        public void TurnInMission()
        {
            if (!CanTurnInMission()) return;
            var m = ActiveMission;
            if (m.Type == "mining")
            {
                Player.Cargo[m.OreId] -= m.OreAmount;
                if (Player.Cargo[m.OreId] <= 0.01f) Player.Cargo.Remove(m.OreId);
            }
            else if (m.Type == "courier")
            {
                Player.ExtraCargo = 0f;
            }
            Player.Credits += m.Reward;
            ActiveMission = null;
            MissionCounter++;
            Log("Mission complete! Reward: " + GameData.FmtCredits(m.Reward) + ".");
            SaveSystem.Save(this);
        }

        public void AbandonMission()
        {
            if (ActiveMission == null) return;
            if (ActiveMission.Type == "courier") Player.ExtraCargo = 0f;
            Log("Mission abandoned: " + ActiveMission.Title + ".");
            ActiveMission = null;
            SaveSystem.Save(this);
        }

        public string StationName(string sysId, string stationId)
        {
            if (!Universe.Systems.ContainsKey(sysId)) return "station";
            var c = Universe.Systems[sysId].Find(stationId);
            return c != null ? c.Name : "station";
        }

        public void SetTraining(string skillId)
        {
            if (!GameData.Skills.ContainsKey(skillId)) return;
            Player.ActiveSkill = skillId;
            Log("Now training " + GameData.Skills[skillId].Name + ".");
        }

        // ---------- overview ----------

        public List<SpaceObject> OverviewEntries()
        {
            var pos = Ship.transform.position;
            var list = new List<SpaceObject>();
            foreach (var o in View.Objects)
            {
                if (o == null) continue;
                if (o.Kind == ObjKind.Asteroid
                    && Vector3.Distance(pos, o.transform.position) > 4000f) continue;
                list.Add(o);
            }
            list.Sort((a, b) =>
                Vector3.Distance(pos, a.transform.position)
                    .CompareTo(Vector3.Distance(pos, b.transform.position)));
            if (list.Count > 40) list.RemoveRange(40, list.Count - 40);
            return list;
        }
    }
}
