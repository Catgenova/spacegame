using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// A launched attack drone: a small bird-dart that flies out from the
    /// player ship, orbits its target, and flashes a beam when the owning
    /// controller module lands a damage cycle. Clear the target (module off,
    /// lock lost, target dead) and it flies home and despawns.
    /// </summary>
    public class DroneUnit : MonoBehaviour
    {
        public NpcPirate Target;

        Vector3 _vel;
        float _orbitAng;
        float _flash;
        LineRenderer _beam;

        const float Speed = 7.5f;
        const float OrbitRange = 22f;

        public static DroneUnit Launch(Vector3 from)
        {
            var go = new GameObject("Drone");
            go.transform.position = from + Random.onUnitSphere * 2f;
            var d = go.AddComponent<DroneUnit>();
            d.BuildVisual();
            return d;
        }

        void BuildVisual()
        {
            var white = new Color(0.88f, 0.90f, 0.92f);
            var dark = new Color(0.16f, 0.30f, 0.26f);
            Part(PrimitiveType.Cube, Vector3.zero, new Vector3(0.35f, 0.16f, 0.85f), Vector3.zero, white, false);
            Part(PrimitiveType.Cube, new Vector3(-0.42f, 0f, -0.10f), new Vector3(0.75f, 0.05f, 0.35f), new Vector3(0f, 18f, 0f), dark, false);
            Part(PrimitiveType.Cube, new Vector3(0.42f, 0f, -0.10f), new Vector3(0.75f, 0.05f, 0.35f), new Vector3(0f, -18f, 0f), dark, false);
            Part(PrimitiveType.Cube, new Vector3(0f, 0f, -0.52f), new Vector3(0.16f, 0.12f, 0.16f), Vector3.zero, new Color(0.35f, 0.95f, 0.85f), true);

            var beamGo = new GameObject("Beam");
            beamGo.transform.SetParent(transform, false);
            _beam = beamGo.AddComponent<LineRenderer>();
            _beam.positionCount = 2;
            _beam.startWidth = 0.25f;
            _beam.endWidth = 0.12f;
            _beam.material = SystemView.Mat(new Color(0.35f, 0.95f, 0.85f), true);
            _beam.enabled = false;
        }

        void Part(PrimitiveType t, Vector3 pos, Vector3 scale, Vector3 euler, Color c, bool emissive)
        {
            var p = GameObject.CreatePrimitive(t);
            Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(transform, false);
            p.transform.localPosition = pos;
            p.transform.localScale = scale;
            p.transform.localRotation = Quaternion.Euler(euler.x, euler.y, euler.z);
            p.GetComponent<Renderer>().material = SystemView.Mat(c, emissive);
        }

        /// <summary>Called by the controller when a damage cycle lands.</summary>
        public void Fire()
        {
            _flash = 0.25f;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.Ready) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;

            Vector3 want;
            if (Target != null)
            {
                _orbitAng += 1.6f * dt;
                want = Target.transform.position + new Vector3(
                    Mathf.Cos(_orbitAng) * OrbitRange,
                    Mathf.Sin(_orbitAng * 0.7f) * 5f,
                    Mathf.Sin(_orbitAng) * OrbitRange);
            }
            else
            {
                // fly home and despawn
                want = gm.Ship.transform.position;
                if (Vector3.Distance(transform.position, want) < 6f)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            var desired = (want - transform.position);
            float d = desired.magnitude;
            desired = d > 0.01f ? desired.normalized * Mathf.Min(Speed, d * 2f) : Vector3.zero;
            _vel += (desired - _vel) * Mathf.Min(1f, dt / 0.5f);
            transform.position += _vel * dt;
            if (_vel.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(_vel.normalized, Vector3.up);

            if (_flash > 0f)
            {
                _flash -= dt;
                bool show = _flash > 0f && Target != null;
                _beam.enabled = show;
                if (show)
                {
                    _beam.SetPosition(0, transform.position);
                    _beam.SetPosition(1, Target.transform.position);
                }
            }
            else if (_beam.enabled)
            {
                _beam.enabled = false;
            }
        }
    }
}
