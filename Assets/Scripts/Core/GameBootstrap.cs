using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Zero-setup entry point: builds the whole game at runtime from any scene
    /// (empty or not). Add nothing to your scene — just press Play.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameManager.I != null) return;

            // Anti-aliasing. Nothing else sets this, and without it every hull
            // edge in the game is a staircase. Under URP the pipeline asset owns
            // MSAA instead, so this is a no-op there and the camera flag below
            // does the work.
            QualitySettings.antiAliasing = 4;

            // Lighting. Three-point: a key sun, a cool fill from the opposite
            // side so unlit faces are readable rather than black, and a dim rim
            // from behind to separate hulls from the starfield.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.20f, 0.24f, 0.34f);
            RenderSettings.ambientEquatorColor = new Color(0.13f, 0.15f, 0.21f);
            RenderSettings.ambientGroundColor = new Color(0.06f, 0.06f, 0.09f);
            RenderSettings.fog = false;
            AddLight("Sunlight", new Color(1f, 0.96f, 0.88f), 1.15f, 35f, 140f);
            AddLight("Fill", new Color(0.42f, 0.55f, 0.80f), 0.38f, 12f, -40f);
            AddLight("Rim", new Color(0.65f, 0.72f, 0.95f), 0.28f, -20f, -150f);

            // Give the metal something to reflect. Nearly every material in the
            // game is metallic, and a metallic surface with no environment
            // resolves to flat dark colour — which is most of why the hulls read
            // as blocks of paint. There is no skybox here (the camera clears to
            // solid colour and the stars are geometry), so the reflection probe
            // has nothing to capture; a tiny procedural cubemap stands in.
            RenderSettings.defaultReflectionMode =
                UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = SpaceCubemap();
            RenderSettings.reflectionIntensity = 1f;

            // Camera: adopt the scene's main camera or create one.
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.012f, 0.02f, 0.045f);
            // The starfield sits at 450k-900k units, so a million is enough far
            // plane. Halving it halves the depth range the buffer has to cover.
            cam.farClipPlane = 1000000f;
            cam.nearClipPlane = 2f;
            cam.allowMSAA = true;
            cam.allowHDR = true;
            if (cam.GetComponent<CameraRig>() == null) cam.gameObject.AddComponent<CameraRig>();
            // Bloom, so emissive surfaces read as light rather than bright paint.
            // Built-in pipeline only; it disables itself if the shader is missing.
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null
                && cam.GetComponent<BloomEffect>() == null)
                cam.gameObject.AddComponent<BloomEffect>();

            // Background starfield (pinned to the camera — a cheap skybox).
            ShipVisuals.BuildStarfield();

            // Player ship: an empty root; the per-hull visual is built by
            // ShipController.RebuildVisual once the player state is loaded.
            var shipGo = new GameObject("PlayerShip");
            var ship = shipGo.AddComponent<ShipController>();

            // Game root.
            var gameGo = new GameObject("Game");
            Object.DontDestroyOnLoad(gameGo);
            var view = gameGo.AddComponent<SystemView>();
            var gm = gameGo.AddComponent<GameManager>();
            gm.Init(view, ship);
            gm.Log(RenderReport());

            // UI: UI Toolkit HUD is primary; IMGUI is the automatic fallback
            // and stays one F10 away (UiSwitcher). No uGUI EventSystem needed:
            // UI Toolkit runtime panels use their own built-in input when no
            // EventSystem is present (and the uGUI package may not exist in
            // this code-only project at all).
            gameGo.AddComponent<UiSwitcher>();
            gameGo.AddComponent<AudioDirector>();
            if (!UitHud.TryCreate()) gameGo.AddComponent<HudUI>();
        }

        /// <summary>Names the active render pipeline and the shader the world
        /// actually resolved, in the message log. Which pipeline a project runs
        /// is not in version control — ProjectSettings only carries the editor
        /// version — so the running game is the only reliable place to ask.
        /// It also decides what post-processing is even possible.</summary>
        static string RenderReport()
        {
            var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            string pipe = rp == null ? "Built-in Render Pipeline" : rp.GetType().Name;
            string shader = SystemView.ShaderName();
            var cam = Camera.main;
            bool bloom = cam != null && cam.GetComponent<BloomEffect>() != null
                && cam.GetComponent<BloomEffect>().enabled;
            return "Renderer: " + pipe + "  ·  shader \"" + shader + "\"  ·  MSAA "
                + QualitySettings.antiAliasing + "x  ·  bloom " + (bloom ? "on" : "off");
        }

        static void AddLight(string name, Color c, float intensity, float pitch, float yaw)
        {
            var go = new GameObject(name);
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = c;
            l.intensity = intensity;
            l.shadows = LightShadows.None;   // nothing here casts useful shadows
            go.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        /// <summary>A 16px cubemap of deep space: near-black below, a faint cold
        /// glow above and a warm hint toward the star. Costs nothing and gives
        /// every metallic surface a gradient to catch instead of flat black.</summary>
        static Cubemap SpaceCubemap()
        {
            const int n = 16;
            var cm = new Cubemap(n, TextureFormat.RGBA32, false);
            var faces = new[]
            {
                CubemapFace.PositiveX, CubemapFace.NegativeX, CubemapFace.PositiveY,
                CubemapFace.NegativeY, CubemapFace.PositiveZ, CubemapFace.NegativeZ,
            };
            foreach (var face in faces)
            {
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float v = y / (float)(n - 1);
                        var c = face == CubemapFace.PositiveY
                            ? new Color(0.16f, 0.20f, 0.30f)
                            : face == CubemapFace.NegativeY
                                ? new Color(0.02f, 0.02f, 0.04f)
                                : Color.Lerp(new Color(0.03f, 0.04f, 0.07f),
                                             new Color(0.13f, 0.16f, 0.24f), v);
                        // A warm smear on one face, standing in for the star.
                        if (face == CubemapFace.PositiveX)
                            c = Color.Lerp(c, new Color(0.45f, 0.38f, 0.28f), 0.35f);
                        px[y * n + x] = c;
                    }
                cm.SetPixels(px, face);
            }
            cm.Apply();
            return cm;
        }
    }
}
