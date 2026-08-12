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

        const float Inertia = 1.4f; // seconds to converge on desired velocity

        public void Init(NpcDef def)
        {
            Def = def;
            Shield = def.Shield;
            Armor = def.Armor;
            Hull = def.Hull;
            Kind = ObjKind.Npc;
            DisplayName = def.Name;
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

                _cycleT += dt;
                if (d <= Def.Range && _cycleT >= Def.Cycle)
                {
                    _cycleT = 0f;
                    gm.DamagePlayer(Def.Dmg, this);
                }
            }
            else
            {
                _cycleT = 0f;
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
