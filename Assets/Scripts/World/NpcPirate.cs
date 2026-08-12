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
        public Vector3 Vel => _vel;

        Vector3 _vel;
        float _cycleT;
        float _webT; // stasis-webbed while > 0: half speed
        float _disruptT; // warp-disrupted while > 0: cannot warp out
        bool _jamLogged;
        float _lockT;      // pirates need a moment to lock you too
        float _fireFlash;  // seconds the fire beam stays visible
        bool _fleeing;
        float _fleeT;
        float _idleT;      // time spent with no target — bored pirates roam belts
        LineRenderer _beam;

        const float Inertia = 1.4f;    // seconds to converge on desired velocity
        const float LockDelay = 1.5f;  // seconds before a pirate can open fire
        const float FleeWarpTime = 5f; // seconds of running before the warp-out

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

        /// <summary>Stasis web: halves velocity while the effect holds.</summary>
        public void ApplyWeb(float duration) => _webT = Mathf.Max(_webT, duration);
        public bool Webbed => _webT > 0f;

        /// <summary>Warp disruptor: the target aligns out but can never jump.</summary>
        public void ApplyDisrupt(float duration) => _disruptT = Mathf.Max(_disruptT, duration);
        public bool Disrupted => _disruptT > 0f;

        float EffSpeed => Def.Speed * (_webT > 0f ? 0.5f : 1f);

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.Ready) return;
            float dt = Time.deltaTime;
            if (_webT > 0f) _webT -= dt;
            if (_disruptT > 0f) _disruptT -= dt;

            Vector3 desired = Vector3.zero;
            bool playerVulnerable = !gm.Docked && !gm.Ship.InWarp;
            float d = Vector3.Distance(transform.position, gm.Ship.transform.position);

            // Badly damaged pirates cut and run (overlords never do).
            if (!_fleeing && !Def.NeverFlees && Hull < Def.Hull * 0.3f)
            {
                _fleeing = true;
                if (d < Def.Engage) gm.Log(Def.Name + " is breaking off and aligning out!");
            }
            if (_fleeing)
            {
                Vector3 away = transform.position - gm.Ship.transform.position;
                desired = (away.sqrMagnitude > 1f ? away.normalized : transform.forward)
                    * EffSpeed * 1.25f;
                _fleeT += dt;
                float k2 = Mathf.Min(1f, dt / Inertia);
                _vel += (desired - _vel) * k2;
                transform.position += _vel * dt;
                if (_vel.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.LookRotation(_vel.normalized, Vector3.up);
                if (_beam.enabled) _beam.enabled = false;
                if (_fleeT >= FleeWarpTime)
                {
                    if (Disrupted)
                    {
                        // Held at the brink of warp — drop the point and it jumps.
                        _fleeT = FleeWarpTime;
                        if (!_jamLogged)
                        {
                            _jamLogged = true;
                            gm.Log(Def.Name + "'s warp drive is jammed — it cannot escape!");
                        }
                    }
                    else
                    {
                        gm.NpcFled(this);
                    }
                }
                return;
            }

            if (playerVulnerable && d < Def.Engage)
            {
                _idleT = 0f;
                Vector3 toPlayer = gm.Ship.transform.position - transform.position;
                if (d > Def.Orbit * 1.25f)
                {
                    desired = toPlayer.normalized * EffSpeed;
                }
                else
                {
                    // Orbit: fly tangent to the player with radial correction.
                    Vector3 radial = -toPlayer.normalized;
                    Vector3 tangent = Vector3.Cross(radial, Vector3.up).normalized;
                    float radialErr = Mathf.Clamp((d - Def.Orbit) / Def.Orbit, -0.6f, 0.6f);
                    desired = (tangent + radial * -radialErr).normalized * EffSpeed;
                }

                _lockT += dt;
                _cycleT += dt;
                if (d <= Def.Range && _lockT >= LockDelay && _cycleT >= Def.Cycle)
                {
                    _cycleT = 0f;
                    _fireFlash = 0.28f;
                    float angVel = Combat.AngularVelocity(
                        gm.Ship.transform.position - transform.position, gm.Ship.Vel - _vel);
                    float dmg = Combat.RollDamage(Def.Dmg, Def.Tracking, angVel, out _);
                    gm.DamagePlayer(dmg, this);
                }
            }
            else
            {
                _cycleT = 0f;
                _lockT = 0f;

                // Bored pirates occasionally relocate to another belt.
                _idleT += dt;
                if (_idleT > 25f && Random.value < dt / 20f)
                {
                    _idleT = 0f;
                    var belts = gm.System.Celestials.FindAll(c => c.Kind == ObjKind.Belt);
                    if (belts.Count > 0)
                    {
                        var belt = belts[Random.Range(0, belts.Count)];
                        var off = Random.insideUnitSphere * 400f;
                        off.y *= 0.2f;
                        transform.position = belt.Pos + off;
                        _vel = Vector3.zero;
                    }
                }
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
