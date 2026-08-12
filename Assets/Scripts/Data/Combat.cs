using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Turret tracking math, EVE-style: a gun can only follow a target moving
    /// across its field of view up to its tracking speed. Orbiting fast and
    /// close makes big slow guns miss — a small ship can safely brawl a
    /// battleship-grade turret if it keeps its transversal up.
    /// </summary>
    public static class Combat
    {
        /// <summary>Angular velocity (rad/s) of a target as seen by the shooter.</summary>
        public static float AngularVelocity(Vector3 relPos, Vector3 relVel)
        {
            float d2 = relPos.sqrMagnitude;
            if (d2 < 1f) return 10f;
            return Vector3.Cross(relPos, relVel).magnitude / d2;
        }

        /// <summary>Chance of a solid hit, 0.1 .. 1.</summary>
        public static float HitChance(float tracking, float angularVel)
            => Mathf.Clamp01(0.1f + 0.9f * (tracking / (tracking + angularVel)));

        /// <summary>Roll a cycle's damage: full on a hit, 20% on a glancing blow.</summary>
        public static float RollDamage(float dmg, float tracking, float angularVel, out bool hit)
        {
            hit = Random.value < HitChance(tracking, angularVel);
            return hit ? dmg : dmg * 0.2f;
        }
    }
}
