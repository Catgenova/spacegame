using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// The remains of a destroyed pirate. May hold salvageable modules;
    /// evaporates after a few minutes either way.
    /// </summary>
    public class Wreck : SpaceObject
    {
        public readonly List<string> Loot = new List<string>();
        public readonly List<Blueprint> BpLoot = new List<Blueprint>();
        /// <summary>Graded scrap in the hulk: commodity id -> m3 remaining.</summary>
        public readonly Dictionary<string, float> ScrapLoot = new Dictionary<string, float>();

        public float ScrapM3()
        {
            float sum = 0f;
            foreach (var v in ScrapLoot.Values) sum += v;
            return sum;
        }
        public float Life = 180f;

        void Update()
        {
            Life -= Time.deltaTime;
            if (Life > 0f) return;
            var gm = GameManager.I;
            if (gm == null) return;
            if (gm.Selected == this) gm.Select(null);
            gm.View.RemoveObject(this);
        }
    }
}
