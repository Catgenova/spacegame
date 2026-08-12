using UnityEngine;

namespace SpaceGame
{
    /// <summary>Base for every selectable thing in space.</summary>
    public class SpaceObject : MonoBehaviour
    {
        public string Id;
        public string DisplayName;
        public ObjKind Kind;
    }

    public class CelestialBody : SpaceObject
    {
        public Celestial Data;
    }

    public class AsteroidBody : SpaceObject
    {
        public AsteroidData Data;

        /// <summary>Shrink visual with remaining ore.</summary>
        public void SyncScale()
        {
            float s = Mathf.Clamp(2f + Data.Amount / 800f, 2f, 8f);
            transform.localScale = new Vector3(s, s * 0.85f, s * 1.1f);
        }
    }
}
