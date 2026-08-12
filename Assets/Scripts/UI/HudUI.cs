using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Prototype HUD built with IMGUI so the project needs no UI assets:
    /// ship status bars, module rack, overview, target actions, messages,
    /// station services (market / fitting / ships / repair) and skills.
    /// Also handles click-to-select and module hotkeys.
    /// </summary>
    public class HudUI : MonoBehaviour
    {
        // Shared "pointer is over UI" flag — written by whichever HUD is active
        // (this IMGUI one or UitHud), read by input/camera code.
        public static bool MouseOverUI { get; set; }

        readonly List<Rect> _uiRects = new List<Rect>();
        Vector2 _overviewScroll, _stationScroll, _skillsScroll;
        bool _showSkills;
        int _stationTab;

        static GUIStyle _rowStyle, _titleStyle, _smallStyle;
        static bool _stylesReady;

        GameManager GM => GameManager.I;

        void Update()
        {
            if (GM == null || !GM.Ready) return;

            // GUI-space mouse position (y flipped) vs last frame's panel rects.
            var mp = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            MouseOverUI = false;
            foreach (var r in _uiRects)
                if (r.Contains(mp)) { MouseOverUI = true; break; }

            // Click to select.
            if (Input.GetMouseButtonDown(0) && !MouseOverUI && !GM.Docked && Camera.main != null)
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 2000000f))
                {
                    var obj = hit.collider.GetComponentInParent<SpaceObject>();
                    if (obj != null) GM.Select(obj);
                }
            }

            // Module hotkeys 1-8.
            for (int i = 0; i < 8; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    GM.Ship.ToggleModule(i);

            if (Input.GetKeyDown(KeyCode.K)) _showSkills = !_showSkills;
            if (Input.GetKeyDown(KeyCode.M))
                GM.Log("The galaxy map needs the UI Toolkit HUD — press F10 to switch.");
            if (Input.GetKeyDown(KeyCode.Escape)) _showSkills = false;
        }

        void OnGUI()
        {
            if (GM == null || !GM.Ready) return;
            GUI.skin = UiSkin.Skin;
            EnsureStyles();
            if (Event.current.type == EventType.Repaint) _uiRects.Clear();

            DrawTopBar();
            DrawMessages();

            if (GM.Docked)
            {
                DrawStationWindow();
            }
            else
            {
                DrawHud();
                DrawModRack();
                DrawOverview();
                DrawTargetPanel();
                DrawMissionTracker();
                if (GM.Ship.InWarp) DrawCenterText("— WARP DRIVE ACTIVE —");
            }

            if (_showSkills) DrawSkillsWindow();
        }

        static void EnsureStyles()
        {
            if (_stylesReady) return;
            _rowStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                padding = new RectOffset(6, 6, 2, 2),
            };
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
            };
            _smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
            };
            _stylesReady = true;
        }

        void Panel(Rect r)
        {
            if (Event.current.type == EventType.Repaint) _uiRects.Add(r);
            UiSkin.Panel(r);
        }

        static void DrawBarRow(float x, float y, float w, string label, float frac, Color c, string text)
        {
            GUI.Label(new Rect(x, y, 34, 14), label, _smallStyle);
            var track = new Rect(x + 36, y + 3, w - 116, 8);
            GUI.color = new Color(0.05f, 0.09f, 0.15f);
            GUI.DrawTexture(track, Texture2D.whiteTexture);
            GUI.color = c;
            GUI.DrawTexture(new Rect(track.x, track.y, track.width * Mathf.Clamp01(frac), track.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var num = new GUIStyle(_smallStyle) { alignment = TextAnchor.MiddleRight };
            GUI.Label(new Rect(x + w - 78, y, 74, 14), text, num);
        }

        void DrawTopBar()
        {
            var r = new Rect(0, 0, Screen.width, 26);
            Panel(r);
            var sys = GM.System;
            string sec = sys.Sec.ToString("0.0");
            GUI.Label(new Rect(12, 4, 400, 20),
                sys.Name.ToUpper() + "   <sec " + sec + ">" + (GM.Docked ? "   [DOCKED: " + GM.Station.Name + "]" : ""),
                _titleStyle);
            var right = new GUIStyle(_titleStyle) { alignment = TextAnchor.MiddleRight };
            GUI.Label(new Rect(Screen.width - 460, 4, 440, 20),
                GameData.StandingTier(GM.Player.Standing) + "  ·  " + GameData.FmtCredits(GM.Player.Credits), right);
        }

        void DrawHud()
        {
            var p = GM.Player;
            var st = p.ComputeStats();
            var r = new Rect(12, Screen.height - 158, 250, 118);
            Panel(r);
            float y = r.y + 8;
            DrawBarRow(r.x + 8, y, r.width - 16, "SHD", p.Shield / st.MaxShield, new Color(0.35f, 0.7f, 1f), Mathf.Round(p.Shield) + "/" + Mathf.Round(st.MaxShield)); y += 17;
            DrawBarRow(r.x + 8, y, r.width - 16, "ARM", p.Armor / st.MaxArmor, new Color(0.75f, 0.78f, 0.82f), Mathf.Round(p.Armor) + "/" + Mathf.Round(st.MaxArmor)); y += 17;
            DrawBarRow(r.x + 8, y, r.width - 16, "HUL", p.HullHp / st.MaxHull, new Color(1f, 0.55f, 0.36f), Mathf.Round(p.HullHp) + "/" + Mathf.Round(st.MaxHull)); y += 17;
            DrawBarRow(r.x + 8, y, r.width - 16, "CAP", p.Cap / st.MaxCap, new Color(1f, 0.84f, 0.37f), Mathf.Round(p.Cap) + "/" + Mathf.Round(st.MaxCap)); y += 17;
            DrawBarRow(r.x + 8, y, r.width - 16, "CRG", p.CargoUsed() / st.CargoCap, new Color(0.62f, 0.48f, 1f), Mathf.Round(p.CargoUsed()) + "/" + Mathf.Round(st.CargoCap) + " m3"); y += 20;
            float ms = GM.Ship.Vel.magnitude * GameData.UnitsToMs;
            GUI.Label(new Rect(r.x + 8, y, r.width - 16, 16),
                p.Hull.Name + "   " + Mathf.Round(ms) + " m/s", _smallStyle);
        }

        void DrawModRack()
        {
            var rack = GM.Ship.Rack;
            if (rack.Count == 0) return;
            float w = rack.Count * 62f + 8f;
            var r = new Rect((Screen.width - w) / 2f, Screen.height - 74, w, 62);
            Panel(r);
            for (int i = 0; i < rack.Count; i++)
            {
                var entry = rack[i];
                var m = entry.Def;
                var br = new Rect(r.x + 6 + i * 62f, r.y + 5, 56, 44);
                GUI.backgroundColor = entry.Active ? new Color(0.4f, 1f, 0.6f) : Color.white;
                if (GUI.Button(br, (i + 1) + "\n" + m.Short))
                    GM.Ship.ToggleModule(i);
                GUI.backgroundColor = Color.white;
                if (entry.Active && m.Cycle > 0.01f && m.Kind != ModuleKind.Afterburner)
                {
                    GUI.color = new Color(0.4f, 1f, 0.6f);
                    GUI.DrawTexture(new Rect(br.x, br.yMax + 3, br.width * Mathf.Clamp01(entry.T / m.Cycle), 4), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }
        }

        void DrawOverview()
        {
            var r = new Rect(Screen.width - 292, 32, 284, Screen.height - 48);
            Panel(r);
            GUI.Label(new Rect(r.x + 8, r.y + 4, 200, 16), "OVERVIEW", _smallStyle);
            var listRect = new Rect(r.x + 4, r.y + 24, r.width - 8, r.height - 30);
            var entries = GM.OverviewEntries();
            GUILayout.BeginArea(listRect);
            _overviewScroll = GUILayout.BeginScrollView(_overviewScroll);
            foreach (var o in entries)
            {
                if (o == null) continue;
                string d = GameData.FmtDist(GM.DistTo(o));
                string label = KindIcon(o.Kind) + " " + o.DisplayName + "  —  " + d;
                GUI.backgroundColor = GM.Selected == o ? new Color(0.5f, 0.75f, 1f) : Color.white;
                GUI.contentColor = KindColor(o.Kind);
                if (GUILayout.Button(label, _rowStyle, GUILayout.Height(20)))
                    GM.Select(o);
                GUI.backgroundColor = Color.white;
                GUI.contentColor = Color.white;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        public static string KindIcon(ObjKind k)
        {
            switch (k)
            {
                case ObjKind.Sun: return "☀";
                case ObjKind.Planet: return "●";
                case ObjKind.Belt: return "◦";
                case ObjKind.Station: return "▣";
                case ObjKind.Gate: return "◈";
                case ObjKind.Asteroid: return "▪";
                case ObjKind.Npc: return "▲";
                case ObjKind.Wreck: return "☒";
                default: return "·";
            }
        }

        public static Color KindColor(ObjKind k)
        {
            switch (k)
            {
                case ObjKind.Npc: return new Color(1f, 0.45f, 0.45f);
                case ObjKind.Asteroid: return new Color(0.85f, 0.78f, 0.62f);
                case ObjKind.Station: return new Color(0.55f, 0.8f, 1f);
                case ObjKind.Gate: return new Color(1f, 0.85f, 0.5f);
                case ObjKind.Wreck: return new Color(0.65f, 0.6f, 0.5f);
                default: return new Color(0.8f, 0.86f, 0.92f);
            }
        }

        void DrawTargetPanel()
        {
            var sel = GM.Selected;
            if (sel == null) return;
            var r = new Rect(12, Screen.height - 340, 250, 170);
            Panel(r);
            GUI.contentColor = KindColor(sel.Kind);
            GUI.Label(new Rect(r.x + 8, r.y + 5, r.width - 16, 18), sel.DisplayName, _titleStyle);
            GUI.contentColor = Color.white;
            // Lock status for lockable targets.
            string lockText = "";
            if (sel is NpcPirate || sel is AsteroidBody)
            {
                if (GM.Locked) lockText = "  [LOCKED]";
                else if (GM.DistTo(sel) > GameManager.LockRange) lockText = "  [OUT OF LOCK RANGE]";
                else lockText = "  [LOCKING " + Mathf.RoundToInt(GM.LockProgress * 100f) + "%]";
            }
            GUI.contentColor = GM.Locked ? new Color(0.5f, 1f, 0.65f) : new Color(1f, 0.85f, 0.5f);
            GUI.Label(new Rect(r.x + 8, r.y + 24, r.width - 16, 14),
                GameData.FmtDist(GM.DistTo(sel)) + lockText, _smallStyle);
            GUI.contentColor = Color.white;

            float y = r.y + 40;
            if (sel is NpcPirate npc)
            {
                DrawBarRow(r.x + 8, y, r.width - 16, "SHD", npc.Shield / npc.Def.Shield, new Color(0.35f, 0.7f, 1f), Mathf.Round(npc.Shield) + ""); y += 15;
                DrawBarRow(r.x + 8, y, r.width - 16, "ARM", npc.Armor / npc.Def.Armor, new Color(0.75f, 0.78f, 0.82f), Mathf.Round(npc.Armor) + ""); y += 15;
                DrawBarRow(r.x + 8, y, r.width - 16, "HUL", npc.Hull / npc.Def.Hull, new Color(1f, 0.55f, 0.36f), Mathf.Round(npc.Hull) + ""); y += 17;
            }
            else if (sel is AsteroidBody rock)
            {
                GUI.Label(new Rect(r.x + 8, y, r.width - 16, 14),
                    GameData.Ores[rock.Data.Ore].Name + ": " + Mathf.Round(rock.Data.Amount) + " m3 remaining", _smallStyle);
                y += 17;
            }
            else if (sel is Wreck wreck)
            {
                GUI.Label(new Rect(r.x + 8, y, r.width - 16, 14),
                    (wreck.Loot.Count > 0 ? wreck.Loot.Count + " item(s) detected inside" : "Scan inconclusive")
                    + (wreck.BpLoot.Count > 0 ? " + BLUEPRINT" : ""),
                    _smallStyle);
                y += 17;
            }

            // Expected hit quality for the first fitted weapon vs this target.
            if (sel is NpcPirate target)
            {
                foreach (var entry in GM.Ship.Rack)
                {
                    if (entry.Def.Kind != ModuleKind.Weapon) continue;
                    float angVel = Combat.AngularVelocity(
                        target.transform.position - GM.Ship.transform.position,
                        target.Vel - GM.Ship.Vel);
                    int pct = Mathf.RoundToInt(Combat.HitChance(entry.Def.Tracking, angVel) * 100f);
                    GUI.Label(new Rect(r.x + 8, y, r.width - 16, 14),
                        entry.Def.Short + " tracking: ~" + pct + "% hit", _smallStyle);
                    y += 14;
                    break;
                }
            }

            var by = r.yMax - 30;
            float bx = r.x + 8;
            if (GUI.Button(new Rect(bx, by, 70, 22), "Approach")) { GM.Ship.Approach(sel); }
            bx += 74;
            if (GUI.Button(new Rect(bx, by, 54, 22), "Orbit")) { GM.Ship.Orbit(sel, sel is NpcPirate ? 60f : 80f); }
            bx += 58;
            if (GUI.Button(new Rect(bx, by, 50, 22), "Warp")) { GM.WarpToSelected(); }
            bx += 54;
            if (sel.Kind == ObjKind.Station)
            {
                if (GUI.Button(new Rect(bx, by, 50, 22), "Dock")) GM.Dock();
            }
            else if (sel.Kind == ObjKind.Gate)
            {
                if (GUI.Button(new Rect(bx, by, 50, 22), "Jump")) GM.Jump();
            }
            else if (sel.Kind == ObjKind.Wreck)
            {
                if (GUI.Button(new Rect(bx, by, 50, 22), "Loot")) GM.LootWreck();
            }
        }

        void DrawMessages()
        {
            var r = new Rect(280, Screen.height - 110, 460, 96);
            if (Event.current.type == EventType.Repaint) _uiRects.Add(r);
            int n = GM.MessageLog.Count;
            float y = r.yMax - 16;
            for (int i = n - 1; i >= 0 && i > n - 7; i--)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.45f + 0.55f * ((i - (n - 7)) / 6f));
                GUI.Label(new Rect(r.x, y, r.width, 15), GM.MessageLog[i], _smallStyle);
                GUI.color = Color.white;
                y -= 15;
            }
        }

        void DrawCenterText(string text)
        {
            var style = new GUIStyle(_titleStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
            GUI.Label(new Rect(0, Screen.height * 0.22f, Screen.width, 30), text, style);
        }

        // ---------- station ----------

        void DrawStationWindow()
        {
            float w = Mathf.Min(760f, Screen.width - 40f);
            float h = Mathf.Min(520f, Screen.height - 80f);
            var r = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            Panel(r);

            GUI.Label(new Rect(r.x + 12, r.y + 8, w - 24, 20), GM.Station.Name.ToUpper(), _titleStyle);

            string[] tabs = { "Market", "Refine", "Fitting", "Ships", "Industry", "Repair", "Agent" };
            for (int i = 0; i < tabs.Length; i++)
            {
                GUI.backgroundColor = _stationTab == i ? new Color(0.5f, 0.75f, 1f) : Color.white;
                if (GUI.Button(new Rect(r.x + 12 + i * 84, r.y + 34, 80, 24), tabs[i]))
                    _stationTab = i;
                GUI.backgroundColor = Color.white;
            }
            GUI.backgroundColor = new Color(0.45f, 1f, 0.6f);
            if (GUI.Button(new Rect(r.xMax - 110, r.y + 34, 98, 24), "UNDOCK"))
            {
                GM.Undock();
                GUI.backgroundColor = Color.white;
                return;
            }
            GUI.backgroundColor = Color.white;

            var body = new Rect(r.x + 12, r.y + 66, w - 24, h - 78);
            GUILayout.BeginArea(body);
            _stationScroll = GUILayout.BeginScrollView(_stationScroll);
            switch (_stationTab)
            {
                case 0: DrawMarketTab(); break;
                case 1: DrawRefineTab(); break;
                case 2: DrawFittingTab(); break;
                case 3: DrawShipsTab(); break;
                case 4: DrawIndustryTab(); break;
                case 5: DrawRepairTab(); break;
                case 6: DrawAgentTab(); break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawMarketTab()
        {
            var p = GM.Player;
            int trade = p.SkillLevel("trade");

            GUILayout.Label("— SELL ORE & MINERALS —", _smallStyle);
            bool any = false;
            foreach (var id in new List<string>(p.Cargo.Keys))
            {
                float qty = p.Cargo[id];
                if (qty <= 0f) continue;
                any = true;
                long unit = Market.ApplyTradeSkill(Market.OreSellPrice(GM.StationId, id), trade, true);
                GUILayout.BeginHorizontal();
                GUILayout.Label(GameData.Commodity(id).Name + "  ×" + Mathf.Round(qty) + " m3", GUILayout.Width(240));
                GUILayout.Label(unit + " cr/m3", GUILayout.Width(110));
                GUILayout.Label("= " + GameData.FmtCredits((long)(unit * qty)), GUILayout.Width(130));
                if (GUILayout.Button("Sell All", GUILayout.Width(80))) GM.SellCommodity(id);
                GUILayout.EndHorizontal();
            }
            if (!any) GUILayout.Label("Cargo hold is empty. Mine some ore!", _smallStyle);

            GUILayout.Space(14);
            GUILayout.Label("— MODULES FOR SALE —", _smallStyle);
            foreach (var m in GameData.Modules.Values)
            {
                long price = Market.ApplyTradeSkill(Market.ModuleBuyPrice(GM.StationId, m.Id), trade, false);
                GUILayout.BeginHorizontal();
                GUILayout.Label(m.Name + "  [" + m.Slot + "]", GUILayout.Width(240));
                GUILayout.Label(GameData.FmtCredits(price), GUILayout.Width(110));
                GUI.enabled = p.Credits >= price;
                if (GUILayout.Button("Buy", GUILayout.Width(80))) GM.BuyModule(m.Id);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.Label("    " + m.Desc, _smallStyle);
            }
        }

        void DrawRefineTab()
        {
            var p = GM.Player;
            int pct = Mathf.RoundToInt(GM.RefineYield() * 100f);
            GUILayout.Label("— REFINERY —", _smallStyle);
            GUILayout.Label("Current yield: " + pct + "%  (base 66%, +4.5% per Refining level). "
                + "Minerals are lighter and often worth more than raw ore.", _smallStyle);
            GUILayout.Space(8);

            bool any = false;
            foreach (var id in new List<string>(p.Cargo.Keys))
            {
                if (!GameData.Ores.TryGetValue(id, out var def) || def.RefineInto == null) continue;
                float qty = p.Cargo[id];
                if (qty <= 0f) continue;
                any = true;
                string outputs = "";
                foreach (var kv in def.RefineInto)
                    outputs += (outputs.Length > 0 ? ", " : "")
                        + Mathf.Round(qty * GM.RefineYield() * kv.Value) + " "
                        + GameData.Minerals[kv.Key].Name;
                GUILayout.BeginHorizontal();
                GUILayout.Label(def.Name + "  ×" + Mathf.Round(qty) + " m3", GUILayout.Width(220));
                GUILayout.Label("→  " + outputs + " (m3)", GUILayout.Width(330));
                if (GUILayout.Button("Refine", GUILayout.Width(80)))
                {
                    GM.RefineOre(id);
                    GUILayout.EndHorizontal();
                    break;
                }
                GUILayout.EndHorizontal();
            }
            if (!any) GUILayout.Label("No refinable ore in your cargo hold.", _smallStyle);
        }

        void DrawFittingTab()
        {
            var p = GM.Player;
            var st = p.ComputeStats();

            GUILayout.Label("— FITTED (" + p.Hull.Name + ") —", _smallStyle);
            foreach (var slot in Slots.All)
            {
                if (!p.Fitting.ContainsKey(slot)) continue;
                var arr = p.Fitting[slot];
                string slotName = slot == SlotType.Web ? "Web"
                    : slot == SlotType.Disruptor ? "Disruptor"
                    : slot == SlotType.High && p.Hull.TurretOnly ? "Turret" : slot.ToString();
                for (int i = 0; i < arr.Length; i++)
                {
                    GUILayout.BeginHorizontal();
                    string label = slotName + " " + (i + 1) + ":  "
                        + (string.IsNullOrEmpty(arr[i]) ? "<empty>" : GameData.Modules[arr[i]].Name);
                    GUILayout.Label(label, GUILayout.Width(320));
                    if (!string.IsNullOrEmpty(arr[i])
                        && GUILayout.Button("Unfit", GUILayout.Width(80)))
                        GM.UnfitModule(slot, i);
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(14);
            GUILayout.Label("— HANGAR —", _smallStyle);
            if (p.Hangar.Count == 0) GUILayout.Label("No spare modules. Buy some on the market.", _smallStyle);
            for (int i = 0; i < p.Hangar.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(GameData.Modules[p.Hangar[i]].Name
                    + "  [" + GameData.Modules[p.Hangar[i]].Slot + "]", GUILayout.Width(320));
                if (GUILayout.Button("Fit", GUILayout.Width(80))) { GM.FitModule(i); GUILayout.EndHorizontal(); break; }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(14);
            GUILayout.Label("— SHIP STATS —", _smallStyle);
            GUILayout.Label(
                "Shield " + Mathf.Round(st.MaxShield) + "   Armor " + Mathf.Round(st.MaxArmor)
                + "   Hull " + Mathf.Round(st.MaxHull) + "\nCap " + Mathf.Round(st.MaxCap)
                + " (+" + st.CapRegen.ToString("0.0") + "/s)   Cargo " + Mathf.Round(st.CargoCap)
                + " m3   Speed " + Mathf.Round(st.Speed * GameData.UnitsToMs) + " m/s", _smallStyle);
        }

        void DrawShipsTab()
        {
            var p = GM.Player;
            int trade = p.SkillLevel("trade");
            foreach (var s in GameData.Ships.Values)
            {
                bool current = s.Id == p.HullId;
                long price = Market.ApplyTradeSkill(Market.ShipBuyPrice(GM.StationId, s.Id), trade, false);
                long tradeIn = Market.ShipTradeInValue(GM.StationId, p.HullId);
                GUILayout.BeginHorizontal();
                GUILayout.Label((current ? "▶ " : "") + s.Name + "  (" + s.Class + ")", GUILayout.Width(260));
                GUILayout.Label(current ? "ACTIVE" : GameData.FmtCredits(price - tradeIn) + " after trade-in", GUILayout.Width(220));
                if (!current)
                {
                    GUI.enabled = p.Credits >= price - tradeIn;
                    if (GUILayout.Button("Buy & Board", GUILayout.Width(110))) GM.BuyShip(s.Id);
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
                GUILayout.Label("    " + s.Desc + "  |  Cargo " + s.Cargo + " m3, "
                    + s.HighSlots + "H/" + s.MidSlots + "M/" + s.LowSlots + "L, "
                    + Mathf.Round(s.Speed * GameData.UnitsToMs) + " m/s", _smallStyle);
                GUILayout.Space(6);
            }
        }

        void DrawIndustryTab()
        {
            var p = GM.Player;
            GUILayout.Label("— SHIP MANUFACTURING —", _smallStyle);
            if (p.Blueprints.Count == 0)
            {
                GUILayout.Label("No blueprints. Loot pirate wrecks — convoy haulers are the best source.", _smallStyle);
                return;
            }
            foreach (var bp in new List<Blueprint>(p.Blueprints))
            {
                var def = ShipGen.Def(bp);
                GUILayout.Label(ShipGen.DescribeBlueprint(bp) + "  ·  body #" + bp.Hash);
                GUILayout.Label("    " + def.Class + " · Turrets " + def.HighSlots + " · Webs " + def.WebSlots
                    + (def.DisruptorSlots > 0 ? " · Disr " + def.DisruptorSlots : "")
                    + " · " + Mathf.Round(def.Speed * GameData.UnitsToMs) + " m/s", _smallStyle);
                string blocker = GM.ManufactureBlocker(bp);
                if (blocker == null)
                {
                    if (GUILayout.Button("Manufacture", GUILayout.Width(110))) { GM.Manufacture(bp); break; }
                }
                else
                {
                    GUILayout.Label("    " + blocker, _smallStyle);
                }
                GUILayout.Space(6);
            }
        }

        void DrawRepairTab()
        {
            long cost = GM.RepairCost();
            GUILayout.Label(cost <= 0
                ? "Your ship is in perfect condition."
                : "Full armor and hull repair: " + GameData.FmtCredits(cost));
            if (cost > 0)
            {
                GUI.enabled = GM.Player.Credits >= cost;
                if (GUILayout.Button("Repair", GUILayout.Width(120))) GM.Repair();
                GUI.enabled = true;
            }
        }

        string MissionProgress(Mission m) => Missions.ProgressText(GM, m);

        void DrawMissionTracker()
        {
            var m = GM.ActiveMission;
            if (m == null) return;
            var r = new Rect(Screen.width / 2f - 260, 32, 520, 38);
            Panel(r);
            GUI.contentColor = new Color(1f, 0.85f, 0.5f);
            GUI.Label(new Rect(r.x + 8, r.y + 3, r.width - 16, 16), "MISSION: " + m.Title, _smallStyle);
            GUI.contentColor = Color.white;
            GUI.Label(new Rect(r.x + 8, r.y + 19, r.width - 16, 16), MissionProgress(m), _smallStyle);
        }

        void DrawAgentTab()
        {
            var m = GM.ActiveMission;
            if (m != null)
            {
                GUILayout.Label("— ACTIVE MISSION —", _smallStyle);
                GUILayout.Label(m.Title);
                GUILayout.Label(m.Desc, _smallStyle);
                GUILayout.Label("Progress: " + MissionProgress(m), _smallStyle);
                GUILayout.Label("Reward: " + GameData.FmtCredits(m.Reward));
                GUILayout.Space(8);
                GUILayout.BeginHorizontal();
                if (GM.CanTurnInMission())
                {
                    GUI.backgroundColor = new Color(0.45f, 1f, 0.6f);
                    if (GUILayout.Button("Complete Mission", GUILayout.Width(150))) GM.TurnInMission();
                    GUI.backgroundColor = Color.white;
                }
                if (GUILayout.Button("Abandon", GUILayout.Width(90))) GM.AbandonMission();
                GUILayout.EndHorizontal();
                return;
            }

            GUILayout.Label("— AVAILABLE CONTRACTS —", _smallStyle);
            GUILayout.Label("One active mission at a time. New offers appear after each accept or turn-in.", _smallStyle);
            GUILayout.Space(6);
            foreach (var offer in GM.StationOffers())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(offer.Title, GUILayout.Width(330));
                GUILayout.Label(GameData.FmtCredits(offer.Reward), GUILayout.Width(120));
                if (GUILayout.Button("Accept", GUILayout.Width(80)))
                {
                    GM.AcceptMission(offer);
                    GUILayout.EndHorizontal();
                    break;
                }
                GUILayout.EndHorizontal();
                GUILayout.Label("    " + offer.Desc, _smallStyle);
                GUILayout.Space(6);
            }
        }

        void DrawSkillsWindow()
        {
            float w = 430f, h = Mathf.Min(420f, Screen.height - 80f);
            var r = new Rect(30, 60, w, h);
            Panel(r);
            GUI.Label(new Rect(r.x + 12, r.y + 8, w - 24, 20), "SKILL TRAINING", _titleStyle);
            if (GUI.Button(new Rect(r.xMax - 30, r.y + 6, 22, 22), "×")) _showSkills = false;

            var body = new Rect(r.x + 12, r.y + 36, w - 24, h - 48);
            GUILayout.BeginArea(body);
            _skillsScroll = GUILayout.BeginScrollView(_skillsScroll);
            var p = GM.Player;
            foreach (var sk in GameData.Skills.Values)
            {
                bool training = p.ActiveSkill == sk.Id;
                var s = p.Skills[sk.Id];
                GUILayout.BeginHorizontal();
                GUI.contentColor = training ? new Color(0.5f, 1f, 0.65f) : Color.white;
                GUILayout.Label(sk.Name + "  —  Level " + s.Level
                    + (training ? "  (training)" : ""), GUILayout.Width(280));
                GUI.contentColor = Color.white;
                if (!training && s.Level < GameData.SkillMaxLevel
                    && GUILayout.Button("Train", GUILayout.Width(70)))
                    GM.SetTraining(sk.Id);
                GUILayout.EndHorizontal();
                GUILayout.Label("    " + sk.Desc + "  ("
                    + Mathf.RoundToInt(p.SkillProgress(sk.Id) * 100f) + "% to next)", _smallStyle);
                GUILayout.Space(4);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
