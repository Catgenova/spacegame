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

        // Galaxy-map route: destination system id, or null.
        public string RouteDest;
        List<Mission> _offerCache;
        string _offerKey;

        public readonly List<string> MessageLog = new List<string>();

        float _npcTimer;
        float _saveTimer;
        float _convoyTimer;

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
                Log("Welcome to the frontier, capsuleer. You have a Probe, a mining head and "
                    + "5,000 credits. Warp to a belt, fill the hold, sell it, repeat — the "
                    + "Shipyard tab here sells real hulls.");
            }

            // Always stamp the build into the log, so which code you are
            // running is never a guess.
            Log(BuildInfo.BootLine);

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
            {
                Sfx.LevelUp();
                Log(GameData.Skills[Player.ActiveSkill].Name + " trained to level "
                    + Player.Skills[Player.ActiveSkill].Level + ".");
            }

            _saveTimer += dt;
            if (_saveTimer > 20f) { _saveTimer = 0f; SaveSystem.Save(this); }

            // Rush contracts tick everywhere — even while docked.
            if (ActiveMission != null && ActiveMission.TimeLeft > 0f)
            {
                ActiveMission.TimeLeft -= dt;
                if (ActiveMission.TimeLeft <= 0f)
                {
                    Log("Mission failed — the deadline expired: " + ActiveMission.Title + ".");
                    if (ActiveMission.Type == "courier") Player.ExtraCargo = 0f;
                    ActiveMission = null;
                    SaveSystem.Save(this);
                }
            }

            if (Docked)
            {
                Locked = false;
                LockProgress = 0f;
                return;
            }

            UpdateLock(dt);
            UpdateNpcSpawns(dt);
            UpdateConvoys(dt);
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
                Sfx.Locked();
                Log("Target locked: " + Selected.DisplayName + ".");
            }
        }

        // ---------- route planning (galaxy map) ----------

        public void SetDestination(string sysId)
        {
            if (sysId == null || sysId == SystemId || sysId == RouteDest)
            {
                if (RouteDest != null) Log("Route cleared.");
                RouteDest = null;
                return;
            }
            RouteDest = sysId;
            int jumps = Missions.JumpCount(SystemId, sysId);
            Log("Route set: " + jumps + " jump" + (jumps == 1 ? "" : "s") + " to "
                + Universe.Systems[sysId].Name + ".");
        }

        /// <summary>Next system on the route, or null when no route is active.</summary>
        public string NextRouteHop()
        {
            if (RouteDest == null || RouteDest == SystemId) return null;
            var path = Missions.RoutePath(SystemId, RouteDest);
            return path.Count > 0 ? path[0] : null;
        }

        public int RouteJumpsLeft()
            => RouteDest == null ? 0 : Missions.RoutePath(SystemId, RouteDest).Count;

        // ---------- system / travel ----------

        public void LoadSystem(string id)
        {
            SystemId = id;
            Selected = null;
            View.LoadSystem(System);
            _npcTimer = 5f;
            _convoyTimer = 60f + Random.value * 120f;
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
            Sfx.Dock();
            Log("Docked at " + st.DisplayName + ".");
            if (Player.CargoModules.Count > 0)
            {
                Store.Modules.AddRange(Player.CargoModules);
                Log(Player.CargoModules.Count + " salvaged module(s) moved into this station's storage.");
                Player.CargoModules.Clear();
            }
            SaveSystem.Save(this);
        }

        public void Undock()
        {
            if (!Docked) return;
            Docked = false;
            PlaceAtStation();
            Ship.RefreshRack();
            Sfx.Undock();
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
            Sfx.JumpGate();
            Log("Jumped to " + System.Name + " (" + System.Sec.ToString("0.0") + " sec).");
            if (RouteDest == SystemId)
            {
                RouteDest = null;
                Log("Route destination reached.");
            }
            SaveSystem.Save(this);
        }

        // ---------- combat ----------

        public void DamagePlayer(float dmg, NpcPirate from)
        {
            bool onShield = Player.Shield > dmg * 0.5f;
            float d = dmg;
            if (Player.Shield > 0f) { float a = Mathf.Min(Player.Shield, d); Player.Shield -= a; d -= a; }
            if (d > 0f && Player.Armor > 0f) { float a = Mathf.Min(Player.Armor, d); Player.Armor -= a; d -= a; }
            if (d > 0f) Player.HullHp -= d;
            Sfx.PlayerHit(onShield);
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
            Player.CargoModules.Clear();
            // Straight back to where you started: a Probe with a mining head.
            // Nothing sells modules, so the one tool you are handed has to be the
            // one that earns on its own.
            Player.BoardProbe();
            SystemId = HomeSystem;
            StationId = HomeStation;
            Docked = true;
            LoadSystem(SystemId);
            PlaceAtStation();
            Ship.RefreshRack();
            Ship.RebuildVisual();
            Log("You wake up in a fresh clone at Solara Prime, in a Probe. One mining head, "
                + "50 m3 of hold, no guns — but the warp drive is instant. Get to work.");
            SaveSystem.Save(this);
        }

        public void NpcKilled(NpcPirate npc)
        {
            Player.Credits += npc.Def.Bounty;
            Sfx.Explosion(npc.Def.Id == "overlord" || npc.Def.Id == "convoyhauler" ? 2f
                : npc.Def.Id == "marauder" ? 1.4f : 1f);
            Log(npc.Def.Name + " destroyed. Bounty: " + GameData.FmtCredits(npc.Def.Bounty) + ".");
            if (Selected == npc) Selected = null;

            // Faction standing.
            string tierBefore = GameData.StandingTier(Player.Standing);
            Player.Standing = Mathf.Min(10f, Player.Standing + npc.Def.StandingGain);
            string tierAfter = GameData.StandingTier(Player.Standing);
            if (tierAfter != tierBefore)
                Log("Frontier Authority standing increased: you are now " + tierAfter
                    + ". Better mission pay and cheaper repairs unlocked.");

            // Leave a wreck. Pirates carry no fittable gear — the hulk yields
            // graded scrap, and rarely a blueprint chip.
            var wreck = View.SpawnWreck(npc.Def, npc.transform.position, null);
            if (npc.Def.ScrapClass > 0 && npc.Def.ScrapMax > 0f)
            {
                string sid = GameData.ScrapIdForClass(npc.Def.ScrapClass);
                float m3 = Mathf.Round(Random.Range(npc.Def.ScrapMin, npc.Def.ScrapMax));
                if (m3 > 0f)
                {
                    wreck.ScrapLoot.TryGetValue(sid, out var had);
                    wreck.ScrapLoot[sid] = had + m3;
                }
            }

            // Ship blueprint chips: rarer, and the real reason to hunt convoys.
            if (Random.value < npc.Def.BpChance)
                wreck.BpLoot.Add(Random.value < 0.55f
                    ? ModGen.RollBlueprint(npc.Def.Id)
                    : ShipGen.RollBlueprint(npc.Def.Id));

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
            else if (m != null && m.Type == "convoykill" && npc.Def.Id == "convoyhauler"
                && m.KillsDone < 1)
            {
                m.KillsDone = 1;
                Log("Mission: convoy hauler destroyed — report to the agent!");
                SaveSystem.Save(this);
            }
        }

        /// <summary>A pirate escaped by warping out — no bounty, no wreck.</summary>
        public void NpcFled(NpcPirate npc)
        {
            if (Selected == npc) Selected = null;
            Log(npc.Def.Name + " warped away!");
            View.RemoveObject(npc);
        }

        /// <summary>The storage bay of the station we're docked at.</summary>
        public StationStore Store => Player.StoreAt(StationId);

        /// <summary>Display name of the station we're docked at.</summary>
        public string HereName() => Station != null ? Station.Name : "this station";

        public const float LootRange = 40f;

        // ---------- station storage ----------
        // Unlimited capacity, but strictly local: what you leave here can only
        // be picked up here.

        /// <summary>Move one commodity from the hold into station storage.</summary>
        public void DepositCommodity(string id)
        {
            if (!Docked || !Player.Cargo.TryGetValue(id, out float qty) || qty <= 0f) return;
            Store.AddCargo(id, qty);
            Player.Cargo.Remove(id);
            Log("Stored " + Mathf.Round(qty) + " m3 " + GameData.Commodity(id).Name
                + " at " + HereName() + ".");
            SaveSystem.Save(this);
        }

        /// <summary>Empty the whole hold into station storage.</summary>
        public void DepositAllCargo()
        {
            if (!Docked || Player.Cargo.Count == 0) return;
            float moved = 0f;
            foreach (var kv in Player.Cargo) { Store.AddCargo(kv.Key, kv.Value); moved += kv.Value; }
            Player.Cargo.Clear();
            Log("Stored " + Mathf.Round(moved) + " m3 of cargo at " + HereName() + ".");
            SaveSystem.Save(this);
        }

        /// <summary>Pull a commodity back out, as much as the hold will take.</summary>
        public void WithdrawCommodity(string id)
        {
            if (!Docked || !Store.Cargo.TryGetValue(id, out float have) || have <= 0f) return;
            float room = Player.ComputeStats().CargoCap - Player.CargoUsed();
            if (room <= 0.01f) { Log("Cargo hold is full."); return; }
            float take = Mathf.Min(have, room);
            Player.Cargo.TryGetValue(id, out var had);
            Player.Cargo[id] = had + take;
            Store.Cargo[id] = have - take;
            if (Store.Cargo[id] <= 0.01f) Store.Cargo.Remove(id);
            Log("Loaded " + Mathf.Round(take) + " m3 " + GameData.Commodity(id).Name
                + (take < have ? " (hold full — rest still stored)." : "."));
            SaveSystem.Save(this);
        }

        /// <summary>Leave a blueprint in this station's vault.</summary>
        public void DepositBlueprint(int index)
        {
            if (!Docked || index < 0 || index >= Player.Blueprints.Count) return;
            var bp = Player.Blueprints[index];
            Store.Blueprints.Add(bp);
            Player.Blueprints.RemoveAt(index);
            Log("Filed " + ShipGen.DescribeBlueprint(bp) + " at " + HereName() + ".");
            SaveSystem.Save(this);
        }

        public void WithdrawBlueprint(int index)
        {
            if (!Docked || index < 0 || index >= Store.Blueprints.Count) return;
            var bp = Store.Blueprints[index];
            Player.Blueprints.Add(bp);
            Store.Blueprints.RemoveAt(index);
            Log("Collected " + ShipGen.DescribeBlueprint(bp) + ".");
            SaveSystem.Save(this);
        }

        /// <summary>Pull up to `m3` of exotics (and any blueprint chips) out of a
        /// sealed container. Partial hauls are fine — the hold filling up should
        /// cost you time, not the whole container.</summary>
        public void CrackContainer(SiteContainer can, float m3)
        {
            if (can == null || can.Empty) return;

            // Blueprint chips are data — they come out whole, first.
            for (int i = can.BpLoot.Count - 1; i >= 0; i--)
            {
                var bp = can.BpLoot[i];
                Player.Blueprints.Add(bp);
                Log("BLUEPRINT recovered from the container: " + ShipGen.DescribeBlueprint(bp));
                can.BpLoot.RemoveAt(i);
            }

            float cap = Player.ComputeStats().CargoCap;
            var ids = new List<string>(can.Exotics.Keys);
            foreach (var id in ids)
            {
                float room = cap - Player.CargoUsed();
                if (room <= 0.01f) { Log("Cargo hold full — exotics left in the container."); break; }
                float take = Mathf.Min(Mathf.Min(can.Exotics[id], m3), room);
                if (take <= 0.01f) continue;
                Player.Cargo.TryGetValue(id, out var had);
                Player.Cargo[id] = had + take;
                can.Exotics[id] -= take;
                if (can.Exotics[id] <= 0.01f) can.Exotics.Remove(id);
                Log("Recovered " + Mathf.Round(take) + " m3 " + GameData.Commodity(id).Name + ".");
                Sfx.MinerChunk();
                m3 -= take;
                if (m3 <= 0.01f) break;
            }

            if (can.Empty)
            {
                Log(can.DisplayName + " stripped.");
                var site = can.Site;
                if (Selected == can) Select(null);
                View.RemoveObject(can);
                if (site != null)
                {
                    site.Containers.Remove(can);
                    site.NoteContainerEmptied(this);
                }
            }
            SaveSystem.Save(this);
        }

        /// <summary>Tick a salvage mission for one recovered haul.</summary>
        public void CountSalvageMission()
        {
            var m = ActiveMission;
            if (m == null || m.Type != "salvage" || m.SalvageDone >= m.SalvageRequired) return;
            m.SalvageDone++;
            Log("Mission: " + m.SalvageDone + "/" + m.SalvageRequired + " salvage recovered"
                + (m.SalvageDone >= m.SalvageRequired ? " — report to the agent!" : "."));
        }

        public void LootWreck()
        {
            // The Loot button doubles as "crack container" when one is selected.
            if (Selected is SiteContainer can && !Docked)
            {
                if (DistTo(can) > LootRange)
                { Log("Get within " + GameData.FmtDist(LootRange) + " to crack the container."); return; }
                CrackContainer(can, 9999f);
                return;
            }
            var w = Selected as Wreck;
            if (w == null || Docked) return;
            if (DistTo(w) > LootRange) { Log("Get within " + GameData.FmtDist(LootRange) + " to salvage."); return; }

            // Blueprint chips are data — no cargo space needed.
            for (int i = w.BpLoot.Count - 1; i >= 0; i--)
            {
                var bp = w.BpLoot[i];
                Player.Blueprints.Add(bp);
                Log("BLUEPRINT acquired: " + ShipGen.DescribeBlueprint(bp)
                    + " — see the Industry tab at any station.");
                w.BpLoot.RemoveAt(i);
            }

            var st = Player.ComputeStats();

            // Scrap is a bulk commodity — take as much as the hold allows.
            if (w.ScrapLoot.Count > 0)
            {
                var sids = new List<string>(w.ScrapLoot.Keys);
                foreach (var sid in sids)
                {
                    float room = st.CargoCap - Player.CargoUsed();
                    if (room <= 0.01f) { Log("Cargo hold full — scrap left in the wreck."); break; }
                    float take = Mathf.Min(w.ScrapLoot[sid], room);
                    Player.Cargo.TryGetValue(sid, out var had);
                    Player.Cargo[sid] = had + take;
                    w.ScrapLoot[sid] -= take;
                    if (w.ScrapLoot[sid] <= 0.01f) w.ScrapLoot.Remove(sid);
                    Log("Salvaged " + Mathf.Round(take) + " m3 "
                        + GameData.Commodity(sid).Name + ".");
                    CountSalvageMission();
                }
            }

            if (w.Loot.Count == 0)
            {
                if (w.ScrapLoot.Count == 0)
                {
                    Log("Nothing more of value in the wreck.");
                    Select(null);
                    View.RemoveObject(w);
                }
                return;
            }
            for (int i = w.Loot.Count - 1; i >= 0; i--)
            {
                if (st.CargoCap - Player.CargoUsed() < GameData.ModuleCargoVolume)
                {
                    Log("Cargo hold too full for more salvage.");
                    break;
                }
                Player.CargoModules.Add(w.Loot[i]);
                Log("Salvaged " + GameData.ResolveModule(w.Loot[i]).Name + ".");
                w.Loot.RemoveAt(i);
                CountSalvageMission();
            }
            if (w.Loot.Count == 0 && w.BpLoot.Count == 0 && w.ScrapLoot.Count == 0)
            {
                Select(null);
                View.RemoveObject(w);
            }
            SaveSystem.Save(this);
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

        /// <summary>
        /// Pirate convoys run the belts of low-sec systems: a fat hauler with a
        /// big bounty and guaranteed loot, plus two escorts. Hunting one is the
        /// finale of "The Abyss Job".
        /// </summary>
        void UpdateConvoys(float dt)
        {
            if (System.Sec > 0.5f) return;
            _convoyTimer -= dt;
            if (_convoyTimer > 0f) return;
            bool hunting = ActiveMission != null && ActiveMission.Type == "convoykill";
            _convoyTimer = (hunting ? 120f : 240f) + Random.value * 160f;
            if (View.Npcs.Exists(n => n.Def.Id == "convoyhauler")) return;
            if (!hunting && Random.value > 0.7f) return;
            SpawnConvoy();
        }

        void SpawnConvoy()
        {
            var belts = System.Celestials.FindAll(c => c.Kind == ObjKind.Belt);
            if (belts.Count == 0) return;
            var belt = belts[Random.Range(0, belts.Count)];
            var basePos = belt.Pos + new Vector3(400f, 40f, 250f);
            View.SpawnNpc("convoyhauler", basePos);
            string escortType = System.Sec <= 0.1f ? "overlord" : "marauder";
            for (int i = 0; i < 2; i++)
            {
                var off = Random.insideUnitSphere * 120f;
                off.y *= 0.2f;
                View.SpawnNpc(escortType, basePos + off);
            }
            Log("A pirate convoy has been sighted near " + belt.Name + "!");
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

        public void SellCommodity(string id)
        {
            if (!Docked || !Player.Cargo.TryGetValue(id, out float qty) || qty <= 0f) return;
            long unit = Market.ApplyTradeSkill(Market.OreSellPrice(StationId, id), TradeLevel, true);
            long total = (long)Mathf.Round(unit * qty);
            Player.Credits += total;
            Player.Cargo.Remove(id);
            Log("Sold " + Mathf.Round(qty) + " m3 " + GameData.Commodity(id).Name
                + " for " + GameData.FmtCredits(total) + ".");
            SaveSystem.Save(this);
        }

        public float RefineYield()
            => GameData.BaseRefineYield + GameData.RefineYieldPerLevel * Player.SkillLevel("refining");

        /// <summary>Refine feedstock out of your hold. The metal goes to the
        /// station's bay, not back into your ship.</summary>
        public void RefineOre(string oreId) => Refine(oreId, false);

        /// <summary>Refine feedstock that is already sitting in this station's
        /// bay, without hauling it back aboard first.</summary>
        public void RefineStored(string oreId) => Refine(oreId, true);

        /// <summary>Run ore or scrap through the station refinery.
        ///
        /// Refining is a station service, so the output is warehoused here
        /// rather than stuffed into your hold: every metal it produces lands in
        /// this station's bay. That matters because scrap mills out into several
        /// times its own volume, and because the bay is unlimited — you can put
        /// a full hold through the refinery without first working out whether
        /// the result will fit. Collect what you actually want to carry from the
        /// Storage tab afterwards.
        ///
        /// Feedstock can come from your hold or straight out of the bay.</summary>
        void Refine(string oreId, bool fromStore)
        {
            if (!Docked || !GameData.TryRefinable(oreId, out var def)) return;
            var source = fromStore ? Store.Cargo : Player.Cargo;
            if (!source.TryGetValue(oreId, out float qty) || qty <= 0f) return;
            float yield = RefineYield();
            source.Remove(oreId);
            var summary = "";
            foreach (var kv in def.RefineInto)
            {
                float outM3 = qty * yield * kv.Value;
                Store.AddCargo(kv.Key, outM3);
                summary += (summary.Length > 0 ? ", " : "")
                    + Mathf.Round(outM3) + " m3 " + GameData.Commodity(kv.Key).Name;
            }
            Log("Refined " + Mathf.Round(qty) + " m3 " + def.Name
                + (fromStore ? " out of the bay" : "") + " into " + summary
                + " — waiting in " + HereName() + "'s storage bay.");
            SaveSystem.Save(this);
        }

        /// <summary>What this station charges for a hull, trade skill included.
        /// Licensed yard stock carries a premium over the catalogue.</summary>
        public long ShipPriceHere(string shipId)
        {
            long price = Shipyard.Sells(Station, shipId)
                ? Shipyard.Price(StationId, shipId)
                : Market.ShipBuyPrice(StationId, shipId);
            return Market.ApplyTradeSkill(price, TradeLevel, false);
        }

        public void BuyShip(string shipId)
        {
            if (!Docked || shipId == Player.HullId) return;
            // Yard stock is the only thing anyone sells: no catalogue hulls
            // exist, and Class 2 and 3 never reach a pad.
            if (!Shipyard.Sells(Station, shipId))
            {
                Log("That hull is not for sale here.");
                return;
            }
            var def = GameData.ResolveShip(shipId);
            long price = ShipPriceHere(shipId);
            long tradeIn = Market.ShipTradeInValue(StationId, Player.HullId);
            long cost = price - tradeIn;
            if (Player.Credits < cost) { Log("Not enough credits (even with trade-in)."); return; }
            if (Player.CargoUsed() > def.Cargo)
            {
                Log("Your cargo will not fit in the new ship. Sell some ore first.");
                return;
            }
            Player.Credits -= cost;
            StripModulesToHangar();
            Player.SetHull(shipId);
            Ship.RebuildVisual();
            Log("Signed for a licensed " + def.Name + " off the pad. Net cost "
                + GameData.FmtCredits(cost) + " after trade-in.");
            SaveSystem.Save(this);
        }

        void StripModulesToHangar()
        {
            foreach (var slot in Slots.All)
            {
                if (!Player.Fitting.ContainsKey(slot)) continue;
                foreach (var modId in Player.Fitting[slot])
                    if (!string.IsNullOrEmpty(modId)) Store.Modules.Add(modId);
            }
        }

        public void FitModule(int hangarIndex)
        {
            if (!Docked || hangarIndex < 0 || hangarIndex >= Store.Modules.Count) return;
            string modId = Store.Modules[hangarIndex];
            var m = GameData.ResolveModule(modId);
            if (m.Slot == SlotType.High && Player.Hull.TurretOnly && m.Kind != ModuleKind.Weapon)
            {
                Log("This hull's hardpoints only accept turrets — no " + m.Name + " here.");
                return;
            }
            if (!Player.Fitting.ContainsKey(m.Slot) || Player.Fitting[m.Slot].Length == 0)
            {
                Log("This hull has no " + (m.Slot == SlotType.Web ? "web" : m.Slot.ToString().ToLower())
                    + " slot.");
                return;
            }
            var arr = Player.Fitting[m.Slot];
            int free = -1;
            for (int i = 0; i < arr.Length; i++)
                if (string.IsNullOrEmpty(arr[i])) { free = i; break; }
            if (free == -1) { Log("No free " + m.Slot + " slot."); return; }
            arr[free] = modId;
            Store.Modules.RemoveAt(hangarIndex);
            Log("Fitted " + m.Name + ".");
            SaveSystem.Save(this);
        }

        public void UnfitModule(SlotType slot, int idx)
        {
            if (!Docked) return;
            string modId = Player.Fitting[slot][idx];
            if (string.IsNullOrEmpty(modId)) return;
            Player.Fitting[slot][idx] = null;
            Store.Modules.Add(modId);
            Log("Unfitted " + GameData.ResolveModule(modId).Name + ".");
            SaveSystem.Save(this);
        }

        // ---------- manufacturing (Industry tab) ----------

        /// <summary>How much of a material the assembly line here can reach: this
        /// station's bay plus your hold. The refinery banks its output locally, so
        /// insisting on the hold would mean withdrawing metal just to feed it back
        /// in.</summary>
        public float MaterialOnHand(string id)
        {
            Store.Cargo.TryGetValue(id, out float bay);
            Player.Cargo.TryGetValue(id, out float hold);
            return bay + hold;
        }

        /// <summary>What a build will have to take out of your hold, because the
        /// bay could not cover it. The hull swap needs this to know how much space
        /// the build actually frees.</summary>
        float HoldShareOfBuild(Dictionary<string, float> cost)
        {
            float fromHold = 0f;
            foreach (var kv in cost)
            {
                Store.Cargo.TryGetValue(kv.Key, out float bay);
                fromHold += Mathf.Max(0f, kv.Value - bay);
            }
            return fromHold;
        }

        /// <summary>Consume a material for a build, spending the station's bay
        /// first so your hold is disturbed as little as possible.</summary>
        void TakeMaterial(string id, float want)
        {
            Store.Cargo.TryGetValue(id, out float bay);
            float fromBay = Mathf.Min(want, bay);
            if (fromBay > 0f)
            {
                Store.Cargo[id] = bay - fromBay;
                if (Store.Cargo[id] <= 0.01f) Store.Cargo.Remove(id);
            }
            float fromHold = want - fromBay;
            if (fromHold <= 0f) return;
            Player.Cargo.TryGetValue(id, out float hold);
            Player.Cargo[id] = hold - fromHold;
            if (Player.Cargo[id] <= 0.01f) Player.Cargo.Remove(id);
        }

        /// <summary>"336/320 Iron, 90/180 Aluminium" — what a build needs against
        /// what your hold and this station's bay can supply between them.</summary>
        public string MaterialLine(Dictionary<string, float> cost)
        {
            var line = "";
            foreach (var kv in cost)
                line += (line.Length > 0 ? ", " : "") + Mathf.Round(MaterialOnHand(kv.Key))
                     + "/" + kv.Value + " " + GameData.Commodity(kv.Key).Name;
            return line;
        }

        string MaterialBlocker(Dictionary<string, float> cost)
        {
            foreach (var kv in cost)
            {
                float have = MaterialOnHand(kv.Key);
                if (have < kv.Value)
                    return "Missing " + Mathf.Round(kv.Value - have) + " m3 "
                        + GameData.Commodity(kv.Key).Name
                        + " (counting your hold and " + HereName() + "'s bay).";
            }
            return null;
        }

        /// <summary>Why this blueprint can't be built right now, or null if it can.</summary>
        public string ManufactureBlocker(Blueprint bp)
        {
            if (!Docked) return "Dock at a station to manufacture.";
            if (bp.RunsLeft <= 0) return "Blueprint exhausted.";
            if (bp.IsModule) return ModuleBlocker(bp);
            var missing = MaterialBlocker(ShipGen.MaterialCost(bp));
            if (missing != null) return missing;
            if (Player.Credits < ShipGen.Fee(bp))
                return "Assembly fee is " + GameData.FmtCredits(ShipGen.Fee(bp)) + ".";
            return null;
        }

        string ModuleBlocker(Blueprint bp)
        {
            var missing = MaterialBlocker(ModGen.MaterialCost(bp));
            if (missing != null) return missing;
            if (Player.Credits < ModGen.Fee(bp))
                return "Assembly fee is " + GameData.FmtCredits(ModGen.Fee(bp)) + ".";
            return null;
        }

        /// <summary>Print one module off a module blueprint. The finished piece
        /// carries whatever modifiers its rarity rolled and lands in this
        /// station's storage bay.</summary>
        void ManufactureModule(Blueprint bp)
        {
            foreach (var kv in ModGen.MaterialCost(bp)) TakeMaterial(kv.Key, kv.Value);
            Player.Credits -= ModGen.Fee(bp);

            // Each run is its own piece: mix the print's hash with the run index
            // so a 5-run Common print yields five distinct (if plain) modules.
            int run = GameData.ModBpRuns[bp.Rarity] - bp.RunsLeft;
            string craftId = ModGen.CraftId(bp.ModuleId, bp.Hash + run, bp.Rarity);
            Store.Modules.Add(craftId);
            var made = GameData.ResolveModule(craftId);

            bp.RunsLeft--;
            Log("Manufactured " + made.Name + " — in this station's storage.");
            if (bp.RunsLeft <= 0)
            {
                Player.Blueprints.Remove(bp);
                Log("That blueprint is spent and gone.");
            }
            SaveSystem.Save(this);
        }

        public void Manufacture(Blueprint bp)
        {
            if (ManufactureBlocker(bp) != null) { Log(ManufactureBlocker(bp)); return; }
            if (bp.IsModule) { ManufactureModule(bp); return; }
            var def = ShipGen.Def(bp);

            // Everything left in the hold once the build has taken its share must
            // fit the new hull. Only the part drawn from the hold frees space —
            // materials spent out of the station's bay were never aboard.
            var cost = ShipGen.MaterialCost(bp);
            if (Player.CargoUsed() - HoldShareOfBuild(cost) > def.Cargo)
            {
                Log("Your remaining cargo would not fit the " + def.Name
                    + "'s hold. Sell or store some first.");
                return;
            }

            foreach (var kv in cost) TakeMaterial(kv.Key, kv.Value);
            Player.Credits -= ShipGen.Fee(bp);
            long tradeIn = Market.ShipTradeInValue(StationId, Player.HullId);
            Player.Credits += tradeIn;

            StripModulesToHangar();
            Player.SetHull(def.Id);
            Ship.RebuildVisual();

            bp.RunsLeft--;
            Log("Manufactured " + def.Name + " (body " + bp.Hash + "). Old hull traded in for "
                + GameData.FmtCredits(tradeIn) + ".");
            if (bp.RunsLeft <= 0)
            {
                Player.Blueprints.Remove(bp);
                Log("Blueprint " + bp.Hash + " is spent — this body will never be built again.");
            }
            SaveSystem.Save(this);
        }

        public long RepairCost()
        {
            var st = Player.ComputeStats();
            float missing = (st.MaxArmor - Player.Armor) + (st.MaxHull - Player.HullHp);
            float discount = 1f - GameData.StandingRepairDiscount(Player.Standing);
            return (long)Mathf.Round(missing * 2f * discount);
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

        public bool ArcAvailable()
            => !Player.ArcDone && Player.Standing >= 1f;

        public List<Mission> StationOffers()
        {
            string key = StationId + "#" + MissionCounter + "#" + ArcAvailable();
            if (_offerKey != key)
            {
                _offerCache = Missions.GenerateOffers(Universe, StationId, SystemId, MissionCounter);
                if (ArcAvailable())
                    _offerCache.Insert(0, Missions.BuildArcStage(Universe, 1, StationId, SystemId));
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
                case "salvage":
                    return StationId == m.OriginStationId && m.SalvageDone >= m.SalvageRequired;
                case "convoykill":
                    return StationId == m.OriginStationId && m.KillsDone >= 1;
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

            long reward = (long)(m.Reward * (1f + GameData.StandingRewardBonus(Player.Standing)));
            Player.Credits += reward;
            Sfx.Payout();
            ActiveMission = null;
            MissionCounter++;
            Log("Mission complete! Reward: " + GameData.FmtCredits(reward)
                + (reward > m.Reward ? " (incl. " + GameData.StandingTier(Player.Standing) + " bonus)" : "") + ".");

            // Story arc: completing a stage hands you the next one on the spot.
            if (m.ArcStage == 1 || m.ArcStage == 2)
            {
                ActiveMission = Missions.BuildArcStage(Universe, m.ArcStage + 1,
                    m.OriginStationId, m.OriginSystemId);
                Log("The agent has more for you — new mission: " + ActiveMission.Title + ".");
            }
            else if (m.ArcStage == 3)
            {
                Player.ArcDone = true;
                Player.Standing = Mathf.Min(10f, Player.Standing + 1f);
                Log("The Abyss Job is settled. Frontier Authority standing greatly increased ("
                    + GameData.StandingTier(Player.Standing) + ").");
            }
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
