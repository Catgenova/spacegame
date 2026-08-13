using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Code-built ship silhouettes (one per hull class), NPC bodies per pirate
    /// tier, and the background starfield. All primitives, no assets.
    /// Forward is +Z on every hull.
    /// </summary>
    public static class ShipVisuals
    {
        static GameObject Part(Transform parent, PrimitiveType t, Vector3 pos, Vector3 scale,
            Vector3 euler, Color c, bool emissive = false)
        {
            var p = GameObject.CreatePrimitive(t);
            Object.Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(parent, false);
            p.transform.localPosition = pos;
            p.transform.localScale = scale;
            p.transform.localRotation = Quaternion.Euler(euler.x, euler.y, euler.z);
            p.GetComponent<Renderer>().material = SystemView.Mat(c, emissive);
            return p;
        }

        /// <summary>Every ship is a generated body and builds its own mesh from
        /// its hash. The Probe is the one hand-built hull; anything else that is
        /// not generated falls back to it rather than flying invisibly.</summary>
        public static GameObject BuildHull(string hullId, Transform shipRoot)
            => ShipGen.IsGeneratedId(hullId)
                ? ShipGen.BuildHull(hullId, shipRoot)
                : BuildProbe(shipRoot);

        /// <summary>The escape pod: a cramped sphere, a mining head on a stalk,
        /// a pair of stub radiators and one oversized drive bell. It should read
        /// instantly as "this is not a ship", because it isn't.</summary>
        static GameObject BuildProbe(Transform shipRoot)
        {
            var root = new GameObject("Hull");
            root.transform.SetParent(shipRoot, false);
            var t = root.transform;
            var shell = new Color(0.78f, 0.72f, 0.36f);   // scuffed rescue yellow
            var dark = new Color(0.22f, 0.24f, 0.28f);
            var glow = new Color(0.45f, 0.8f, 1f);

            // Pressure sphere, slightly squashed, with a dark equatorial band.
            Part(t, PrimitiveType.Sphere, Vector3.zero, new Vector3(1.6f, 1.35f, 1.7f), Vector3.zero, shell);
            Part(t, PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.68f, 0.22f, 1.68f), Vector3.zero, dark);
            // Canopy blister, forward and up.
            Part(t, PrimitiveType.Sphere, new Vector3(0f, 0.45f, 0.62f), new Vector3(0.78f, 0.5f, 0.8f),
                Vector3.zero, new Color(0.30f, 0.52f, 0.68f));
            // Mining head on a short ventral stalk.
            Part(t, PrimitiveType.Cylinder, new Vector3(0f, -0.72f, 0.75f), new Vector3(0.16f, 0.5f, 0.16f),
                new Vector3(70f, 0f, 0f), dark);
            Part(t, PrimitiveType.Cylinder, new Vector3(0f, -0.95f, 1.35f), new Vector3(0.34f, 0.3f, 0.34f),
                new Vector3(70f, 0f, 0f), new Color(0.62f, 0.44f, 0.30f));
            // Stub radiators — no mid or low slots to cool, just the drive.
            Part(t, PrimitiveType.Cube, new Vector3(-1.15f, 0.1f, -0.5f), new Vector3(0.9f, 0.08f, 0.7f),
                new Vector3(0f, 0f, 14f), dark);
            Part(t, PrimitiveType.Cube, new Vector3(1.15f, 0.1f, -0.5f), new Vector3(0.9f, 0.08f, 0.7f),
                new Vector3(0f, 0f, -14f), dark);
            // Oversized drive bell: the instant-warp coil is most of the pod.
            Part(t, PrimitiveType.Cylinder, new Vector3(0f, 0f, -1.35f), new Vector3(0.95f, 0.45f, 0.95f),
                new Vector3(90f, 0f, 0f), dark);
            Part(t, PrimitiveType.Cylinder, new Vector3(0f, 0f, -1.85f), new Vector3(0.7f, 0.1f, 0.7f),
                new Vector3(90f, 0f, 0f), glow, true);
            return root;
        }

        /// <summary>Pirates get their own mesh: a skull hull with segmented
        /// armour, built per class by PirateShipMesh.</summary>
        public static void BuildNpcVisual(NpcDef def, Transform root)
        {
            PirateShipMesh.Build(def, root);
        }

        public static void BuildWreckVisual(Transform root, float s)
        {
            var scorched = new Color(0.22f, 0.2f, 0.19f);
            var ember = new Color(0.9f, 0.45f, 0.15f);
            Part(root, PrimitiveType.Cube, Vector3.zero,
                new Vector3(1.6f * s, 0.9f * s, 2.2f * s), new Vector3(10f, 25f, 5f), scorched);
            Part(root, PrimitiveType.Cube, new Vector3(1.1f * s, 0.4f * s, -0.8f * s),
                new Vector3(1.2f * s, 0.3f * s, 0.9f * s), new Vector3(-15f, 60f, 20f), scorched);
            Part(root, PrimitiveType.Cube, new Vector3(-0.9f * s, -0.3f * s, 0.6f * s),
                new Vector3(0.7f * s, 0.6f * s, 0.5f * s), new Vector3(30f, 10f, 45f), scorched);
            Part(root, PrimitiveType.Cube, new Vector3(0.2f * s, 0f, -0.2f * s),
                new Vector3(0.35f * s, 0.35f * s, 0.35f * s), Vector3.zero, ember, true);
        }

        /// <summary>An anomaly core: a slowly tumbling shattered mass with a hot
        /// unstable centre that reads brighter the richer the tier.</summary>
        public static void BuildSiteVisual(Transform root, int tier)
        {
            var shell = new Color(0.20f, 0.22f, 0.26f);
            var glow = tier == 3 ? new Color(0.95f, 0.55f, 1f)
                : tier == 2 ? new Color(0.55f, 0.85f, 1f)
                : new Color(0.6f, 1f, 0.75f);
            float s = 1f + 0.35f * tier;
            Part(root, PrimitiveType.Cube, Vector3.zero,
                new Vector3(3.4f * s, 2.2f * s, 3.0f * s), new Vector3(18f, 30f, 12f), shell);
            Part(root, PrimitiveType.Cube, new Vector3(1.9f * s, 0.7f * s, -1.3f * s),
                new Vector3(1.5f * s, 1.4f * s, 1.2f * s), new Vector3(-24f, 55f, 30f), shell);
            Part(root, PrimitiveType.Cube, new Vector3(-1.7f * s, -0.6f * s, 1.1f * s),
                new Vector3(1.2f * s, 1.0f * s, 1.6f * s), new Vector3(40f, 15f, 50f), shell);
            Part(root, PrimitiveType.Sphere, Vector3.zero,
                new Vector3(1.5f * s, 1.5f * s, 1.5f * s), Vector3.zero, glow, true);
            root.gameObject.AddComponent<SlowTumble>();
        }

        /// <summary>A sealed container: a banded crate with a lit seam.</summary>
        public static void BuildContainerVisual(Transform root, int tier)
        {
            var body = new Color(0.30f, 0.28f, 0.24f);
            var seam = tier == 3 ? new Color(0.95f, 0.6f, 1f)
                : tier == 2 ? new Color(0.55f, 0.85f, 1f)
                : new Color(0.6f, 1f, 0.75f);
            Part(root, PrimitiveType.Cube, Vector3.zero,
                new Vector3(1.5f, 1.5f, 2.0f), Vector3.zero, body);
            Part(root, PrimitiveType.Cube, new Vector3(0f, 0f, 0.05f),
                new Vector3(1.62f, 0.30f, 0.9f), Vector3.zero, body * 0.7f);
            Part(root, PrimitiveType.Cube, new Vector3(0f, 0.78f, 0f),
                new Vector3(0.5f, 0.10f, 1.2f), Vector3.zero, seam, true);
            root.gameObject.AddComponent<SlowTumble>();
        }

        /// <summary>Distant emissive cubes pinned to the camera position — a cheap skybox.</summary>
        public static GameObject BuildStarfield()
        {
            var root = new GameObject("Starfield");
            root.AddComponent<StarfieldFollow>();
            for (int i = 0; i < 420; i++)
            {
                var dir = Random.onUnitSphere;
                float r = 600000f * (0.75f + Random.value * 0.5f);
                float size = 900f + Random.value * 2200f;
                float warm = Random.value;
                var c = warm > 0.85f
                    ? new Color(1f, 0.85f, 0.7f)
                    : new Color(0.85f + Random.value * 0.15f, 0.9f, 1f);
                var star = Part(root.transform, PrimitiveType.Cube, dir * r,
                    Vector3.one * size, new Vector3(Random.value * 90f, Random.value * 90f, 0f), c, true);
                star.name = "star";
            }
            return root;
        }
    }

    /// <summary>Keeps the starfield centered on the camera (no parallax = infinitely far).</summary>
    /// <summary>Lazy tumble for drifting wreckage — sells "unstable" for free.</summary>
    public class SlowTumble : MonoBehaviour
    {
        Vector3 _spin;

        void Start()
        {
            _spin = new Vector3(Random.Range(-9f, 9f), Random.Range(-12f, 12f), Random.Range(-7f, 7f));
        }

        void Update()
        {
            transform.Rotate(_spin.x * Time.deltaTime, _spin.y * Time.deltaTime,
                _spin.z * Time.deltaTime, Space.Self);
        }
    }

    public class StarfieldFollow : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.position = cam.transform.position;
        }
    }
}
