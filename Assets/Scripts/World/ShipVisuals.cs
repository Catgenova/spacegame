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

        public static GameObject BuildHull(string hullId, Transform shipRoot)
        {
            // Generated bodies build their own unique mesh from the hash.
            if (HiveGenerator.IsHiveId(hullId))
                return HiveShipMesh.Build(HiveGenerator.HashFromId(hullId), shipRoot);

            var root = new GameObject("Hull");
            root.transform.SetParent(shipRoot, false);
            var t = root.transform;
            var steel = new Color(0.45f, 0.62f, 0.85f);
            var dark = new Color(0.25f, 0.34f, 0.48f);
            var engine = new Color(0.45f, 0.8f, 1f);

            switch (hullId)
            {
                case "prospector":
                    Part(t, PrimitiveType.Cube, Vector3.zero, new Vector3(3.2f, 2.2f, 4.5f), Vector3.zero, new Color(0.75f, 0.68f, 0.45f));
                    Part(t, PrimitiveType.Cylinder, new Vector3(-1.6f, 0f, 2.6f), new Vector3(0.7f, 1.2f, 0.7f), new Vector3(90f, 0f, 0f), dark);
                    Part(t, PrimitiveType.Cylinder, new Vector3(1.6f, 0f, 2.6f), new Vector3(0.7f, 1.2f, 0.7f), new Vector3(90f, 0f, 0f), dark);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0f, -2.6f), new Vector3(1.6f, 1.2f, 0.6f), Vector3.zero, engine, true);
                    break;
                case "talon":
                    Part(t, PrimitiveType.Capsule, Vector3.zero, new Vector3(1.3f, 3.6f, 1.3f), new Vector3(90f, 0f, 0f), steel);
                    Part(t, PrimitiveType.Cube, new Vector3(-1.4f, 0f, 0.8f), new Vector3(1.6f, 0.25f, 1.6f), new Vector3(0f, 0f, 8f), dark);
                    Part(t, PrimitiveType.Cube, new Vector3(1.4f, 0f, 0.8f), new Vector3(1.6f, 0.25f, 1.6f), new Vector3(0f, 0f, -8f), dark);
                    Part(t, PrimitiveType.Cube, new Vector3(-0.7f, 0.5f, 1.8f), new Vector3(0.25f, 0.25f, 1.8f), Vector3.zero, dark);
                    Part(t, PrimitiveType.Cube, new Vector3(0.7f, 0.5f, 1.8f), new Vector3(0.25f, 0.25f, 1.8f), Vector3.zero, dark);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0f, -3.4f), new Vector3(1.1f, 0.8f, 0.6f), Vector3.zero, engine, true);
                    break;
                case "mule":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0f, -0.8f), new Vector3(3.6f, 3.2f, 6.5f), Vector3.zero, new Color(0.6f, 0.58f, 0.52f));
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.6f, 3.4f), new Vector3(2.2f, 1.6f, 1.6f), Vector3.zero, steel);
                    Part(t, PrimitiveType.Cube, new Vector3(-1.3f, -0.8f, -4.4f), new Vector3(1.1f, 1.1f, 0.7f), Vector3.zero, engine, true);
                    Part(t, PrimitiveType.Cube, new Vector3(1.3f, -0.8f, -4.4f), new Vector3(1.1f, 1.1f, 0.7f), Vector3.zero, engine, true);
                    break;
                case "aurora":
                    Part(t, PrimitiveType.Capsule, Vector3.zero, new Vector3(1.8f, 5.5f, 1.8f), new Vector3(90f, 0f, 0f), new Color(0.55f, 0.7f, 0.95f));
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.9f, -1f), new Vector3(0.5f, 1.4f, 3.5f), Vector3.zero, dark);
                    Part(t, PrimitiveType.Cube, new Vector3(-2.4f, 0f, -1.5f), new Vector3(2.8f, 0.3f, 2.4f), new Vector3(0f, 0f, 6f), dark);
                    Part(t, PrimitiveType.Cube, new Vector3(2.4f, 0f, -1.5f), new Vector3(2.8f, 0.3f, 2.4f), new Vector3(0f, 0f, -6f), dark);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0f, -5.2f), new Vector3(1.5f, 1f, 0.8f), Vector3.zero, engine, true);
                    break;
                default: // wasp
                    Part(t, PrimitiveType.Capsule, Vector3.zero, new Vector3(1.4f, 2.6f, 1.4f), new Vector3(90f, 0f, 0f), steel);
                    Part(t, PrimitiveType.Cube, new Vector3(-1.1f, 0f, -0.4f), new Vector3(1.2f, 0.2f, 1.2f), Vector3.zero, dark);
                    Part(t, PrimitiveType.Cube, new Vector3(1.1f, 0f, -0.4f), new Vector3(1.2f, 0.2f, 1.2f), Vector3.zero, dark);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0f, -2.4f), new Vector3(0.9f, 0.6f, 0.5f), Vector3.zero, engine, true);
                    break;
            }
            return root;
        }

        public static void BuildNpcVisual(NpcDef def, Transform root)
        {
            if (def.Id == "convoyhauler")
            {
                // A fat armored box with engine blocks — loot on legs.
                var plating = new Color(0.45f, 0.28f, 0.22f);
                var glow = new Color(1f, 0.5f, 0.25f);
                Part(root, PrimitiveType.Cube, Vector3.zero, new Vector3(5f, 4f, 9f), Vector3.zero, plating);
                Part(root, PrimitiveType.Cube, new Vector3(0f, 0.8f, 4.8f), new Vector3(2.6f, 2f, 1.6f), Vector3.zero, new Color(0.6f, 0.38f, 0.3f));
                Part(root, PrimitiveType.Cube, new Vector3(-1.8f, -1f, -5.2f), new Vector3(1.4f, 1.4f, 1f), Vector3.zero, glow, true);
                Part(root, PrimitiveType.Cube, new Vector3(1.8f, -1f, -5.2f), new Vector3(1.4f, 1.4f, 1f), Vector3.zero, glow, true);
                return;
            }

            float s = def.Id == "overlord" ? 2.1f : def.Id == "marauder" ? 1.45f : 1f;
            var body = def.Id == "overlord" ? new Color(0.55f, 0.1f, 0.14f) : new Color(0.75f, 0.22f, 0.2f);
            var accent = new Color(1f, 0.35f, 0.25f);
            Part(root, PrimitiveType.Capsule, Vector3.zero,
                new Vector3(1.3f * s, 2.6f * s, 1.3f * s), new Vector3(90f, 0f, 0f), body);
            Part(root, PrimitiveType.Cube, new Vector3(-1.2f * s, 0f, -0.3f * s),
                new Vector3(1.4f * s, 0.2f * s, 1.1f * s), new Vector3(0f, 0f, 15f), body);
            Part(root, PrimitiveType.Cube, new Vector3(1.2f * s, 0f, -0.3f * s),
                new Vector3(1.4f * s, 0.2f * s, 1.1f * s), new Vector3(0f, 0f, -15f), body);
            Part(root, PrimitiveType.Cube, new Vector3(0f, 0f, -2.2f * s),
                new Vector3(0.8f * s, 0.5f * s, 0.4f * s), Vector3.zero, accent, true);
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
    public class StarfieldFollow : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.position = cam.transform.position;
        }
    }
}
