using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// F10 toggles between the UI Toolkit HUD (primary) and the legacy IMGUI
    /// HUD — insurance while the UI Toolkit runtime setup is young: if the
    /// new UI ever renders wrong, one keypress brings the old one back.
    /// </summary>
    public class UiSwitcher : MonoBehaviour
    {
        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.F10)) return;
            var gm = GameManager.I;
            if (gm == null || !gm.Ready) return;

            var legacy = GetComponent<HudUI>();
            if (UitHud.Instance != null)
            {
                UitHud.DestroyInstance();
                if (legacy == null) gameObject.AddComponent<HudUI>();
                gm.Log("Switched to legacy IMGUI HUD (F10 to switch back).");
            }
            else
            {
                if (legacy != null) Destroy(legacy);
                if (UitHud.TryCreate())
                {
                    gm.Log("Switched to UI Toolkit HUD.");
                }
                else
                {
                    if (legacy == null) gameObject.AddComponent<HudUI>();
                    gm.Log("UI Toolkit HUD unavailable on this setup — staying on IMGUI.");
                }
            }
        }
    }
}
