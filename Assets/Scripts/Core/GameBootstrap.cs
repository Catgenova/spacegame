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

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.20f);
            RenderSettings.fog = false;

            // Sunlight.
            var lightGo = new GameObject("Sunlight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.96f, 0.88f);
            lightGo.transform.rotation = Quaternion.Euler(35f, 140f, 0f);

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
            cam.farClipPlane = 2000000f;
            cam.nearClipPlane = 1f;
            if (cam.GetComponent<CameraRig>() == null) cam.gameObject.AddComponent<CameraRig>();

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
            gameGo.AddComponent<HudUI>();
            gm.Init(view, ship);
        }
    }
}
