using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceGame
{
    /// <summary>
    /// The primary HUD, built with UI Toolkit entirely from code — runtime
    /// PanelSettings, no UXML/USS assets. Mirrors the legacy IMGUI HUD
    /// (which stays available via F10 / automatic fallback, see UiSwitcher).
    /// </summary>
    public class UitHud : MonoBehaviour
    {
        public static UitHud Instance { get; private set; }

        UIDocument _doc;
        VisualElement _root;
        readonly HashSet<VisualElement> _hovered = new HashSet<VisualElement>();

        // top / mission
        Label _sysLabel, _creditsLabel;
        VisualElement _missionWrap;
        Label _missionTitle, _missionProgress;

        // ship status
        VisualElement _hudPanel;
        Bar _shield, _armor, _hullBar, _cap, _cargo;
        Label _speedLabel;

        // target
        VisualElement _targetPanel, _tBars, _targetButtons;
        Label _targetName, _targetInfo, _targetExtra;
        Bar _tShield, _tArmor, _tHull;
        Button _btnDock, _btnJump, _btnLoot;

        // rack
        VisualElement _rackWrap;
        string _rackSig = "";
        class RackEl { public ShipController.RackEntry Entry; public Button Btn; public VisualElement Fill; }
        readonly List<RackEl> _rackEls = new List<RackEl>();

        // overview / messages / windows
        ScrollView _overviewList;
        VisualElement _overviewPanel, _messagesWrap, _stationOverlay, _stationWin, _skillsWin;
        Label _warpBanner, _stationName;
        ScrollView _stationContent, _skillsContent;
        int _stationTab;
        bool _skillsVisible;
        bool _mapVisible;
        VisualElement _mapOverlay, _mapWin;
        readonly Dictionary<string, VisualElement> _mapNodes = new Dictionary<string, VisualElement>();
        VisualElement _routeWrap;
        Label _routeLabel;
        bool _wasDocked = true;
        int _lastMsgCount = -1;
        float _refreshT;
        float _slowRefreshT; // station/skills rebuild cadence (buttons live here)

        GameManager GM => GameManager.I;

        // ---------- lifecycle ----------

        public static bool TryCreate()
        {
            if (Instance != null) return true;
            GameObject go = null;
            try
            {
                go = new GameObject("UIToolkitHUD");
                go.SetActive(false);
                var doc = go.AddComponent<UIDocument>();
                var ps = ScriptableObject.CreateInstance<PanelSettings>();
                ps.scaleMode = PanelScaleMode.ConstantPixelSize;
                doc.panelSettings = ps;
                go.SetActive(true);
                if (doc.rootVisualElement == null)
                    throw new System.Exception("UIDocument produced no root element");
                var hud = go.AddComponent<UitHud>();
                hud._doc = doc;
                hud.Build();
                Instance = hud;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("UI Toolkit HUD unavailable, falling back to IMGUI: " + e.Message);
                if (go != null) Destroy(go);
                return false;
            }
        }

        public static void DestroyInstance()
        {
            if (Instance == null) return;
            Destroy(Instance.gameObject);
            Instance = null;
            HudUI.MouseOverUI = false;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                HudUI.MouseOverUI = false;
            }
        }

        // ---------- element helpers ----------

        static void Abs(VisualElement e) { e.style.position = Position.Absolute; }

        void TrackHover(VisualElement e)
        {
            e.RegisterCallback<PointerEnterEvent>(_ => _hovered.Add(e));
            e.RegisterCallback<PointerLeaveEvent>(_ => _hovered.Remove(e));
        }

        VisualElement Panel()
        {
            var p = new VisualElement();
            Abs(p);
            p.style.backgroundColor = UiSkin.PanelBg;
            p.style.borderLeftWidth = 1;
            p.style.borderRightWidth = 1;
            p.style.borderBottomWidth = 1;
            p.style.borderTopWidth = 2;
            p.style.borderLeftColor = UiSkin.PanelBorder;
            p.style.borderRightColor = UiSkin.PanelBorder;
            p.style.borderBottomColor = UiSkin.PanelBorder;
            p.style.borderTopColor = new Color(UiSkin.Accent.r, UiSkin.Accent.g, UiSkin.Accent.b, 0.6f);
            p.style.paddingLeft = 8;
            p.style.paddingRight = 8;
            p.style.paddingTop = 6;
            p.style.paddingBottom = 6;
            TrackHover(p);
            return p;
        }

        static Label Text(string t, float size, Color c, bool bold = false)
        {
            var l = new Label(t);
            l.pickingMode = PickingMode.Ignore;
            l.style.fontSize = size;
            l.style.color = c;
            l.style.marginTop = 1;
            l.style.marginBottom = 1;
            if (bold) l.style.unityFontStyleAndWeight = FontStyle.Bold;
            return l;
        }

        static Button Btn(string t, System.Action onClick)
        {
            var b = new Button(() => { Sfx.Click(); onClick(); }) { text = t };
            var baseBg = new Color(0.08f, 0.14f, 0.23f);
            b.style.backgroundColor = baseBg;
            b.style.color = UiSkin.TextMain;
            b.style.fontSize = 11;
            b.style.borderLeftWidth = 1;
            b.style.borderRightWidth = 1;
            b.style.borderTopWidth = 1;
            b.style.borderBottomWidth = 1;
            b.style.borderLeftColor = UiSkin.PanelBorder;
            b.style.borderRightColor = UiSkin.PanelBorder;
            b.style.borderTopColor = UiSkin.PanelBorder;
            b.style.borderBottomColor = UiSkin.PanelBorder;
            b.style.paddingLeft = 8;
            b.style.paddingRight = 8;
            b.style.paddingTop = 3;
            b.style.paddingBottom = 3;
            b.style.marginLeft = 2;
            b.style.marginRight = 2;
            b.style.marginTop = 1;
            b.style.marginBottom = 1;
            b.RegisterCallback<PointerEnterEvent>(_ => b.style.backgroundColor = new Color(0.12f, 0.22f, 0.35f));
            b.RegisterCallback<PointerLeaveEvent>(_ => b.style.backgroundColor = baseBg);
            return b;
        }

        static VisualElement Row(params VisualElement[] cells)
        {
            var r = new VisualElement();
            r.style.flexDirection = FlexDirection.Row;
            r.style.alignItems = Align.Center;
            r.style.marginBottom = 2;
            foreach (var c in cells) r.Add(c);
            return r;
        }

        static Label Cell(string t, float w, Color c, float size = 11)
        {
            var l = Text(t, size, c);
            l.style.width = w;
            return l;
        }

        static Label Section(string t)
        {
            var l = Text(t, 10, UiSkin.Accent, true);
            l.style.marginTop = 8;
            l.style.marginBottom = 4;
            return l;
        }

        static Label WrapText(string t, Color c)
        {
            var l = Text(t, 10, c);
            l.style.whiteSpace = WhiteSpace.Normal;
            return l;
        }

        class Bar
        {
            public VisualElement Fill;
            public Label Value;
        }

        Bar MakeBar(VisualElement parent, string label, Color c)
        {
            var lab = Text(label, 9, UiSkin.TextDim);
            lab.style.width = 30;
            var track = new VisualElement();
            track.pickingMode = PickingMode.Ignore;
            track.style.flexGrow = 1;
            track.style.height = 8;
            track.style.backgroundColor = new Color(0.05f, 0.09f, 0.15f);
            track.style.overflow = Overflow.Hidden;
            var fill = new VisualElement();
            fill.pickingMode = PickingMode.Ignore;
            Abs(fill);
            fill.style.left = 0;
            fill.style.top = 0;
            fill.style.bottom = 0;
            fill.style.width = Length.Percent(100);
            fill.style.backgroundColor = c;
            track.Add(fill);
            var val = Text("", 9, UiSkin.TextDim);
            val.style.width = 76;
            val.style.unityTextAlign = TextAnchor.MiddleRight;
            var row = Row(lab, track, val);
            row.style.marginBottom = 3;
            parent.Add(row);
            return new Bar { Fill = fill, Value = val };
        }

        static void SetBar(Bar b, float frac, string text)
        {
            b.Fill.style.width = Length.Percent(Mathf.Clamp01(frac) * 100f);
            b.Value.text = text;
        }

        // ---------- build ----------

        void Build()
        {
            _root = _doc.rootVisualElement;
            _root.pickingMode = PickingMode.Ignore;
            Abs(_root);
            _root.style.left = 0;
            _root.style.top = 0;
            _root.style.right = 0;
            _root.style.bottom = 0;
            _root.style.color = UiSkin.TextMain;

            // Default runtime font (no theme asset exists in a code-only project).
            Font font = null;
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch (System.Exception) { }
            if (font == null)
            {
                try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch (System.Exception) { }
            }
            if (font != null) _root.style.unityFontDefinition = FontDefinition.FromFont(font);

            BuildTopBar();
            BuildMissionTracker();
            BuildRouteIndicator();
            BuildHudPanel();
            BuildTargetPanel();
            BuildModRack();
            BuildOverview();
            BuildMessages();
            BuildWarpBanner();
            BuildSkillsWindow();
            BuildStationWindow();
            BuildMapWindow();
            BuildHelpHint();
        }

        void BuildRouteIndicator()
        {
            _routeWrap = new VisualElement();
            _routeWrap.pickingMode = PickingMode.Ignore;
            Abs(_routeWrap);
            _routeWrap.style.left = 0;
            _routeWrap.style.right = 0;
            _routeWrap.style.top = 76;
            _routeWrap.style.flexDirection = FlexDirection.Row;
            _routeWrap.style.justifyContent = Justify.Center;
            var inner = Panel();
            inner.style.position = Position.Relative;
            inner.style.flexDirection = FlexDirection.Row;
            inner.style.alignItems = Align.Center;
            _routeLabel = Text("", 10, UiSkin.Accent);
            inner.Add(_routeLabel);
            inner.Add(Btn("Warp to gate", WarpToRouteGate));
            _routeWrap.Add(inner);
            _routeWrap.style.display = DisplayStyle.None;
            _root.Add(_routeWrap);
        }

        void WarpToRouteGate()
        {
            var gm = GM;
            string next = gm.NextRouteHop();
            if (next == null || gm.Docked) return;
            foreach (var o in gm.View.Objects)
            {
                if (o is CelestialBody cb && cb.Kind == ObjKind.Gate && cb.Data.GateTo == next)
                {
                    gm.Select(cb);
                    gm.WarpToSelected();
                    return;
                }
            }
        }

        void BuildTopBar()
        {
            var bar = Panel();
            bar.style.left = 0;
            bar.style.right = 0;
            bar.style.top = 0;
            bar.style.borderTopWidth = 0;
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.justifyContent = Justify.SpaceBetween;
            bar.style.alignItems = Align.Center;
            _sysLabel = Text("—", 13, UiSkin.TextMain, true);
            _creditsLabel = Text("0 cr", 13, UiSkin.AccentWarm, true);
            bar.Add(_sysLabel);
            bar.Add(_creditsLabel);
            _root.Add(bar);
        }

        void BuildMissionTracker()
        {
            var wrap = new VisualElement();
            wrap.pickingMode = PickingMode.Ignore;
            Abs(wrap);
            wrap.style.left = 0;
            wrap.style.right = 0;
            wrap.style.top = 30;
            wrap.style.flexDirection = FlexDirection.Row;
            wrap.style.justifyContent = Justify.Center;
            var inner = Panel();
            inner.style.position = Position.Relative;
            _missionTitle = Text("", 10, UiSkin.AccentWarm, true);
            _missionProgress = Text("", 10, UiSkin.TextMain);
            inner.Add(_missionTitle);
            inner.Add(_missionProgress);
            wrap.Add(inner);
            _missionWrap = wrap;
            _root.Add(wrap);
        }

        void BuildHudPanel()
        {
            _hudPanel = Panel();
            _hudPanel.style.left = 12;
            _hudPanel.style.bottom = 12;
            _hudPanel.style.width = 256;
            _shield = MakeBar(_hudPanel, "SHD", new Color(0.35f, 0.7f, 1f));
            _armor = MakeBar(_hudPanel, "ARM", new Color(0.75f, 0.78f, 0.82f));
            _hullBar = MakeBar(_hudPanel, "HUL", new Color(1f, 0.55f, 0.36f));
            _cap = MakeBar(_hudPanel, "CAP", new Color(1f, 0.84f, 0.37f));
            _cargo = MakeBar(_hudPanel, "CRG", new Color(0.62f, 0.48f, 1f));
            _speedLabel = Text("", 11, UiSkin.Accent);
            _speedLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            _hudPanel.Add(_speedLabel);
            _root.Add(_hudPanel);
        }

        void BuildTargetPanel()
        {
            _targetPanel = Panel();
            _targetPanel.style.left = 12;
            _targetPanel.style.bottom = 172;
            _targetPanel.style.width = 256;
            _targetName = Text("—", 13, UiSkin.Accent, true);
            _targetInfo = Text("", 10, UiSkin.TextDim);
            _targetPanel.Add(_targetName);
            _targetPanel.Add(_targetInfo);
            _tBars = new VisualElement();
            _tShield = MakeBar(_tBars, "SHD", new Color(0.35f, 0.7f, 1f));
            _tArmor = MakeBar(_tBars, "ARM", new Color(0.75f, 0.78f, 0.82f));
            _tHull = MakeBar(_tBars, "HUL", new Color(1f, 0.55f, 0.36f));
            _targetPanel.Add(_tBars);
            _targetExtra = Text("", 10, UiSkin.TextDim);
            _targetPanel.Add(_targetExtra);
            _targetButtons = new VisualElement();
            _targetButtons.style.flexDirection = FlexDirection.Row;
            _targetButtons.style.flexWrap = Wrap.Wrap;
            _targetButtons.style.marginTop = 4;
            _targetButtons.Add(Btn("Approach", () => GM.Ship.Approach(GM.Selected)));
            _targetButtons.Add(Btn("Orbit", () => GM.Ship.Orbit(GM.Selected, GM.Selected is NpcPirate ? 60f : 80f)));
            _targetButtons.Add(Btn("Warp", () => GM.WarpToSelected()));
            _btnDock = Btn("Dock", () => GM.Dock());
            _btnJump = Btn("Jump", () => GM.Jump());
            _btnLoot = Btn("Loot", () => GM.LootWreck());
            _targetButtons.Add(_btnDock);
            _targetButtons.Add(_btnJump);
            _targetButtons.Add(_btnLoot);
            _targetPanel.Add(_targetButtons);
            _root.Add(_targetPanel);
        }

        void BuildModRack()
        {
            _rackWrap = new VisualElement();
            _rackWrap.pickingMode = PickingMode.Ignore;
            Abs(_rackWrap);
            _rackWrap.style.left = 0;
            _rackWrap.style.right = 0;
            _rackWrap.style.bottom = 8;
            _rackWrap.style.flexDirection = FlexDirection.Row;
            _rackWrap.style.justifyContent = Justify.Center;
            _root.Add(_rackWrap);
        }

        void BuildOverview()
        {
            _overviewPanel = Panel();
            _overviewPanel.style.right = 12;
            _overviewPanel.style.top = 30;
            _overviewPanel.style.bottom = 12;
            _overviewPanel.style.width = 286;
            var title = Text("OVERVIEW", 10, UiSkin.TextDim, true);
            title.style.marginBottom = 4;
            _overviewPanel.Add(title);
            _overviewList = new ScrollView();
            _overviewList.style.flexGrow = 1;
            _overviewPanel.Add(_overviewList);
            _root.Add(_overviewPanel);
        }

        void BuildMessages()
        {
            _messagesWrap = new VisualElement();
            _messagesWrap.pickingMode = PickingMode.Ignore;
            Abs(_messagesWrap);
            _messagesWrap.style.left = 286;
            _messagesWrap.style.bottom = 80;
            _messagesWrap.style.width = 470;
            _root.Add(_messagesWrap);
        }

        void BuildWarpBanner()
        {
            _warpBanner = Text("— WARP DRIVE ACTIVE —", 18, UiSkin.TextMain, true);
            Abs(_warpBanner);
            _warpBanner.style.left = 0;
            _warpBanner.style.right = 0;
            _warpBanner.style.top = Length.Percent(22);
            _warpBanner.style.unityTextAlign = TextAnchor.MiddleCenter;
            _warpBanner.style.display = DisplayStyle.None;
            _root.Add(_warpBanner);
        }

        void BuildSkillsWindow()
        {
            _skillsWin = Panel();
            _skillsWin.style.left = 30;
            _skillsWin.style.top = 60;
            _skillsWin.style.width = 440;
            _skillsWin.style.display = DisplayStyle.None;
            var head = Row(Text("SKILL TRAINING", 12, UiSkin.Accent, true));
            head.style.justifyContent = Justify.SpaceBetween;
            var close = Btn("×", () => { _skillsVisible = false; SyncWindows(); });
            head.Add(close);
            _skillsWin.Add(head);
            _skillsContent = new ScrollView();
            _skillsContent.style.maxHeight = 380;
            _skillsWin.Add(_skillsContent);
            _root.Add(_skillsWin);
        }

        void BuildStationWindow()
        {
            _stationOverlay = new VisualElement();
            _stationOverlay.pickingMode = PickingMode.Ignore;
            Abs(_stationOverlay);
            _stationOverlay.style.left = 0;
            _stationOverlay.style.right = 0;
            _stationOverlay.style.top = 0;
            _stationOverlay.style.bottom = 0;
            _stationOverlay.style.alignItems = Align.Center;
            _stationOverlay.style.justifyContent = Justify.Center;
            _stationOverlay.style.display = DisplayStyle.None;

            _stationWin = Panel();
            _stationWin.style.position = Position.Relative;
            _stationWin.style.width = 780;
            _stationWin.style.height = 560;
            _stationName = Text("STATION", 14, UiSkin.Accent, true);
            _stationWin.Add(_stationName);

            var tabRow = new VisualElement();
            tabRow.style.flexDirection = FlexDirection.Row;
            tabRow.style.marginTop = 6;
            tabRow.style.marginBottom = 6;
            string[] tabs = { "Market", "Storage", "Refine", "Fitting", "Ships", "Industry", "Repair", "Agent" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int idx = i;
                tabRow.Add(Btn(tabs[i], () => { _stationTab = idx; RefreshStationTab(); }));
            }
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            tabRow.Add(spacer);
            var undock = Btn("UNDOCK", () => GM.Undock());
            undock.style.borderLeftColor = new Color(0.45f, 1f, 0.6f);
            undock.style.borderRightColor = new Color(0.45f, 1f, 0.6f);
            undock.style.borderTopColor = new Color(0.45f, 1f, 0.6f);
            undock.style.borderBottomColor = new Color(0.45f, 1f, 0.6f);
            undock.style.color = new Color(0.45f, 1f, 0.6f);
            tabRow.Add(undock);
            _stationWin.Add(tabRow);

            _stationContent = new ScrollView();
            _stationContent.style.flexGrow = 1;
            _stationWin.Add(_stationContent);

            _stationOverlay.Add(_stationWin);
            _root.Add(_stationOverlay);
        }

        // Hand-laid node positions (normalized within the map area).
        static readonly string[] MapSystems = { "solara", "verdant", "krios", "nadir", "abyss" };
        static readonly Vector2[] MapPos =
        {
            new Vector2(0.08f, 0.50f), new Vector2(0.33f, 0.32f), new Vector2(0.56f, 0.62f),
            new Vector2(0.64f, 0.18f), new Vector2(0.90f, 0.30f),
        };

        static Vector2 NodePx(string sysId, float w, float h)
        {
            for (int i = 0; i < MapSystems.Length; i++)
                if (MapSystems[i] == sysId)
                    return new Vector2(40f + MapPos[i].x * (w - 80f), 30f + MapPos[i].y * (h - 80f));
            return new Vector2(w / 2f, h / 2f);
        }

        static Color SecColor(float sec)
            => new Color(1f - sec * 0.65f, 0.35f + sec * 0.55f, 0.3f);

        void BuildMapWindow()
        {
            _mapOverlay = new VisualElement();
            _mapOverlay.pickingMode = PickingMode.Ignore;
            Abs(_mapOverlay);
            _mapOverlay.style.left = 0;
            _mapOverlay.style.right = 0;
            _mapOverlay.style.top = 0;
            _mapOverlay.style.bottom = 0;
            _mapOverlay.style.alignItems = Align.Center;
            _mapOverlay.style.justifyContent = Justify.Center;
            _mapOverlay.style.display = DisplayStyle.None;

            var win = Panel();
            _mapWin = win;
            win.style.position = Position.Relative;
            win.style.width = 660;
            win.style.height = 440;

            var head = Row(Text("GALAXY MAP", 13, UiSkin.Accent, true));
            head.style.justifyContent = Justify.SpaceBetween;
            head.Add(Btn("×", () => { _mapVisible = false; SyncWindows(); }));
            win.Add(head);
            win.Add(WrapText("Click a system to set (or clear) your route. The route bar in "
                + "space warps you gate to gate.", UiSkin.TextDim));

            const float mapW = 620f, mapH = 330f;
            var area = new VisualElement();
            area.style.position = Position.Relative;
            area.style.width = mapW;
            area.style.height = mapH;

            // Starlanes as dotted trails between systems.
            foreach (var pair in UniverseGenerator.GatePairs)
            {
                var a = NodePx(pair[0], mapW, mapH);
                var b = NodePx(pair[1], mapW, mapH);
                for (int i = 1; i < 13; i++)
                {
                    float t = i / 13f;
                    var dot = new VisualElement();
                    dot.pickingMode = PickingMode.Ignore;
                    Abs(dot);
                    dot.style.left = a.x + (b.x - a.x) * t - 1.5f;
                    dot.style.top = a.y + (b.y - a.y) * t - 1.5f;
                    dot.style.width = 3;
                    dot.style.height = 3;
                    dot.style.backgroundColor = new Color(0.3f, 0.4f, 0.55f, 0.8f);
                    area.Add(dot);
                }
            }

            _mapNodes.Clear();
            foreach (var sysId in MapSystems)
            {
                var sys = GM != null && GM.Universe != null ? GM.Universe.Systems[sysId] : null;
                var p = NodePx(sysId, mapW, mapH);
                string id = sysId;

                var node = new VisualElement();
                Abs(node);
                node.style.left = p.x - 20f;
                node.style.top = p.y - 20f;
                node.style.width = 40;
                node.style.height = 40;
                node.style.backgroundColor = new Color(0.05f, 0.09f, 0.15f);
                node.style.borderTopLeftRadius = 20;
                node.style.borderTopRightRadius = 20;
                node.style.borderBottomLeftRadius = 20;
                node.style.borderBottomRightRadius = 20;
                node.style.borderLeftWidth = 2;
                node.style.borderRightWidth = 2;
                node.style.borderTopWidth = 2;
                node.style.borderBottomWidth = 2;
                node.style.alignItems = Align.Center;
                node.style.justifyContent = Justify.Center;
                node.RegisterCallback<PointerDownEvent>(_ =>
                {
                    Sfx.Click();
                    GM.SetDestination(id);
                    RefreshMap();
                });
                var secLabel = Text(sys != null ? sys.Sec.ToString("0.0") : "?", 10,
                    sys != null ? SecColor(sys.Sec) : Color.white, true);
                node.Add(secLabel);
                area.Add(node);
                _mapNodes[sysId] = node;

                var name = Text(sys != null ? sys.Name : sysId, 11, UiSkin.TextMain, true);
                Abs(name);
                name.style.left = p.x - 50f;
                name.style.top = p.y + 24f;
                name.style.width = 100;
                name.style.unityTextAlign = TextAnchor.MiddleCenter;
                area.Add(name);
            }

            win.Add(area);
            _mapOverlay.Add(win);
            _root.Add(_mapOverlay);
        }

        void RefreshMap()
        {
            var gm = GM;
            var route = gm.RouteDest != null
                ? Missions.RoutePath(gm.SystemId, gm.RouteDest)
                : new List<string>();
            foreach (var kv in _mapNodes)
            {
                Color c;
                if (kv.Key == gm.SystemId) c = Color.white;
                else if (kv.Key == gm.RouteDest) c = UiSkin.AccentWarm;
                else if (route.Contains(kv.Key)) c = new Color(UiSkin.AccentWarm.r, UiSkin.AccentWarm.g, UiSkin.AccentWarm.b, 0.55f);
                else c = SecColor(gm.Universe.Systems[kv.Key].Sec);
                kv.Value.style.borderLeftColor = c;
                kv.Value.style.borderRightColor = c;
                kv.Value.style.borderTopColor = c;
                kv.Value.style.borderBottomColor = c;
            }
        }

        void BuildHelpHint()
        {
            var hint = Text("W/S throttle · A/D turn · X stop · click select · 1-8 modules · K skills · M map · F9 mute · F10 legacy UI", 9, new Color(0.28f, 0.34f, 0.42f));
            Abs(hint);
            hint.style.left = 12;
            hint.style.top = 30;
            _root.Add(hint);
        }

        // ---------- per-frame sync ----------

        void Update()
        {
            var gm = GM;
            if (gm == null || !gm.Ready || _root == null) return;

            HudUI.MouseOverUI = _hovered.Count > 0;
            HandleInput(gm);

            bool docked = gm.Docked;
            if (docked != _wasDocked)
            {
                _wasDocked = docked;
                if (docked) RefreshStationTab();
            }

            _sysLabel.text = gm.System.Name.ToUpper() + "   <sec " + gm.System.Sec.ToString("0.0") + ">"
                + (docked ? "   [DOCKED: " + gm.Station.Name + "]" : "");
            _creditsLabel.text = GameData.StandingTier(gm.Player.Standing)
                + "  ·  " + GameData.FmtCredits(gm.Player.Credits);

            var show = docked ? DisplayStyle.None : DisplayStyle.Flex;
            _hudPanel.style.display = show;
            _overviewPanel.style.display = show;
            _rackWrap.style.display = show;
            _stationOverlay.style.display = docked ? DisplayStyle.Flex : DisplayStyle.None;
            _warpBanner.style.display = !docked && gm.Ship.InWarp ? DisplayStyle.Flex : DisplayStyle.None;

            var m = gm.ActiveMission;
            _missionWrap.style.display = m != null && !docked ? DisplayStyle.Flex : DisplayStyle.None;
            if (m != null)
            {
                _missionTitle.text = "MISSION: " + m.Title;
                _missionProgress.text = Missions.ProgressText(gm, m);
            }

            // Route bar: next hop + jumps remaining.
            string nextHop = gm.NextRouteHop();
            _routeWrap.style.display = nextHop != null && !docked ? DisplayStyle.Flex : DisplayStyle.None;
            if (nextHop != null)
            {
                int left = gm.RouteJumpsLeft();
                _routeLabel.text = "ROUTE: " + left + " jump" + (left == 1 ? "" : "s") + " to "
                    + gm.Universe.Systems[gm.RouteDest].Name
                    + "  ·  next gate: " + gm.Universe.Systems[nextHop].Name + "  ";
            }

            if (!docked)
            {
                SyncStatus(gm);
                SyncTarget(gm);
                SyncRack();
            }
            else
            {
                _targetPanel.style.display = DisplayStyle.None;
                _stationName.text = gm.Station.Name.ToUpper();
            }

            _refreshT -= Time.deltaTime;
            if (_refreshT <= 0f)
            {
                _refreshT = 0.2f;
                if (!docked) RefreshOverview(gm);
                RefreshRackStructure(gm);
                RefreshMessages(gm);
            }

            // Windows with buttons rebuild slowly so presses don't race rebuilds
            // (their buttons also refresh immediately after every action).
            _slowRefreshT -= Time.deltaTime;
            if (_slowRefreshT <= 0f)
            {
                _slowRefreshT = 1f;
                if (docked) RefreshStationTab();
                if (_skillsVisible) RefreshSkills(gm);
            }
        }

        void HandleInput(GameManager gm)
        {
            if (Input.GetMouseButtonDown(0) && !HudUI.MouseOverUI && !gm.Docked && Camera.main != null)
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 2000000f))
                {
                    var obj = hit.collider.GetComponentInParent<SpaceObject>();
                    if (obj != null) gm.Select(obj);
                }
            }
            for (int i = 0; i < 8; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    gm.Ship.ToggleModule(i);
            if (Input.GetKeyDown(KeyCode.K)) { _skillsVisible = !_skillsVisible; SyncWindows(); if (_skillsVisible) RefreshSkills(gm); }
            if (Input.GetKeyDown(KeyCode.M)) { _mapVisible = !_mapVisible; SyncWindows(); if (_mapVisible) RefreshMap(); }
            if (Input.GetKeyDown(KeyCode.Escape)) { _skillsVisible = false; _mapVisible = false; SyncWindows(); }
        }

        void SyncWindows()
        {
            _skillsWin.style.display = _skillsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!_skillsVisible) _hovered.Remove(_skillsWin);
            _mapOverlay.style.display = _mapVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!_mapVisible && _mapWin != null) _hovered.Remove(_mapWin);
        }

        void SyncStatus(GameManager gm)
        {
            var p = gm.Player;
            var st = p.ComputeStats();
            SetBar(_shield, p.Shield / st.MaxShield, Mathf.Round(p.Shield) + "/" + Mathf.Round(st.MaxShield));
            SetBar(_armor, p.Armor / st.MaxArmor, Mathf.Round(p.Armor) + "/" + Mathf.Round(st.MaxArmor));
            SetBar(_hullBar, p.HullHp / st.MaxHull, Mathf.Round(p.HullHp) + "/" + Mathf.Round(st.MaxHull));
            SetBar(_cap, p.Cap / st.MaxCap, Mathf.Round(p.Cap) + "/" + Mathf.Round(st.MaxCap));
            SetBar(_cargo, p.CargoUsed() / st.CargoCap, Mathf.Round(p.CargoUsed()) + "/" + Mathf.Round(st.CargoCap) + " m3");
            _speedLabel.text = p.Hull.Name + "   " + Mathf.Round(gm.Ship.Vel.magnitude * GameData.UnitsToMs) + " m/s";
        }

        void SyncTarget(GameManager gm)
        {
            var sel = gm.Selected;
            if (sel == null)
            {
                _targetPanel.style.display = DisplayStyle.None;
                _hovered.Remove(_targetPanel);
                return;
            }
            _targetPanel.style.display = DisplayStyle.Flex;
            _targetName.text = sel.DisplayName;
            _targetName.style.color = HudUI.KindColor(sel.Kind);

            string lockText = "";
            if (sel is NpcPirate || sel is AsteroidBody)
            {
                if (gm.Locked) lockText = "  [LOCKED]";
                else if (gm.DistTo(sel) > GameManager.LockRange) lockText = "  [OUT OF LOCK RANGE]";
                else lockText = "  [LOCKING " + Mathf.RoundToInt(gm.LockProgress * 100f) + "%]";
            }
            _targetInfo.text = GameData.FmtDist(gm.DistTo(sel)) + lockText;
            _targetInfo.style.color = gm.Locked ? new Color(0.5f, 1f, 0.65f) : new Color(1f, 0.85f, 0.5f);

            if (sel is NpcPirate npc)
            {
                _tBars.style.display = DisplayStyle.Flex;
                SetBar(_tShield, npc.Shield / npc.Def.Shield, Mathf.Round(npc.Shield).ToString());
                SetBar(_tArmor, npc.Armor / npc.Def.Armor, Mathf.Round(npc.Armor).ToString());
                SetBar(_tHull, npc.Hull / npc.Def.Hull, Mathf.Round(npc.Hull).ToString());
            }
            else
            {
                _tBars.style.display = DisplayStyle.None;
            }

            string extra = "";
            if (sel is AsteroidBody rock)
                extra = GameData.Ores[rock.Data.Ore].Name + ": " + Mathf.Round(rock.Data.Amount) + " m3 remaining";
            else if (sel is Wreck wreck)
            {
                extra = wreck.ScrapM3() > 0f
                    ? Mathf.Round(wreck.ScrapM3()) + " m3 scrap detected inside"
                    : wreck.Loot.Count > 0 ? wreck.Loot.Count + " item(s) detected inside"
                    : "Scan inconclusive";
                if (wreck.BpLoot.Count > 0) extra += "  +  BLUEPRINT SIGNATURE";
            }
            else if (sel is NpcPirate target2)
            {
                foreach (var entry in gm.Ship.Rack)
                {
                    if (entry.Def.Kind != ModuleKind.Weapon) continue;
                    float angVel = Combat.AngularVelocity(
                        target2.transform.position - gm.Ship.transform.position,
                        target2.Vel - gm.Ship.Vel);
                    extra = entry.Def.Short + " tracking: ~"
                        + Mathf.RoundToInt(Combat.HitChance(entry.Def.Tracking, angVel) * 100f) + "% hit";
                    break;
                }
            }
            _targetExtra.text = extra;
            _targetExtra.style.display = extra.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            _btnDock.style.display = sel.Kind == ObjKind.Station ? DisplayStyle.Flex : DisplayStyle.None;
            _btnJump.style.display = sel.Kind == ObjKind.Gate ? DisplayStyle.Flex : DisplayStyle.None;
            _btnLoot.style.display = sel.Kind == ObjKind.Wreck ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ---------- module rack ----------

        void RefreshRackStructure(GameManager gm)
        {
            var rack = gm.Ship.Rack;
            var sig = "";
            foreach (var r in rack) sig += r.ModId + ",";
            if (sig == _rackSig) return;
            _rackSig = sig;
            _rackWrap.Clear();
            _rackEls.Clear();
            var panel = Panel();
            panel.style.position = Position.Relative;
            panel.style.flexDirection = FlexDirection.Row;
            for (int i = 0; i < rack.Count; i++)
            {
                int idx = i;
                var entry = rack[i];
                var b = Btn((i + 1) + "\n" + entry.Def.Short, () => gm.Ship.ToggleModule(idx));
                b.style.width = 56;
                b.style.height = 46;
                b.style.whiteSpace = WhiteSpace.Normal;
                b.style.unityTextAlign = TextAnchor.MiddleCenter;
                var fill = new VisualElement();
                fill.pickingMode = PickingMode.Ignore;
                Abs(fill);
                fill.style.left = 0;
                fill.style.bottom = 0;
                fill.style.height = 3;
                fill.style.width = Length.Percent(0);
                fill.style.backgroundColor = new Color(0.4f, 1f, 0.6f);
                b.Add(fill);
                panel.Add(b);
                _rackEls.Add(new RackEl { Entry = entry, Btn = b, Fill = fill });
            }
            if (rack.Count > 0) _rackWrap.Add(panel);
        }

        void SyncRack()
        {
            foreach (var el in _rackEls)
            {
                var active = el.Entry.Active;
                var c = active ? new Color(0.4f, 1f, 0.6f) : UiSkin.PanelBorder;
                el.Btn.style.borderLeftColor = c;
                el.Btn.style.borderRightColor = c;
                el.Btn.style.borderTopColor = c;
                el.Btn.style.borderBottomColor = c;
                float frac = active && el.Entry.Def.Cycle > 0.01f && el.Entry.Def.Kind != ModuleKind.Afterburner
                    ? el.Entry.T / el.Entry.Def.Cycle : 0f;
                el.Fill.style.width = Length.Percent(Mathf.Clamp01(frac) * 100f);
            }
        }

        // ---------- overview / messages ----------

        void RefreshOverview(GameManager gm)
        {
            _overviewList.Clear();
            foreach (var o in gm.OverviewEntries())
            {
                if (o == null) continue;
                var obj = o;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.paddingTop = 1;
                row.style.paddingBottom = 1;
                row.style.paddingLeft = 4;
                if (gm.Selected == obj)
                    row.style.backgroundColor = new Color(0.35f, 0.7f, 1f, 0.22f);
                // PointerDown, not Click: rows are rebuilt periodically and a
                // rebuild between press and release would swallow a Click.
                row.RegisterCallback<PointerDownEvent>(_ => gm.Select(obj));
                row.RegisterCallback<PointerEnterEvent>(_ => row.style.backgroundColor = new Color(0.35f, 0.7f, 1f, 0.12f));
                row.RegisterCallback<PointerLeaveEvent>(_ => row.style.backgroundColor =
                    gm.Selected == obj ? new Color(0.35f, 0.7f, 1f, 0.22f) : new Color(0f, 0f, 0f, 0f));
                var c = HudUI.KindColor(obj.Kind);
                var icon = Text(HudUI.KindIcon(obj.Kind), 11, c);
                icon.style.width = 16;
                var name = Text(obj.DisplayName, 11, c);
                name.style.flexGrow = 1;
                name.style.overflow = Overflow.Hidden;
                var d = Text(GameData.FmtDist(gm.DistTo(obj)), 9, UiSkin.TextDim);
                row.Add(icon);
                row.Add(name);
                row.Add(d);
                _overviewList.Add(row);
            }
        }

        void RefreshMessages(GameManager gm)
        {
            if (gm.MessageLog.Count == _lastMsgCount) return;
            _lastMsgCount = gm.MessageLog.Count;
            _messagesWrap.Clear();
            int n = gm.MessageLog.Count;
            for (int i = Mathf.Max(0, n - 6); i < n; i++)
            {
                var l = Text(gm.MessageLog[i], 10, UiSkin.TextMain);
                l.style.opacity = 0.45f + 0.55f * ((i - (n - 6)) / 6f);
                _messagesWrap.Add(l);
            }
        }

        // ---------- skills ----------

        void RefreshSkills(GameManager gm)
        {
            _skillsContent.Clear();
            var p = gm.Player;
            foreach (var sk in GameData.Skills.Values)
            {
                bool training = p.ActiveSkill == sk.Id;
                var s = p.Skills[sk.Id];
                var name = Cell(sk.Name + "  —  Level " + s.Level + (training ? "  (training)" : ""), 290,
                    training ? new Color(0.5f, 1f, 0.65f) : UiSkin.TextMain);
                var row = Row(name);
                if (!training && s.Level < GameData.SkillMaxLevel)
                {
                    string id = sk.Id;
                    row.Add(Btn("Train", () => { gm.SetTraining(id); RefreshSkills(gm); }));
                }
                _skillsContent.Add(row);
                _skillsContent.Add(WrapText(sk.Desc + "  ("
                    + Mathf.RoundToInt(p.SkillProgress(sk.Id) * 100f) + "% to next)", UiSkin.TextDim));
            }
        }

        // ---------- station ----------

        void RefreshStationTab()
        {
            var gm = GM;
            if (gm == null || !gm.Docked) return;
            _stationContent.Clear();
            switch (_stationTab)
            {
                case 0: BuildMarketTab(gm); break;
                case 1: BuildStorageTab(gm); break;
                case 2: BuildRefineTab(gm); break;
                case 3: BuildFittingTab(gm); break;
                case 4: BuildShipsTab(gm); break;
                case 5: BuildIndustryTab(gm); break;
                case 6: BuildRepairTab(gm); break;
                case 7: BuildAgentTab(gm); break;
            }
        }

        void BuildStorageTab(GameManager gm)
        {
            var p = gm.Player;
            var store = gm.Store;
            var st = p.ComputeStats();

            _stationContent.Add(Section("STORAGE BAY — " + gm.HereName().ToUpper()));
            _stationContent.Add(WrapText(
                "Unlimited capacity, but strictly local: anything left here can only be "
                + "collected at this station. Holding "
                + Mathf.Round(store.TotalM3()) + " m3.", UiSkin.TextDim));

            _stationContent.Add(Section("IN YOUR HOLD  ("
                + Mathf.Round(p.CargoUsed()) + " / " + Mathf.Round(st.CargoCap) + " m3)"));
            bool anyHold = false;
            foreach (var id in new List<string>(p.Cargo.Keys))
            {
                float qty = p.Cargo[id];
                if (qty <= 0f) continue;
                anyHold = true;
                string cid = id;
                _stationContent.Add(Row(
                    Cell(GameData.Commodity(id).Name + "  ×" + Mathf.Round(qty) + " m3", 300, UiSkin.TextMain),
                    Btn("Store", () => { gm.DepositCommodity(cid); RefreshStationTab(); })));
            }
            if (!anyHold) _stationContent.Add(WrapText("Hold is empty.", UiSkin.TextDim));
            else _stationContent.Add(Row(Btn("Store Everything",
                () => { gm.DepositAllCargo(); RefreshStationTab(); })));

            _stationContent.Add(Section("STORED HERE"));
            bool anyStored = false;
            foreach (var id in new List<string>(store.Cargo.Keys))
            {
                float qty = store.Cargo[id];
                if (qty <= 0f) continue;
                anyStored = true;
                string cid = id;
                _stationContent.Add(Row(
                    Cell(GameData.Commodity(id).Name + "  ×" + Mathf.Round(qty) + " m3", 300, UiSkin.TextMain),
                    Btn("Load", () => { gm.WithdrawCommodity(cid); RefreshStationTab(); })));
            }
            if (!anyStored) _stationContent.Add(WrapText("No commodities stored here.", UiSkin.TextDim));

            _stationContent.Add(Section("BLUEPRINT VAULT"));
            for (int i = 0; i < p.Blueprints.Count; i++)
            {
                int idx = i;
                _stationContent.Add(Row(
                    Cell(ShipGen.DescribeBlueprint(p.Blueprints[i]), 420, UiSkin.TextMain),
                    Btn("File", () => { gm.DepositBlueprint(idx); RefreshStationTab(); })));
            }
            for (int i = 0; i < store.Blueprints.Count; i++)
            {
                int idx = i;
                _stationContent.Add(Row(
                    Cell(ShipGen.DescribeBlueprint(store.Blueprints[i]), 420, UiSkin.TextDim),
                    Btn("Collect", () => { gm.WithdrawBlueprint(idx); RefreshStationTab(); })));
            }
            if (p.Blueprints.Count == 0 && store.Blueprints.Count == 0)
                _stationContent.Add(WrapText("No blueprints carried or filed.", UiSkin.TextDim));
        }

        void BuildMarketTab(GameManager gm)
        {
            var p = gm.Player;
            int trade = p.SkillLevel("trade");
            _stationContent.Add(Section("SELL ORE & MINERALS"));
            bool any = false;
            foreach (var id in new List<string>(p.Cargo.Keys))
            {
                float qty = p.Cargo[id];
                if (qty <= 0f) continue;
                any = true;
                string cid = id;
                long unit = Market.ApplyTradeSkill(Market.OreSellPrice(gm.StationId, id), trade, true);
                _stationContent.Add(Row(
                    Cell(GameData.Commodity(id).Name + "  ×" + Mathf.Round(qty) + " m3", 250, UiSkin.TextMain),
                    Cell(unit + " cr/m3", 110, UiSkin.TextDim),
                    Cell("= " + GameData.FmtCredits((long)(unit * qty)), 140, UiSkin.AccentWarm),
                    Btn("Sell All", () => { gm.SellCommodity(cid); RefreshStationTab(); })));
            }
            if (!any) _stationContent.Add(WrapText("Cargo hold is empty. Mine some ore!", UiSkin.TextDim));

            _stationContent.Add(Section("MODULES FOR SALE"));
            foreach (var mDef in GameData.Modules.Values)
            {
                string mid = mDef.Id;
                long price = Market.ApplyTradeSkill(Market.ModuleBuyPrice(gm.StationId, mDef.Id), trade, false);
                _stationContent.Add(Row(
                    Cell(mDef.Name + "  [" + mDef.Slot + "]", 250, UiSkin.TextMain),
                    Cell(GameData.FmtCredits(price), 140, UiSkin.AccentWarm),
                    Btn("Buy", () => { gm.BuyModule(mid); RefreshStationTab(); })));
                _stationContent.Add(WrapText("    " + mDef.Desc, UiSkin.TextDim));
            }
        }

        void BuildRefineTab(GameManager gm)
        {
            var p = gm.Player;
            _stationContent.Add(Section("REFINERY"));
            _stationContent.Add(WrapText("Current yield: " + Mathf.RoundToInt(gm.RefineYield() * 100f)
                + "%  (base 66%, +4.5% per Refining level). Ore and recovered scrap both mill down here — "
                + "scrap is compacted hull, so it yields several times its own volume in metal, and Class 3 "
                + "scrap is the only source of beryllium outside low-sec belts.",
                UiSkin.TextDim));
            bool any = false;
            foreach (var id in new List<string>(p.Cargo.Keys))
            {
                if (!GameData.TryRefinable(id, out var def)) continue;
                float qty = p.Cargo[id];
                if (qty <= 0f) continue;
                any = true;
                string oid = id;
                string outputs = "";
                foreach (var kv in def.RefineInto)
                    outputs += (outputs.Length > 0 ? ", " : "")
                        + Mathf.Round(qty * gm.RefineYield() * kv.Value) + " " + GameData.Minerals[kv.Key].Name;
                _stationContent.Add(Row(
                    Cell(def.Name + "  ×" + Mathf.Round(qty) + " m3", 230, UiSkin.TextMain),
                    Cell("→  " + outputs + " (m3)", 350, UiSkin.TextDim, 10),
                    Btn("Refine", () => { gm.RefineOre(oid); RefreshStationTab(); })));
            }
            if (!any) _stationContent.Add(WrapText("No refinable ore in your cargo hold.", UiSkin.TextDim));
        }

        void BuildFittingTab(GameManager gm)
        {
            var p = gm.Player;
            var st = p.ComputeStats();
            _stationContent.Add(Section("FITTED (" + p.Hull.Name + ")"));
            if (p.Hull.Role != null)
                _stationContent.Add(WrapText(p.Hull.Role
                    + (p.Hull.Features != null ? "  ·  " + string.Join("  ·  ", p.Hull.Features) : ""),
                    UiSkin.AccentWarm));
            foreach (var slot in Slots.All)
            {
                if (!p.Fitting.ContainsKey(slot)) continue;
                var arr = p.Fitting[slot];
                string slotName = slot == SlotType.Web ? "Web"
                    : slot == SlotType.Disruptor ? "Disruptor"
                    : slot == SlotType.Claw ? "Claw"
                    : slot == SlotType.Drone ? "Drone"
                    : slot == SlotType.High && p.Hull.TurretOnly ? "Turret" : slot.ToString();
                for (int i = 0; i < arr.Length; i++)
                {
                    var slotC = slot;
                    int idx = i;
                    bool empty = string.IsNullOrEmpty(arr[i]);
                    var row = Row(Cell(slotName + " " + (i + 1) + ":  "
                        + (empty ? "<empty>" : GameData.ResolveModule(arr[i]).Name), 330,
                        empty ? UiSkin.TextDim : UiSkin.TextMain));
                    if (!empty)
                        row.Add(Btn("Unfit", () => { gm.UnfitModule(slotC, idx); RefreshStationTab(); }));
                    _stationContent.Add(row);
                }
            }

            _stationContent.Add(Section("HANGAR"));
            if (gm.Store.Modules.Count == 0)
                _stationContent.Add(WrapText("No spare modules. Buy some on the market.", UiSkin.TextDim));
            for (int i = 0; i < gm.Store.Modules.Count; i++)
            {
                int idx = i;
                _stationContent.Add(Row(
                    Cell(GameData.ResolveModule(gm.Store.Modules[i]).Name + "  [" + GameData.ResolveModule(gm.Store.Modules[i]).Slot + "]",
                        330, UiSkin.TextMain),
                    Btn("Fit", () => { gm.FitModule(idx); RefreshStationTab(); })));
            }

            _stationContent.Add(Section("SHIP STATS"));
            _stationContent.Add(WrapText(
                "Shield " + Mathf.Round(st.MaxShield) + "   Armor " + Mathf.Round(st.MaxArmor)
                + "   Hull " + Mathf.Round(st.MaxHull) + "   Cap " + Mathf.Round(st.MaxCap)
                + " (+" + st.CapRegen.ToString("0.0") + "/s)   Cargo " + Mathf.Round(st.CargoCap)
                + " m3   Speed " + Mathf.Round(st.Speed * GameData.UnitsToMs) + " m/s", UiSkin.TextDim));
        }

        void BuildShipsTab(GameManager gm)
        {
            var p = gm.Player;
            int trade = p.SkillLevel("trade");
            foreach (var s in GameData.Ships.Values)
            {
                bool current = s.Id == p.HullId;
                string sid = s.Id;
                long price = Market.ApplyTradeSkill(Market.ShipBuyPrice(gm.StationId, s.Id), trade, false);
                long tradeIn = Market.ShipTradeInValue(gm.StationId, p.HullId);
                var row = Row(
                    Cell((current ? "▶ " : "") + s.Name + "  (" + s.Class + ")", 270,
                        current ? new Color(0.5f, 1f, 0.65f) : UiSkin.TextMain),
                    Cell(current ? "ACTIVE" : GameData.FmtCredits(price - tradeIn) + " after trade-in", 230,
                        UiSkin.AccentWarm));
                if (!current)
                    row.Add(Btn("Buy & Board", () => { gm.BuyShip(sid); RefreshStationTab(); }));
                _stationContent.Add(row);
                _stationContent.Add(WrapText("    " + s.Desc + "  |  Cargo " + s.Cargo + " m3, "
                    + s.HighSlots + "H/" + s.MidSlots + "M/" + s.LowSlots + "L, "
                    + Mathf.Round(s.Speed * GameData.UnitsToMs) + " m/s", UiSkin.TextDim));
            }
        }

        /// <summary>One module blueprint: what it makes, and at what cost.</summary>
        void BuildModulePrintCard(GameManager gm, Blueprint bp)
        {
            var baseDef = GameData.ResolveModule(bp.ModuleId);
            int mods = GameData.ModBpMods[bp.Rarity];
            _stationContent.Add(Section(baseDef.Name + "  —  " + GameData.RarityNames[bp.Rarity]
                + "  ·  " + bp.RunsLeft + " run" + (bp.RunsLeft == 1 ? "" : "s") + " left"));
            _stationContent.Add(WrapText("[" + baseDef.Slot + " slot]  "
                + (mods == 0
                    ? "Factory-standard: base stats, no modifiers."
                    : "Each run rolls " + mods + " modifier" + (mods == 1 ? "" : "s") + " onto the finished piece.")
                + "  " + GameData.ModBpMatMult[bp.Rarity] + "x material cost.", UiSkin.AccentWarm));
            _stationContent.Add(WrapText(baseDef.Desc, UiSkin.TextDim));
            string cost = "";
            foreach (var kv in ModGen.MaterialCost(bp))
                cost += (cost.Length > 0 ? ", " : "") + kv.Value + " " + GameData.Minerals[kv.Key].Name;
            _stationContent.Add(WrapText("Cost per run: " + cost + " (m3) + "
                + GameData.FmtCredits(ModGen.Fee(bp)) + " fee.", UiSkin.TextDim));
            string blocker = gm.ManufactureBlocker(bp);
            var row = Row();
            if (blocker == null)
            {
                var build = Btn("Manufacture", () => { gm.Manufacture(bp); RefreshStationTab(); });
                build.style.color = new Color(0.5f, 1f, 0.65f);
                row.Add(build);
            }
            else row.Add(Cell(blocker, 460, UiSkin.TextDim));
            _stationContent.Add(row);
        }

        void BuildIndustryTab(GameManager gm)
        {
            var p = gm.Player;
            _stationContent.Add(Section("MANUFACTURING"));
            _stationContent.Add(WrapText("Each run consumes refined minerals from your cargo hold "
                + "plus an assembly fee (shown per blueprint); your current hull is traded in. "
                + "Refine ore on the Refine tab to source minerals.", UiSkin.TextDim));

            if (p.Blueprints.Count == 0)
            {
                _stationContent.Add(WrapText("No blueprints. Pirate wrecks sometimes carry blueprint "
                    + "chips — convoy haulers are the best source.", UiSkin.TextDim));
                return;
            }

            foreach (var bp in new List<Blueprint>(p.Blueprints))
            {
                var b = bp;
                if (bp.IsModule) { BuildModulePrintCard(gm, b); continue; }
                var def = ShipGen.Def(bp);
                _stationContent.Add(Section(def.Name + "  —  " + GameData.RarityNames[bp.Rarity]
                    + "  ·  " + bp.RunsLeft + " run" + (bp.RunsLeft == 1 ? "" : "s") + " left  ·  body #" + bp.Hash));
                _stationContent.Add(WrapText(def.Class + "  ·  " + def.Role, UiSkin.TextDim));
                _stationContent.Add(WrapText(
                    "Turrets " + def.HighSlots + " · Webs " + def.WebSlots
                    + (def.DisruptorSlots > 0 ? " · Disr " + def.DisruptorSlots : "")
                    + (def.ClawSlots > 0 ? " · Claws " + def.ClawSlots : "")
                    + (def.DroneSlots > 0 ? " · Drones " + def.DroneSlots : "")
                    + (def.SensorSlots > 0 ? " · Sensors " + def.SensorSlots : "")
                    + (def.CollectorSlots > 0 ? " · Collectors " + def.CollectorSlots : "") + " · Lows " + def.LowSlots
                    + " · " + Mathf.Round(def.Speed * GameData.UnitsToMs) + " m/s · Shield "
                    + def.Shield + " · Cargo " + def.Cargo + " m3", UiSkin.TextDim));
                if (def.Features != null)
                    _stationContent.Add(WrapText(string.Join("  ·  ", def.Features), UiSkin.AccentWarm));
                string bpCost = "";
                foreach (var kv in ShipGen.MaterialCost(bp))
                    bpCost += (bpCost.Length > 0 ? ", " : "") + kv.Value + " " + GameData.Minerals[kv.Key].Name;
                _stationContent.Add(WrapText("Cost per run: " + bpCost + " (m3) + "
                    + GameData.FmtCredits(ShipGen.Fee(bp)) + " fee.", UiSkin.TextDim));
                string blocker = gm.ManufactureBlocker(bp);
                var row = Row();
                if (blocker == null)
                {
                    var build = Btn("Manufacture", () => { gm.Manufacture(b); RefreshStationTab(); });
                    build.style.color = new Color(0.5f, 1f, 0.65f);
                    row.Add(build);
                }
                else
                {
                    row.Add(Cell(blocker, 520, new Color(1f, 0.6f, 0.5f), 10));
                }
                _stationContent.Add(row);
            }
        }

        void BuildRepairTab(GameManager gm)
        {
            long cost = gm.RepairCost();
            _stationContent.Add(Section("REPAIR BAY"));
            _stationContent.Add(WrapText(cost <= 0
                ? "Your ship is in perfect condition."
                : "Full armor and hull repair: " + GameData.FmtCredits(cost), UiSkin.TextMain));
            if (cost > 0)
                _stationContent.Add(Row(Btn("Repair", () => { gm.Repair(); RefreshStationTab(); })));
        }

        void BuildAgentTab(GameManager gm)
        {
            var m = gm.ActiveMission;
            if (m != null)
            {
                _stationContent.Add(Section("ACTIVE MISSION"));
                _stationContent.Add(Text(m.Title, 12, UiSkin.TextMain, true));
                _stationContent.Add(WrapText(m.Desc, UiSkin.TextDim));
                _stationContent.Add(WrapText("Progress: " + Missions.ProgressText(gm, m), UiSkin.TextDim));
                _stationContent.Add(Text("Reward: " + GameData.FmtCredits(m.Reward), 11, UiSkin.AccentWarm));
                var row = Row();
                if (gm.CanTurnInMission())
                {
                    var done = Btn("Complete Mission", () => { gm.TurnInMission(); RefreshStationTab(); });
                    done.style.color = new Color(0.5f, 1f, 0.65f);
                    row.Add(done);
                }
                row.Add(Btn("Abandon", () => { gm.AbandonMission(); RefreshStationTab(); }));
                _stationContent.Add(row);
                return;
            }

            _stationContent.Add(Section("AVAILABLE CONTRACTS"));
            _stationContent.Add(WrapText(
                "One active mission at a time. New offers appear after each accept or turn-in.", UiSkin.TextDim));
            foreach (var offer in gm.StationOffers())
            {
                var o = offer;
                _stationContent.Add(Row(
                    Cell(o.Title, 340, UiSkin.TextMain),
                    Cell(GameData.FmtCredits(o.Reward), 130, UiSkin.AccentWarm),
                    Btn("Accept", () => { gm.AcceptMission(o); RefreshStationTab(); })));
                _stationContent.Add(WrapText("    " + o.Desc, UiSkin.TextDim));
            }
        }
    }
}
