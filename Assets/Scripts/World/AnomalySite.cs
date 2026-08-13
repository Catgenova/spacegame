using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// One cracked-open container inside an anomaly. Holds exotic metal and
    /// sometimes a blueprint chip. Looting is per-container, so a site is a
    /// route to fly, not a single button.
    /// </summary>
    public class SiteContainer : SpaceObject
    {
        public AnomalySite Site;
        public readonly Dictionary<string, float> Exotics = new Dictionary<string, float>();
        public readonly List<Blueprint> BpLoot = new List<Blueprint>();

        public float TotalM3()
        {
            float sum = 0f;
            foreach (var v in Exotics.Values) sum += v;
            return sum;
        }

        public bool Empty => TotalM3() <= 0.01f && BpLoot.Count == 0;
    }

    /// <summary>
    /// A deep-space anomaly turned up by a Trail hull's sensor sweep: the only
    /// source of exotic metals in the game.
    ///
    /// The whole point is time pressure. A site is structurally unstable and
    /// counts down to a detonation that destroys everything left inside and
    /// shockwaves anyone still in the blast. Partway through that countdown the
    /// noise draws a response fleet, which warps in on top of you. Containers are
    /// spread out, so clearing a site means covering ground fast — which is
    /// exactly what a Trail hull with a good collector and an afterburner does.
    ///
    /// Sites are transient and deliberately unsaved: leave one and it is gone.
    /// </summary>
    public class AnomalySite : SpaceObject
    {
        public int Tier;                  // 1..3 — richer and deadlier
        public float Life;                // seconds until detonation
        public float TotalLife;
        public float RatTimer;            // seconds until the response fleet lands
        public bool RatsCalled;
        public string[] RatWave;          // npc ids that answer
        public readonly List<SiteContainer> Containers = new List<SiteContainer>();

        public const float BlastRange = 260f;   // 26 km
        public const float BlastDamage = 340f;

        public float LifeFrac => TotalLife > 0f ? Mathf.Clamp01(Life / TotalLife) : 0f;

        public int ContainersLeft
        {
            get
            {
                int n = 0;
                foreach (var c in Containers) if (c != null && !c.Empty) n++;
                return n;
            }
        }

        public float ExoticM3Left()
        {
            float sum = 0f;
            foreach (var c in Containers) if (c != null) sum += c.TotalM3();
            return sum;
        }

        bool _warnedFleet, _warnedHalf, _warnedFinal;

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            float dt = Time.deltaTime;

            // The response fleet answers the noise partway through.
            if (!RatsCalled)
            {
                RatTimer -= dt;
                if (RatTimer <= 6f && !_warnedFleet)
                {
                    _warnedFleet = true;
                    gm.Log("WARNING: hostile signatures aligning on the anomaly — six seconds.");
                }
                if (RatTimer <= 0f)
                {
                    RatsCalled = true;
                    CallTheFleet(gm);
                }
            }

            Life -= dt;
            if (Life <= 30f && !_warnedFinal)
            {
                _warnedFinal = true;
                gm.Log("ANOMALY CRITICAL: structural collapse in 30 seconds. Get clear.");
            }
            else if (Life <= TotalLife * 0.5f && !_warnedHalf)
            {
                _warnedHalf = true;
                gm.Log("Anomaly destabilising — roughly half its window left.");
            }
            if (Life > 0f) return;

            Detonate(gm);
        }

        void CallTheFleet(GameManager gm)
        {
            if (RatWave == null || RatWave.Length == 0) return;
            gm.Log("A response fleet warps in on the anomaly — " + RatWave.Length + " hostiles!");
            for (int i = 0; i < RatWave.Length; i++)
            {
                var dir = Random.onUnitSphere;
                dir.y *= 0.15f;
                dir = dir.normalized;
                var pos = transform.position + dir * Random.Range(90f, 190f);
                gm.View.SpawnNpc(RatWave[i], pos);
            }
            Sfx.WarpExit();
        }

        void Detonate(GameManager gm)
        {
            Sfx.Explosion(2.4f);
            float lost = ExoticM3Left();
            gm.Log(lost > 0.5f
                ? "The anomaly collapses — " + Mathf.Round(lost) + " m3 of exotics lost with it."
                : "The anomaly collapses, stripped clean. Good flying.");

            // Anything still inside the blast takes a serious hit.
            if (gm.Ship != null)
            {
                float d = Vector3.Distance(gm.Ship.transform.position, transform.position);
                if (d <= BlastRange)
                {
                    float falloff = 1f - d / BlastRange;
                    gm.Log("Caught in the shockwave!");
                    gm.DamagePlayer(BlastDamage * falloff, null);
                }
            }
            foreach (var c in new List<SiteContainer>(Containers))
                if (c != null) gm.View.RemoveObject(c);
            Containers.Clear();
            if (gm.Selected == this) gm.Select(null);
            gm.View.RemoveObject(this);
        }

        /// <summary>Called when a container is emptied, to close the site out
        /// early once there is nothing left worth staying for.</summary>
        public void NoteContainerEmptied(GameManager gm)
        {
            if (ContainersLeft > 0) return;
            gm.Log("Anomaly stripped clean — every container cracked. It will still blow; clear the area.");
        }
    }
}
