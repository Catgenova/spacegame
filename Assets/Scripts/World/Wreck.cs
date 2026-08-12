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
