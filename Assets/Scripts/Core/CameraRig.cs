using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// EVE-style orbit camera: follows the player ship, right-drag to orbit,
    /// scroll wheel to zoom.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public float Distance = 60f;
        public float Yaw = 20f;
        public float Pitch = 25f;

        const float MinDist = 10f;
        const float MaxDist = 4000f;

        void LateUpdate()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.Ready || gm.Ship == null) return;

            if (Input.GetMouseButton(1))
            {
                Yaw += Input.GetAxis("Mouse X") * 3.5f;
                Pitch = Mathf.Clamp(Pitch - Input.GetAxis("Mouse Y") * 3.5f, -80f, 80f);
            }
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f && !HudUI.MouseOverUI)
                Distance = Mathf.Clamp(Distance * (1f - scroll * 1.2f), MinDist, MaxDist);

            var target = gm.Ship.transform.position;
            var rot = Quaternion.Euler(Pitch, Yaw, 0f);
            transform.position = target - rot * Vector3.forward * Distance;
            transform.rotation = rot;
        }
    }
}
