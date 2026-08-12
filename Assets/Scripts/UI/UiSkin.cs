using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// A themed IMGUI skin built at runtime: dark sci-fi panels, accent
    /// buttons with hover/active states, and consistent typography — no
    /// texture assets, everything generated in code.
    /// </summary>
    public static class UiSkin
    {
        public static readonly Color Accent = new Color(0.35f, 0.7f, 1f);
        public static readonly Color AccentWarm = new Color(1f, 0.82f, 0.42f);
        public static readonly Color TextMain = new Color(0.8f, 0.87f, 0.94f);
        public static readonly Color TextDim = new Color(0.5f, 0.58f, 0.68f);
        public static readonly Color PanelBg = new Color(0.035f, 0.06f, 0.1f, 0.94f);
        public static readonly Color PanelBorder = new Color(0.16f, 0.28f, 0.44f, 0.95f);

        static GUISkin _skin;
        static Texture2D _white;

        public static Texture2D White
        {
            get
            {
                if (_white == null) _white = Solid(Color.white);
                return _white;
            }
        }

        public static GUISkin Skin
        {
            get
            {
                if (_skin == null) Build();
                return _skin;
            }
        }

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(2, 2);
            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                    t.SetPixel(x, y, c);
            t.Apply();
            return t;
        }

        static void Build()
        {
            _skin = Object.Instantiate(GUI.skin);

            var btnBg = Solid(new Color(0.08f, 0.14f, 0.23f));
            var btnHover = Solid(new Color(0.12f, 0.22f, 0.35f));
            var btnActive = Solid(new Color(0.16f, 0.32f, 0.5f));

            _skin.button.normal.background = btnBg;
            _skin.button.normal.textColor = TextMain;
            _skin.button.hover.background = btnHover;
            _skin.button.hover.textColor = Color.white;
            _skin.button.active.background = btnActive;
            _skin.button.active.textColor = Color.white;
            _skin.button.fontSize = 11;

            _skin.label.normal.textColor = TextMain;

            _skin.box.normal.background = Solid(PanelBg);
            _skin.box.normal.textColor = TextDim;
        }

        /// <summary>Bordered panel: 1px border, dark translucent body, accent top edge.</summary>
        public static void Panel(Rect r)
        {
            GUI.color = PanelBorder;
            GUI.DrawTexture(r, White);
            GUI.color = PanelBg;
            GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), White);
            GUI.color = new Color(Accent.r, Accent.g, Accent.b, 0.5f);
            GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, r.width - 2, 2), White);
            GUI.color = Color.white;
        }
    }
}
