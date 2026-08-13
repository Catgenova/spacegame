using UnityEngine;
using static SpaceGame.MeshKit;

namespace SpaceGame
{
    /// <summary>
    /// The pirate fleet: four hulls built to one visual doctrine.
    ///
    /// Every pirate ship is a scavenged animal skull bolted to a spine of dark
    /// segmented armour. The skull is bone-cream with hollow sockets and a red
    /// pinpoint burning where the eye was; behind it the hull is charcoal plate
    /// in banded segments, studded with red glow ports and brass fittings, and
    /// every one of them flies a torn flag off a mast on the spine. That is the
    /// faction read: whatever else changes between classes, a pirate looks like
    /// something dead wearing a gun battery.
    ///
    /// What changes per class is the animal and the weapon it carries:
    ///   rookie        corvid skull, long black beak, ragged blade fins  (Class 1)
    ///   marauder      fanged predator skull with a cannon in its mouth  (Class 2)
    ///   overlord      horned rhino skull, boxy stacked armour           (Class 3)
    ///   convoyhauler  rounded skull trailing curled claws, fat pod body (Class 3)
    ///
    /// Deterministic per class: every rookie looks like every other rookie, so a
    /// silhouette on the overview means something.
    /// </summary>
    public static class PirateShipMesh
    {
        // Material slots, shared by all four hulls.
        const int Bone = 0, Armour = 1, Glow = 2, Black = 3, Brass = 4;

        public static GameObject Build(NpcDef def, Transform root)
        {
            var b = new Builder();
            string id = def == null ? "rookie" : def.Id;

            if (id == "marauder") Marauder(b);
            else if (id == "overlord") Overlord(b);
            else if (id == "convoyhauler") Hauler(b);
            else Corvette(b);

            var go = new GameObject("PirateHull");
            go.transform.SetParent(root, false);
            var mesh = new Mesh { name = "pirate_" + id };
            mesh.SetVertices(b.V);
            mesh.subMeshCount = 5;
            for (int m = 0; m < 5; m++) mesh.SetTriangles(b.Sub[m], m);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().mesh = mesh;
            go.AddComponent<MeshRenderer>().materials = new[]
            {
                Metal(new Color(0.84f, 0.80f, 0.68f), 0.18f, 0.42f),  // weathered bone
                Metal(new Color(0.13f, 0.14f, 0.16f), 0.82f, 0.55f),  // charcoal plate
                SystemView.Mat(new Color(1f, 0.34f, 0.10f), true),    // furnace red
                Metal(new Color(0.05f, 0.05f, 0.06f), 0.70f, 0.80f),  // black horn/beak
                Metal(new Color(0.52f, 0.38f, 0.16f), 0.85f, 0.60f),  // brass fittings
            };
            return go;
        }

        // ---------------- shared vocabulary ----------------

        /// <summary>A skull as a loft: rings marching forward from the back of the
        /// cranium to the snout, each an ellipse scaled by the given curves. This
        /// is what makes the bone read as a skull rather than a sphere — the
        /// cranium swells, pinches at the brow, then tapers into the muzzle.</summary>
        static void SkullLoft(Builder b, float zBack, float zNose, float halfW, float halfH,
            float[] ts, float[] ws, float[] hs, float[] ys, int rings, int segs, int mat)
        {
            var ring = new Vector3[rings][];
            for (int i = 0; i < rings; i++)
            {
                float t = i / (float)(rings - 1);
                float z = Mathf.Lerp(zBack, zNose, t);
                float rw = halfW * CrSample(ts, ws, t);
                float rh = halfH * CrSample(ts, hs, t);
                float cy = halfH * CrSample(ts, ys, t);
                ring[i] = new Vector3[segs];
                for (int j = 0; j < segs; j++)
                {
                    float a = Mathf.PI * 2f * j / segs;
                    ring[i][j] = new Vector3(Mathf.Cos(a) * rw, cy + Mathf.Sin(a) * rh, z);
                }
            }
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < segs; j++)
                {
                    int j2 = (j + 1) % segs;
                    b.QuadUDS(ring[i][j], ring[i][j2], ring[i + 1][j2], ring[i + 1][j], mat);
                }
            // Caps, so the skull is closed at both ends.
            var backC = new Vector3(0f, halfH * CrSample(ts, ys, 0f), zBack);
            var noseC = new Vector3(0f, halfH * CrSample(ts, ys, 1f), zNose);
            for (int j = 0; j < segs; j++)
            {
                int j2 = (j + 1) % segs;
                b.TriUDS(backC, ring[0][j], ring[0][j2], mat);
                b.TriUDS(noseC, ring[rings - 1][j2], ring[rings - 1][j], mat);
            }
        }

        /// <summary>A hollow eye socket: a dark cylinder sunk into the bone with a
        /// rim of brow around it and a red pinpoint burning at the back.</summary>
        static void EyeSocket(Builder b, Vector3 c, float side, float r, float depth)
        {
            var inward = new Vector3(-side, 0f, 0f);
            Tube(b, new[] { c, c + inward * depth }, new[] { r, r * 0.62f }, 10, Black, true);
            // Brow rim standing proud of the socket.
            Tube(b, new[] { c + new Vector3(side * 0.02f, 0f, 0f), c + new Vector3(side * 0.07f, 0f, 0f) },
                new[] { r * 1.12f, r * 1.04f }, 10, Bone, false);
            Ball(b, c + inward * (depth * 0.78f), r * 0.26f, Glow, 3, 6);
        }

        /// <summary>One armour segment: a banded ring, a spine block on top, and
        /// glow ports punched through the plate.</summary>
        static void ArmourSegment(Builder b, float z, float halfW, float halfH, float len,
            int ports, bool boxy)
        {
            if (boxy)
            {
                // Stacked blocks: the heavy hulls read as crates welded together.
                BevelBox(b, new Vector3(0f, 0f, z), new Vector3(halfW, halfH, len), 0.10f, Armour);
                BevelBox(b, new Vector3(0f, halfH * 0.92f, z),
                    new Vector3(halfW * 0.62f, halfH * 0.30f, len * 0.80f), 0.06f, Armour);
            }
            else
            {
                Tube(b, new[] { new Vector3(0f, 0f, z + len), new Vector3(0f, 0f, z - len) },
                    new[] { halfW, halfW }, 12, Armour, false);
            }
            // Brass band around the joint.
            Tube(b, new[] { new Vector3(0f, 0f, z - len * 0.92f), new Vector3(0f, 0f, z - len * 1.06f) },
                new[] { halfW * 1.06f, halfW * 1.06f }, 12, Brass, false);
            // Glow ports around the flanks.
            for (int i = 0; i < ports; i++)
            {
                float a = Mathf.PI * 2f * (i + 0.5f) / ports;
                var p = new Vector3(Mathf.Cos(a) * halfW * 0.99f, Mathf.Sin(a) * halfH * 0.99f, z);
                Box(b, p, new Vector3(0.10f, 0.10f, len * 0.34f), Glow);
            }
        }

        /// <summary>The mast and torn flag every pirate flies.</summary>
        static void Flag(Builder b, Vector3 basePt, float h, float w)
        {
            var top = basePt + new Vector3(0f, h, 0f);
            Tube(b, new[] { basePt, top }, new[] { 0.07f, 0.045f }, 6, Brass, true);
            // Two ragged panels, so the flag has a tattered trailing edge.
            var a = top + new Vector3(0f, -0.05f, 0f);
            var c = top + new Vector3(0f, -h * 0.42f, 0f);
            b.QuadUDS(a, a + new Vector3(0f, 0f, -w), c + new Vector3(0f, 0f, -w * 0.72f), c, Black);
            b.TriUDS(a + new Vector3(0f, 0f, -w), c + new Vector3(0f, 0f, -w * 0.72f),
                a + new Vector3(0f, -h * 0.14f, -w * 1.28f), Black);
        }

        /// <summary>A curved tapering horn, claw or fang: the same shape serves a
        /// rhino's horn, a hauler's claws and a predator's teeth.</summary>
        static void Horn(Builder b, Vector3 root, Vector3 dir, Vector3 curl,
            float len, float r, int mat, int tipMat)
        {
            var d = dir.normalized;
            var p1 = root + d * (len * 0.36f) + curl * (len * 0.06f);
            var p2 = root + d * (len * 0.70f) + curl * (len * 0.26f);
            var p3 = root + d * len + curl * (len * 0.58f);
            Tube(b, new[] { root, p1, p2 }, new[] { r, r * 0.72f, r * 0.42f }, 8, mat, false);
            Tube(b, new[] { p2, p3 }, new[] { r * 0.42f, r * 0.05f }, 8, tipMat, true);
        }

        /// <summary>Engine cluster at the stern, always burning red.</summary>
        static void Engines(Builder b, float z, float spread, float r, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.PI * 2f * i / count + Mathf.PI * 0.25f;
                var c = new Vector3(Mathf.Cos(a) * spread, Mathf.Sin(a) * spread * 0.66f, z);
                BevelBox(b, c + new Vector3(0f, 0f, r * 1.4f),
                    new Vector3(r * 0.92f, r * 0.92f, r * 1.5f), 0.08f, Armour);
                Nozzle(b, c, Vector3.back, r * 0.78f, r * 1.25f, 10, Armour, Glow);
            }
        }

        // ---------------- Class 1: corvid stealth corvette ----------------

        static void Corvette(Builder b)
        {
            const float S = 1.0f;
            float[] ts = { 0.00f, 0.18f, 0.38f, 0.58f, 0.78f, 1.00f };
            float[] ws = { 0.52f, 1.00f, 0.94f, 0.70f, 0.36f, 0.14f };
            float[] hs = { 0.54f, 1.00f, 0.88f, 0.62f, 0.32f, 0.12f };
            float[] ys = { 0.00f, 0.07f, 0.02f, -0.08f, -0.20f, -0.30f };
            SkullLoft(b, -0.6f * S, 3.4f * S, 1.05f * S, 0.95f * S, ts, ws, hs, ys, 18, 16, Bone);

            // Huge corvid eye sockets, set well forward on the cranium.
            for (int side = -1; side <= 1; side += 2)
                EyeSocket(b, new Vector3(side * 0.80f * S, 0.10f * S, 1.05f * S),
                    side, 0.46f * S, 0.52f * S);

            // The beak: two long slender black mandibles, the upper overhanging.
            Horn(b, new Vector3(0f, -0.10f * S, 3.2f * S), Vector3.forward, Vector3.down,
                3.5f * S, 0.30f * S, Black, Black);
            Horn(b, new Vector3(0f, -0.34f * S, 3.1f * S), Vector3.forward, Vector3.down,
                2.6f * S, 0.22f * S, Black, Black);
            // Nare slit on each side of the upper beak.
            for (int side = -1; side <= 1; side += 2)
                Box(b, new Vector3(side * 0.16f * S, -0.06f * S, 3.9f * S),
                    new Vector3(0.03f * S, 0.05f * S, 0.34f * S), Glow);

            // Slim armour spine: a corvette carries only three segments.
            for (int i = 0; i < 3; i++)
                ArmourSegment(b, (-1.1f - i * 1.15f) * S, (0.86f - i * 0.07f) * S,
                    (0.78f - i * 0.06f) * S, 0.52f * S, 4, false);

            // Ragged blade fins fanning off the stern — the corvette's signature.
            for (int side = -1; side <= 1; side += 2)
                for (int f = 0; f < 3; f++)
                {
                    var rt = new Vector3(side * 0.60f * S, (0.28f - f * 0.30f) * S,
                        (-3.1f - f * 0.22f) * S);
                    var span = new Vector3(side, 0.34f - f * 0.22f, -0.62f - f * 0.16f);
                    PlateFin(b, rt, span, Vector3.forward, (2.3f - f * 0.34f) * S,
                        1.25f * S, 0.34f * S, 0.10f * S, Armour, Armour, Black);
                }

            Flag(b, new Vector3(0f, 0.80f * S, -1.5f * S), 1.5f * S, 1.1f * S);
            Engines(b, -4.6f * S, 0.52f * S, 0.34f * S, 2);
        }

        // ---------------- Class 2: fanged heavy destroyer ----------------

        static void Marauder(Builder b)
        {
            const float S = 1.5f;
            float[] ts = { 0.00f, 0.16f, 0.34f, 0.52f, 0.74f, 1.00f };
            float[] ws = { 0.58f, 1.00f, 0.72f, 0.90f, 0.60f, 0.38f };
            float[] hs = { 0.66f, 1.00f, 0.70f, 0.62f, 0.46f, 0.30f };
            float[] ys = { 0.00f, 0.05f, -0.04f, -0.10f, -0.20f, -0.30f };
            SkullLoft(b, -0.7f * S, 3.1f * S, 1.10f * S, 0.98f * S, ts, ws, hs, ys, 18, 16, Bone);

            for (int side = -1; side <= 1; side += 2)
                EyeSocket(b, new Vector3(side * 0.86f * S, 0.16f * S, 0.85f * S),
                    side, 0.36f * S, 0.42f * S);

            // Open jaws: an upper palate and a dropped lower mandible, with the
            // black of the mouth between them.
            BevelBox(b, new Vector3(0f, -0.52f * S, 2.5f * S),
                new Vector3(0.74f * S, 0.16f * S, 1.15f * S), 0.06f, Bone);
            BevelBox(b, new Vector3(0f, -1.16f * S, 2.35f * S),
                new Vector3(0.66f * S, 0.15f * S, 1.05f * S), 0.06f, Bone);
            Box(b, new Vector3(0f, -0.84f * S, 2.4f * S),
                new Vector3(0.60f * S, 0.16f * S, 1.00f * S), Black);

            // Sabre teeth: long pair at the front, a row of smaller ones behind.
            for (int side = -1; side <= 1; side += 2)
            {
                Horn(b, new Vector3(side * 0.46f * S, -0.62f * S, 3.35f * S),
                    Vector3.down, Vector3.forward, 1.25f * S, 0.16f * S, Bone, Bone);
                Horn(b, new Vector3(side * 0.40f * S, -1.06f * S, 3.20f * S),
                    Vector3.up, Vector3.forward, 0.80f * S, 0.12f * S, Bone, Bone);
                for (int t = 0; t < 4; t++)
                {
                    float z = (2.9f - t * 0.52f) * S;
                    Horn(b, new Vector3(side * 0.52f * S, -0.64f * S, z),
                        Vector3.down, Vector3.zero, (0.46f - t * 0.05f) * S, 0.09f * S, Bone, Bone);
                    Horn(b, new Vector3(side * 0.46f * S, -1.04f * S, z),
                        Vector3.up, Vector3.zero, (0.38f - t * 0.04f) * S, 0.08f * S, Bone, Bone);
                }
            }

            // The cannon fed out through the mouth.
            Tube(b, new[] { new Vector3(0f, -0.86f * S, 2.2f * S), new Vector3(0f, -0.86f * S, 4.6f * S) },
                new[] { 0.17f * S, 0.13f * S }, 10, Black, false);
            Tube(b, new[] { new Vector3(0f, -0.86f * S, 4.6f * S), new Vector3(0f, -0.86f * S, 4.95f * S) },
                new[] { 0.20f * S, 0.18f * S }, 10, Brass, true);

            // Banded barrel body — five segments, heavily lit.
            for (int i = 0; i < 5; i++)
                ArmourSegment(b, (-1.3f - i * 1.28f) * S, (0.95f - i * 0.04f) * S,
                    (0.88f - i * 0.04f) * S, 0.58f * S, 6, false);

            // Dorsal spikes along the spine, as on the reference's back.
            for (int i = 0; i < 5; i++)
                Horn(b, new Vector3(0f, 0.82f * S, (-1.0f - i * 1.28f) * S),
                    new Vector3(0f, 1f, -0.35f), Vector3.zero, 0.78f * S, 0.14f * S, Armour, Black);

            Flag(b, new Vector3(0f, 1.05f * S, -1.4f * S), 1.7f * S, 1.25f * S);
            Engines(b, -7.4f * S, 0.62f * S, 0.40f * S, 3);
        }

        // ---------------- Class 3: horned rhino overlord ----------------

        static void Overlord(Builder b)
        {
            const float S = 2.1f;
            float[] ts = { 0.00f, 0.18f, 0.36f, 0.56f, 0.78f, 1.00f };
            float[] ws = { 0.66f, 1.00f, 0.80f, 0.94f, 0.72f, 0.44f };
            float[] hs = { 0.74f, 1.00f, 0.78f, 0.70f, 0.54f, 0.36f };
            float[] ys = { 0.00f, 0.03f, -0.06f, -0.14f, -0.24f, -0.34f };
            SkullLoft(b, -0.8f * S, 2.9f * S, 1.22f * S, 1.05f * S, ts, ws, hs, ys, 18, 16, Bone);

            for (int side = -1; side <= 1; side += 2)
                EyeSocket(b, new Vector3(side * 0.98f * S, 0.14f * S, 0.90f * S),
                    side, 0.40f * S, 0.46f * S);

            // The horn: a heavy forward-curling spike, and a smaller one behind it.
            Horn(b, new Vector3(0f, -0.10f * S, 2.85f * S),
                new Vector3(0f, 0.34f, 1f), Vector3.up, 2.9f * S, 0.44f * S, Bone, Black);
            Horn(b, new Vector3(0f, 0.30f * S, 2.05f * S),
                new Vector3(0f, 0.72f, 1f), Vector3.up, 1.15f * S, 0.24f * S, Bone, Black);
            // Cheek bosses and a row of molars along the jaw line.
            for (int side = -1; side <= 1; side += 2)
            {
                Blob(b, new Vector3(side * 0.92f * S, -0.62f * S, 1.85f * S),
                    new Vector3(0.30f * S, 0.26f * S, 0.52f * S), Bone, 5, 8);
                for (int t = 0; t < 4; t++)
                    Ball(b, new Vector3(side * 0.62f * S, -0.86f * S, (2.4f - t * 0.42f) * S),
                        0.13f * S, Bone, 3, 6);
            }

            // Boxy stacked armour: the overlord is crates and gun decks, not a tube.
            for (int i = 0; i < 5; i++)
                ArmourSegment(b, (-1.5f - i * 1.42f) * S, (1.08f - i * 0.05f) * S,
                    (0.82f - i * 0.03f) * S, 0.64f * S, 6, true);
            // A second row of blocks slung under the flanks.
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 4; i++)
                {
                    var c = new Vector3(side * 1.05f * S, -0.62f * S, (-1.8f - i * 1.42f) * S);
                    BevelBox(b, c, new Vector3(0.34f * S, 0.34f * S, 0.52f * S), 0.08f, Armour);
                    Box(b, c + new Vector3(side * 0.34f * S, 0f, 0f),
                        new Vector3(0.03f * S, 0.14f * S, 0.30f * S), Glow);
                }

            Flag(b, new Vector3(0f, 1.30f * S, -1.6f * S), 2.0f * S, 1.5f * S);
            Engines(b, -8.6f * S, 0.86f * S, 0.46f * S, 4);
        }

        // ---------------- Class 3: clawed convoy hauler ----------------

        static void Hauler(Builder b)
        {
            const float S = 2.4f;
            float[] ts = { 0.00f, 0.20f, 0.40f, 0.60f, 0.80f, 1.00f };
            float[] ws = { 0.70f, 1.00f, 0.86f, 0.90f, 0.72f, 0.46f };
            float[] hs = { 0.78f, 1.00f, 0.84f, 0.76f, 0.62f, 0.42f };
            float[] ys = { 0.00f, 0.01f, -0.08f, -0.16f, -0.26f, -0.36f };
            SkullLoft(b, -0.7f * S, 2.4f * S, 1.15f * S, 1.02f * S, ts, ws, hs, ys, 18, 16, Bone);

            for (int side = -1; side <= 1; side += 2)
                EyeSocket(b, new Vector3(side * 0.90f * S, 0.06f * S, 0.80f * S),
                    side, 0.44f * S, 0.50f * S);

            // Six curled claws reaching forward from under the skull: the raider's
            // grapples, and the reason nothing it catches gets away.
            for (int side = -1; side <= 1; side += 2)
                for (int c = 0; c < 3; c++)
                {
                    var root = new Vector3(side * (0.34f + c * 0.30f) * S,
                        (-0.70f - c * 0.10f) * S, (2.0f - c * 0.34f) * S);
                    var dir = new Vector3(side * (0.22f + c * 0.16f), -0.30f, 1f);
                    Horn(b, root, dir, Vector3.down, (2.5f - c * 0.22f) * S, 0.20f * S, Bone, Black);
                    // Brass banding partway along each claw.
                    Tube(b, new[] { root + dir.normalized * (0.9f * S), root + dir.normalized * (1.1f * S) },
                        new[] { 0.19f * S, 0.19f * S }, 8, Brass, false);
                }

            // Fat pod body: a wide barrel with cargo rings.
            for (int i = 0; i < 4; i++)
                ArmourSegment(b, (-1.4f - i * 1.55f) * S, (1.30f - i * 0.03f) * S,
                    (1.10f - i * 0.03f) * S, 0.72f * S, 8, false);

            // Engine pods in two banks, as on the reference's stern.
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 2; i++)
                {
                    var c = new Vector3(side * 1.35f * S, (0.30f - i * 0.85f) * S, -6.6f * S);
                    BevelBox(b, c, new Vector3(0.42f * S, 0.40f * S, 0.80f * S), 0.10f, Armour);
                    Nozzle(b, c + new Vector3(0f, 0f, -0.82f * S), Vector3.back,
                        0.30f * S, 0.44f * S, 10, Armour, Glow);
                }

            Flag(b, new Vector3(0f, 1.45f * S, -1.5f * S), 2.1f * S, 1.6f * S);
            Engines(b, -7.9f * S, 0.70f * S, 0.44f * S, 2);
        }
    }
}
