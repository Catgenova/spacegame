using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// The player's ship in space: EVE-style inertial flight (manual WASD or
    /// approach/orbit autopilot), warp drive, and fitted module cycles
    /// (mining lasers, weapons, boosters, afterburner).
    /// </summary>
    public class ShipController : MonoBehaviour
    {
        public enum CmdMode { None, Approach, Orbit }

        public class RackEntry
        {
            public SlotType Slot;
            public int Index;
            public string ModId;
            public bool Active;
            public float T;
            public ModuleDef Def => GameData.ResolveModule(ModId);
        }

        const float Inertia = 1.4f;

        public Vector3 Vel;
        public float Throttle;
        public readonly List<RackEntry> Rack = new List<RackEntry>();

        CmdMode _cmd = CmdMode.None;
        SpaceObject _cmdTarget;
        float _orbitRange = 80f;

        // Warp state
        Vector3 _warpFrom, _warpTo;
        float _warpT, _warpDur;
        bool _warpAligning;
        public bool InWarp { get; private set; }

        LineRenderer _beam;
        TrailRenderer _trail;

        GameManager GM => GameManager.I;
        PlayerState P => GM.Player;

        void Awake()
        {
            var beamGo = new GameObject("Beam");
            beamGo.transform.SetParent(transform, false);
            _beam = beamGo.AddComponent<LineRenderer>();
            _beam.positionCount = 2;
            _beam.startWidth = 0.8f;
            _beam.endWidth = 0.4f;
            _beam.material = SystemView.Mat(Color.cyan, true);
            _beam.enabled = false;
        }

        /// <summary>Rebuild the hull visual (called on init and every hull change).</summary>
        public void RebuildVisual()
        {
            var old = transform.Find("Hull");
            if (old != null) Destroy(old.gameObject);
            ShipVisuals.BuildHull(P.HullId, transform);

            if (_trail == null)
            {
                var trailGo = new GameObject("EngineTrail");
                trailGo.transform.SetParent(transform, false);
                trailGo.transform.localPosition = new Vector3(0f, 0f, -3f);
                _trail = trailGo.AddComponent<TrailRenderer>();
                _trail.time = 1.4f;
                _trail.startWidth = 1.1f;
                _trail.endWidth = 0.05f;
                _trail.minVertexDistance = 0.5f;
                _trail.material = SystemView.Mat(new Color(0.45f, 0.8f, 1f), true);
            }
        }

        /// <summary>Rebuild the activatable module rack from the fitting (high then mid).</summary>
        public void RefreshRack()
        {
            Rack.Clear();
            foreach (var slot in new[] { SlotType.High, SlotType.Mid, SlotType.Web, SlotType.Disruptor, SlotType.Claw, SlotType.Drone, SlotType.Sensor, SlotType.Collector })
            {
                if (!P.Fitting.ContainsKey(slot)) continue;
                var arr = P.Fitting[slot];
                for (int i = 0; i < arr.Length; i++)
                    if (!string.IsNullOrEmpty(arr[i]))
                        Rack.Add(new RackEntry { Slot = slot, Index = i, ModId = arr[i] });
            }
        }

        public void ResetMotion()
        {
            Vel = Vector3.zero;
            Throttle = 0f;
            _cmd = CmdMode.None;
            _cmdTarget = null;
            InWarp = false;
            DeactivateAll();
        }

        readonly System.Collections.Generic.Dictionary<RackEntry, DroneUnit> _drones
            = new System.Collections.Generic.Dictionary<RackEntry, DroneUnit>();

        void ReleaseDrone(RackEntry r)
        {
            if (_drones.TryGetValue(r, out var d))
            {
                if (d != null) d.Target = null; // flies home and despawns
                _drones.Remove(r);
            }
        }

        public void DeactivateAll()
        {
            foreach (var r in Rack)
            {
                if (r.Active && r.Def.Kind == ModuleKind.Drone) ReleaseDrone(r);
                r.Active = false;
                r.T = 0f;
            }
        }

        // ---------- commands ----------

        public void Approach(SpaceObject target)
        {
            if (target == null || GM.Docked || InWarp) return;
            _cmd = CmdMode.Approach;
            _cmdTarget = target;
        }

        public void Orbit(SpaceObject target, float range = 80f)
        {
            if (target == null || GM.Docked || InWarp) return;
            _cmd = CmdMode.Orbit;
            _cmdTarget = target;
            _orbitRange = range;
        }

        public bool WarpTo(Vector3 target, string label)
        {
            if (GM.Docked || InWarp) return false;
            float d = Vector3.Distance(transform.position, target);
            if (d < GameData.MinWarpDist)
            {
                GM.Log("Too close to warp — approach instead.");
                return false;
            }
            var off = Random.onUnitSphere;
            off.y *= 0.2f;
            _warpFrom = transform.position;
            _warpTo = target + off.normalized * Random.Range(80f, 140f);
            _warpDur = Mathf.Clamp(3f + d / 45000f, 4f, 10f);
            _warpT = 0f;
            _warpAligning = true;
            InWarp = true;
            _cmd = CmdMode.None;
            DeactivateAll();
            Sfx.WarpEnter();
            GM.Log("Warp drive active — " + label + ".");
            return true;
        }

        // ---------- modules ----------

        public void ToggleModule(int rackIndex)
        {
            if (GM.Docked || InWarp || rackIndex < 0 || rackIndex >= Rack.Count) return;
            var r = Rack[rackIndex];
            if (r.Active)
            {
                if (r.Def.Kind == ModuleKind.Drone) ReleaseDrone(r);
                r.Active = false;
                r.T = 0f;
                return;
            }
            var m = r.Def;
            if ((m.Kind == ModuleKind.Miner || m.Kind == ModuleKind.Claw || m.Kind == ModuleKind.Weapon
                || m.Kind == ModuleKind.Web || m.Kind == ModuleKind.Disruptor
                || m.Kind == ModuleKind.Drone || m.Kind == ModuleKind.Collector) && !ValidTarget(m))
            {
                var sel = GM.Selected;
                if (m.Kind == ModuleKind.Collector)
                {
                    GM.Log(!(sel is Wreck) && !(sel is SiteContainer)
                        ? "Select a wreck or a sealed container first."
                        : "Target out of collector range.");
                    Sfx.Deny();
                    return;
                }
                bool wantsRock = m.Kind == ModuleKind.Miner || m.Kind == ModuleKind.Claw;
                bool rightKind = wantsRock ? sel is AsteroidBody : sel is NpcPirate;
                if (!rightKind)
                    GM.Log(wantsRock
                        ? "Select an asteroid first."
                        : "Select a hostile ship first.");
                else if (Vector3.Distance(transform.position, sel.transform.position) > m.Range)
                    GM.Log(m.Kind == ModuleKind.Claw
                        ? "The claw needs to touch the rock — approach to 0 km."
                        : "Target out of range for " + m.Name + ".");
                else
                    GM.Log("Still locking target — wait for the lock.");
                Sfx.Deny();
                return;
            }
            if (P.Cap < m.CapUse) { GM.Log("Capacitor too low."); Sfx.Deny(); return; }
            r.Active = true;
            r.T = 0f;
            if (m.Kind != ModuleKind.Afterburner) P.Cap -= m.CapUse; // first cycle paid up front
        }

        bool ValidTarget(ModuleDef m)
        {
            var sel = GM.Selected;
            if (sel == null) return false;
            float d = Vector3.Distance(transform.position, sel.transform.position);
            if (m.Kind == ModuleKind.Miner || m.Kind == ModuleKind.Claw)
                return sel is AsteroidBody && d <= m.Range && GM.Locked;
            if (m.Kind == ModuleKind.Weapon || m.Kind == ModuleKind.Web || m.Kind == ModuleKind.Disruptor
                || m.Kind == ModuleKind.Drone)
                return sel is NpcPirate && d <= m.Range && GM.Locked;
            if (m.Kind == ModuleKind.Collector)
                // wrecks and containers need no target lock
                return (sel is Wreck || sel is SiteContainer) && d <= m.Range;
            return true;
        }

        bool AfterburnerOn()
        {
            foreach (var r in Rack)
                if (r.Active && r.Def.Kind == ModuleKind.Afterburner) return true;
            return false;
        }

        // ---------- update ----------

        void Update()
        {
            if (GM == null || !GM.Ready || GM.Docked) return;
            float dt = Time.deltaTime;
            var st = P.ComputeStats();

            // Regen.
            P.Cap = Mathf.Min(st.MaxCap, P.Cap + st.CapRegen * dt);
            P.Shield = Mathf.Min(st.MaxShield, P.Shield + st.MaxShield * 0.005f * dt);

            if (InWarp) UpdateWarp(dt);
            else
            {
                UpdateFlight(dt, st);
                UpdateModules(dt);
            }
            UpdateBeam();
        }

        void UpdateWarp(float dt)
        {
            if (_warpAligning)
            {
                var want = Quaternion.LookRotation((_warpTo - transform.position).normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 120f * dt);
                _warpT += dt;
                if (_warpT > 1.2f) { _warpAligning = false; _warpT = 0f; }
                return;
            }
            _warpT += dt;
            float p = Mathf.Clamp01(_warpT / _warpDur);
            float ease = p * p * (3f - 2f * p); // smoothstep: slow in, fast middle, slow out
            transform.position = Vector3.LerpUnclamped(_warpFrom, _warpTo, ease);
            Vel = Vector3.zero;
            if (p >= 1f)
            {
                InWarp = false;
                Sfx.WarpExit();
                GM.Log("Warp drive disengaged.");
            }
        }

        void UpdateFlight(float dt, ShipStats st)
        {
            float speed = st.Speed * (AfterburnerOn() ? 1.8f : 1f);

            // Keyboard flight overrides autopilot.
            bool manual = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S)
                       || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);
            if (manual) { _cmd = CmdMode.None; _cmdTarget = null; }

            if (Input.GetKey(KeyCode.A)) transform.Rotate(0f, -st.Turn * dt, 0f, Space.World);
            if (Input.GetKey(KeyCode.D)) transform.Rotate(0f, st.Turn * dt, 0f, Space.World);
            if (Input.GetKey(KeyCode.W)) Throttle = Mathf.Clamp01(Throttle + dt * 0.8f);
            if (Input.GetKey(KeyCode.S)) Throttle = Mathf.Clamp01(Throttle - dt * 0.8f);
            if (Input.GetKeyDown(KeyCode.X)) { Throttle = 0f; _cmd = CmdMode.None; }

            if (_cmd != CmdMode.None && _cmdTarget == null) _cmd = CmdMode.None; // target despawned

            Vector3 desired;
            if (_cmd == CmdMode.Approach)
            {
                Vector3 to = _cmdTarget.transform.position - transform.position;
                float d = to.magnitude;
                if (d < 25f)
                {
                    _cmd = CmdMode.None;
                    Throttle = 0f;
                    desired = Vector3.zero;
                }
                else
                {
                    desired = to.normalized * speed;
                    Throttle = 1f;
                    FaceVelocity(desired, st.Turn, dt);
                }
            }
            else if (_cmd == CmdMode.Orbit)
            {
                Vector3 to = _cmdTarget.transform.position - transform.position;
                float d = to.magnitude;
                Vector3 radial = -to.normalized;
                Vector3 tangent = Vector3.Cross(radial, Vector3.up).normalized;
                float radialErr = Mathf.Clamp((d - _orbitRange) / Mathf.Max(_orbitRange, 1f), -0.7f, 0.7f);
                desired = (tangent + radial * -radialErr).normalized * speed;
                Throttle = 1f;
                FaceVelocity(desired, st.Turn, dt);
            }
            else
            {
                desired = transform.forward * Throttle * speed;
            }

            float k = Mathf.Min(1f, dt / Inertia);
            Vel += (desired - Vel) * k;
            transform.position += Vel * dt;
        }

        void FaceVelocity(Vector3 dir, float turnDegPerSec, float dt)
        {
            if (dir.sqrMagnitude < 0.001f) return;
            var want = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnDegPerSec * 2f * dt);
        }

        void UpdateModules(float dt)
        {
            foreach (var r in Rack)
            {
                if (!r.Active) continue;
                var m = r.Def;

                if (m.Kind == ModuleKind.Afterburner)
                {
                    P.Cap -= m.CapUse * dt;
                    if (P.Cap <= 0f)
                    {
                        P.Cap = 0f;
                        r.Active = false;
                        GM.Log("Afterburner shut down — capacitor empty.");
                    }
                    continue;
                }

                r.T += dt;
                if (r.T < m.Cycle) continue;
                r.T = 0f;

                if (m.Kind == ModuleKind.Miner || m.Kind == ModuleKind.Claw) CompleteMiningCycle(m, r);
                else if (m.Kind == ModuleKind.Weapon) CompleteWeaponCycle(m, r);
                else if (m.Kind == ModuleKind.Web) CompleteWebCycle(m, r);
                else if (m.Kind == ModuleKind.Disruptor) CompleteDisruptCycle(m, r);
                else if (m.Kind == ModuleKind.Drone) CompleteDroneCycle(m, r);
                else if (m.Kind == ModuleKind.Sensor) CompleteSensorCycle(m, r);
                else if (m.Kind == ModuleKind.Collector) CompleteCollectorCycle(m, r);
                else if (m.Kind == ModuleKind.ShieldBooster)
                    P.Shield = Mathf.Min(P.ComputeStats().MaxShield, P.Shield + m.BoostAmount);

                // Decide whether the next cycle starts.
                if (r.Active)
                {
                    if ((m.Kind == ModuleKind.Miner || m.Kind == ModuleKind.Claw || m.Kind == ModuleKind.Weapon
                        || m.Kind == ModuleKind.Web || m.Kind == ModuleKind.Disruptor
                        || m.Kind == ModuleKind.Drone || m.Kind == ModuleKind.Collector) && !ValidTarget(m))
                    {
                        if (m.Kind == ModuleKind.Drone) ReleaseDrone(r);
                        r.Active = false;
                        GM.Log(m.Name + " deactivated — target lost or out of range.");
                    }
                    else if (P.Cap < m.CapUse)
                    {
                        r.Active = false;
                        GM.Log(m.Name + " deactivated — capacitor empty.");
                    }
                    else
                    {
                        P.Cap -= m.CapUse;
                    }
                }
            }
        }

        void CompleteMiningCycle(ModuleDef m, RackEntry r)
        {
            var rock = GM.Selected as AsteroidBody;
            if (rock == null || Vector3.Distance(transform.position, rock.transform.position) > m.Range)
            {
                r.Active = false;
                return;
            }
            var st = P.ComputeStats();
            float bonus = (1f + 0.05f * P.SkillLevel("mining")) * P.Hull.MiningBonus;
            float space = st.CargoCap - P.CargoUsed();
            if (space <= 0.01f)
            {
                r.Active = false;
                GM.Log("Cargo hold full.");
                return;
            }
            float mined = Mathf.Min(m.Yield * bonus, rock.Data.Amount, space);
            P.Cargo.TryGetValue(rock.Data.Ore, out float have);
            P.Cargo[rock.Data.Ore] = have + mined;
            Sfx.MinerChunk();
            rock.Data.Amount -= mined;
            if (rock.Data.Amount <= 0.01f)
            {
                GM.Log(rock.DisplayName + " depleted.");
                GM.RemoveAsteroid(rock);
                r.Active = false;
            }
            else
            {
                rock.SyncScale();
            }
        }

        void CompleteWeaponCycle(ModuleDef m, RackEntry r)
        {
            var npc = GM.Selected as NpcPirate;
            if (npc == null || Vector3.Distance(transform.position, npc.transform.position) > m.Range)
            {
                r.Active = false;
                return;
            }
            // Tracking: fast transversal makes slow guns land glancing blows.
            float angVel = Combat.AngularVelocity(
                npc.transform.position - transform.position, npc.Vel - Vel);
            float dmg = m.Dmg * (1f + 0.05f * P.SkillLevel("gunnery"));
            dmg = Combat.RollDamage(dmg, m.Tracking, angVel, out bool hit);
            Sfx.WeaponFire(m, hit);
            if (npc.TakeDamage(dmg))
            {
                r.Active = false;
                GM.NpcKilled(npc);
            }
        }

        void CompleteWebCycle(ModuleDef m, RackEntry r)
        {
            var npc = GM.Selected as NpcPirate;
            if (npc == null || Vector3.Distance(transform.position, npc.transform.position) > m.Range)
            {
                r.Active = false;
                return;
            }
            npc.ApplyWeb(m.Cycle + 0.6f);
            Sfx.Web();
        }

        void CompleteDisruptCycle(ModuleDef m, RackEntry r)
        {
            var npc = GM.Selected as NpcPirate;
            if (npc == null || Vector3.Distance(transform.position, npc.transform.position) > m.Range)
            {
                r.Active = false;
                return;
            }
            npc.ApplyDisrupt(m.Cycle + 0.6f);
            Sfx.Web();
        }

        void CompleteDroneCycle(ModuleDef m, RackEntry r)
        {
            var npc = GM.Selected as NpcPirate;
            if (npc == null || Vector3.Distance(transform.position, npc.transform.position) > m.Range)
            {
                ReleaseDrone(r);
                r.Active = false;
                return;
            }
            if (!_drones.TryGetValue(r, out var drone) || drone == null)
            {
                drone = DroneUnit.Launch(transform.position);
                _drones[r] = drone;
            }
            drone.Target = npc;
            // Drones only bite once they've closed to their orbit.
            if (Vector3.Distance(drone.transform.position, npc.transform.position) > 45f) return;
            float dmg = m.Dmg * (1f + 0.05f * P.SkillLevel("gunnery"));
            drone.Fire();
            Sfx.WeaponFire(m, true);
            if (npc.TakeDamage(dmg))
            {
                ReleaseDrone(r);
                r.Active = false;
                GM.NpcKilled(npc);
            }
        }

        // A sensor sweep sometimes turns up a drifting salvage cache —
        // the Trail line's whole reason to leave the station.
        static readonly NpcDef CacheDef = new NpcDef { Id = "cache", Name = "Drifting Cache" };

        void CompleteSensorCycle(ModuleDef m, RackEntry r)
        {
            // A sharper array finds more, and finds deeper. Low security means
            // richer anomalies: the good stuff sits where the law does not.
            float sec = GM.System != null ? GM.System.Sec : 1f;
            float siteChance = (m.Cycle <= 8f ? 0.26f : 0.18f) + (1f - sec) * 0.16f;
            if (Random.value < siteChance)
            {
                int tier = 1;
                float tr = Random.value;
                float t3 = (1f - sec) * 0.30f, t2 = 0.22f + (1f - sec) * 0.22f;
                if (tr < t3) tier = 3;
                else if (tr < t3 + t2) tier = 2;
                if (m.Cycle > 8f && tier == 3 && Random.value < 0.5f) tier = 2; // T1 array rarely cracks the best

                var sdir = Random.onUnitSphere;
                sdir.y *= 0.12f;
                sdir = sdir.normalized;
                var spos = transform.position + sdir * Random.Range(700f, 1600f);
                var site = GM.View.SpawnSite(tier, spos);
                GM.Log("Sensor sweep: ANOMALY — " + site.DisplayName + " "
                    + GameData.FmtDist(Vector3.Distance(transform.position, spos))
                    + " out. " + site.Containers.Count + " sealed containers, "
                    + Mathf.Round(site.TotalLife) + "s before it collapses, and something will answer. Move.");
                Sfx.Locked();
                return;
            }
            if (Random.value < 0.35f)
            {
                var dir = Random.onUnitSphere;
                dir.y *= 0.15f;
                dir = dir.normalized;
                var pos = transform.position + dir * Random.Range(250f, 600f);
                var loot = new List<string>();
                var cachePool = GameData.Loot["cache"].Pool;
                int n = Random.value < 0.4f ? 2 : 1;
                for (int i = 0; i < n; i++)
                    loot.Add(cachePool[Random.Range(0, cachePool.Length)]);
                var wreck = GM.View.SpawnWreck(CacheDef, pos, loot);
                if (Random.value < 0.08f)
                    wreck.BpLoot.Add(ShipGen.RollBlueprint("cache"));
                GM.Log("Sensor sweep: CONTACT — drifting cache "
                    + GameData.FmtDist(Vector3.Distance(transform.position, pos))
                    + " out. It won't drift forever.");
                Sfx.Web();
            }
            else
            {
                GM.Log("Sensor sweep complete — nothing on this pass.");
            }
        }

        void CompleteCollectorCycle(ModuleDef m, RackEntry r)
        {
            // A sealed container is the collector's other job — and the one that
            // actually pays, if you can crack enough of them in time.
            var can = GM.Selected as SiteContainer;
            if (can != null)
            {
                if (Vector3.Distance(transform.position, can.transform.position) > m.Range)
                {
                    r.Active = false;
                    return;
                }
                GM.CrackContainer(can, m.Yield > 0f ? m.Yield : 12f);
                if (can.Empty) r.Active = false;
                return;
            }
            var w = GM.Selected as Wreck;
            if (w == null || Vector3.Distance(transform.position, w.transform.position) > m.Range)
            {
                r.Active = false;
                return;
            }
            // Blueprint chips are data — pull them all in one pass.
            for (int i = w.BpLoot.Count - 1; i >= 0; i--)
            {
                var bp = w.BpLoot[i];
                P.Blueprints.Add(bp);
                GM.Log("BLUEPRINT tractored in: " + ShipGen.DescribeBlueprint(bp)
                    + " — see the Industry tab at any station.");
                w.BpLoot.RemoveAt(i);
            }
            // Scrap first: one graded batch per cycle, as much as the hold takes.
            if (w.ScrapLoot.Count > 0)
            {
                var st2 = P.ComputeStats();
                float room = st2.CargoCap - P.CargoUsed();
                if (room <= 0.01f)
                {
                    r.Active = false;
                    GM.Log("Cargo hold full — scrap left in the wreck.");
                    return;
                }
                string sid = null;
                foreach (var k in w.ScrapLoot.Keys) { sid = k; break; }
                float take = Mathf.Min(w.ScrapLoot[sid], Mathf.Min(room, 10f));
                P.Cargo.TryGetValue(sid, out var had);
                P.Cargo[sid] = had + take;
                w.ScrapLoot[sid] -= take;
                if (w.ScrapLoot[sid] <= 0.01f) w.ScrapLoot.Remove(sid);
                GM.Log("Collector reeled in " + Mathf.Round(take) + " m3 "
                    + GameData.Commodity(sid).Name + ".");
                Sfx.MinerChunk();
                GM.CountSalvageMission();
            }
            else if (w.Loot.Count > 0)
            {
                var st = P.ComputeStats();
                if (st.CargoCap - P.CargoUsed() < GameData.ModuleCargoVolume)
                {
                    r.Active = false;
                    GM.Log("Cargo hold too full for more salvage.");
                    return;
                }
                int last = w.Loot.Count - 1;
                string modId = w.Loot[last];
                P.CargoModules.Add(modId);
                GM.Log("Collector reeled in " + GameData.ResolveModule(modId).Name + ".");
                w.Loot.RemoveAt(last);
                Sfx.MinerChunk();
                GM.CountSalvageMission();
            }
            if (w.Loot.Count == 0 && w.BpLoot.Count == 0 && w.ScrapLoot.Count == 0)
            {
                r.Active = false;
                GM.Log("Wreck stripped clean.");
                if (GM.Selected == w) GM.Select(null);
                GM.View.RemoveObject(w);
            }
        }

        void UpdateBeam()
        {
            var sel = GM.Selected;
            bool show = false;
            Color color = Color.cyan;
            if (sel != null && !InWarp)
            {
                foreach (var r in Rack)
                {
                    if (!r.Active) continue;
                    if (r.Def.Kind == ModuleKind.Miner && sel is AsteroidBody)
                    {
                        show = true;
                        color = new Color(0.4f, 0.85f, 1f);
                        break;
                    }
                    if (r.Def.Kind == ModuleKind.Claw && sel is AsteroidBody)
                    {
                        show = true;
                        color = new Color(1f, 0.62f, 0.25f);
                        break;
                    }
                    if (r.Def.Kind == ModuleKind.Weapon && sel is NpcPirate)
                    {
                        show = true;
                        color = new Color(1f, 0.45f, 0.3f);
                        break;
                    }
                    if (r.Def.Kind == ModuleKind.Web && sel is NpcPirate)
                    {
                        show = true;
                        color = new Color(0.35f, 0.95f, 0.85f);
                        break;
                    }
                    if (r.Def.Kind == ModuleKind.Disruptor && sel is NpcPirate)
                    {
                        show = true;
                        color = new Color(0.72f, 0.45f, 1f);
                        break;
                    }
                    if (r.Def.Kind == ModuleKind.Collector && sel is Wreck)
                    {
                        show = true;
                        color = new Color(1f, 0.85f, 0.35f);
                        break;
                    }
                }
            }
            _beam.enabled = show;
            if (show)
            {
                _beam.startColor = color;
                _beam.endColor = color;
                _beam.SetPosition(0, transform.position);
                _beam.SetPosition(1, sel.transform.position);
            }
        }
    }
}
