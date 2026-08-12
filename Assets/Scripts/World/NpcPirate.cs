using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Pirate AI: idle near its belt until the player comes within engage
    /// range, then approach, orbit, and shoot. EVE-style inertial movement.
    /// </summary>
    public class NpcPirate : SpaceObject
    {
        public NpcDef Def;
        public float Shield, Armor, Hull;

        Vector3 _vel;
        float _cycleT;
        float _lockT;      // pirates need a moment to lock you too
        float _fireFlash;  // seconds the fire beam stays visible
        LineRenderer _beam;

        const float Inertia = 1.4f;   // seconds to converge on desired velocity
        const float LockDelay = 1.5f; // seconds before a pirate can open fire

        public void Init(NpcDef def)
        {
            Def = def;
            Shield = def.Shield;
            Armor = def.Armor;
            Hull = def.Hull;
            Kind = ObjKind.Npc;
            DisplayName = def.Name;

            var beamGo = new GameObject("Beam");
            beamGo.transform.SetParent(transform, false);
            _beam = beamGo.AddComponent<LineRenderer>();
            _beam.positionCount = 2;
            _beam.startWidth = 0.6f;
            _beam.endWidth = 0.3f;
            _beam.material = SystemView.Mat(new Color(1f, 0.35f, 0.25f), true);
            _beam.enabled = false;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.Ready) return;
            float dt = Time.deltaTime;

            Vector3 desired = Vector3.zero;
            bool playerVulnerable = !gm.Docked && !gm.Ship.InWarp;
            float d = Vector3.Distance(transform.position, gm.Ship.transform.position);

            if (playerVulnerable && d < Def.Engage)
            {
                Vector3 toPlayer = gm.Ship.transform.position - transform.position;
                if (d > Def.Orbit * 1.25f)
                {
                    desired = toPlayer.normalized * Def.Speed;
                }
                else
                {
                    // Orbit: fly tangent to the player with radial correction.
                    Vector3 radial = -toPlayer.normalized;
                    Vector3 tangent = Vector3.Cross(radial, Vector3.up).normalized;
                    float radialErr = Mathf.Clamp((d - Def.Orbit) / Def.Orbit, -0.6f, 0.6f);
                    desired = (tangent + radial * -radialErr).normalized * Def.Speed;
                }

                _lockT += dt;
                _cycleT += dt;
                if (d <= Def.Range && _lockT >= LockDelay && _cycleT >= Def.Cycle)
                {
                    _cycleT = 0f;
                    _fireFlash = 0.28f;
                    gm.DamagePlayer(Def.Dmg, this);
                }
            }
            else
            {
                _cycleT = 0f;
                _lockT = 0f;
            }

            if (_fireFlash > 0f)
            {
                _fireFlash -= dt;
                _beam.enabled = _fireFlash > 0f;
                if (_beam.enabled)
                {
                    _beam.SetPosition(0, transform.position);
                    _beam.SetPosition(1, gm.Ship.transform.position);
                }
            }
            else if (_beam.enabled)
            {
                _beam.enabled = false;
            }

            float k = Mathf.Min(1f, dt / Inertia);
            _vel += (desired - _vel) * k;
            transform.position += _vel * dt;
            if (_vel.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(_vel.normalized, Vector3.up);
        }

        /// <summary>Apply damage shield -> armor -> hull. Returns true if destroyed.</summary>
        public bool TakeDamage(float dmg)
        {
            float d = dmg;
            if (Shield > 0f) { float a = Mathf.Min(Shield, d); Shield -= a; d -= a; }
            if (d > 0f && Armor > 0f) { float a = Mathf.Min(Armor, d); Armor -= a; d -= a; }
            if (d > 0f) Hull -= d;
            return Hull <= 0f;
        }
    }
}
