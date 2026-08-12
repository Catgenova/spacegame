using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Generates the Hive body mesh from a 10-digit hash — v2, high resolution.
    /// True wasp anatomy: head bulge, thorax, waist pinch, striped abdomen and
    /// a stinger tail, lofted smoothly (Catmull-Rom stations, 16-point rounded
    /// profile, shared-vertex smooth normals) with crisp mechanical details on
    /// top: curved segmented antennae with joint beads, three-part folded legs,
    /// thickness-extruded wing blades, a turret drum + barrel marking the
    /// hardpoint, a teal web-emitter ring on the belly, spine greebles, and
    /// twin engines with inset nozzles and glow discs.
    ///
    /// Submeshes: 0 gold metal, 1 black metal, 2 amber glow, 3 teal glow.
    /// All randomness is rolled up front in a fixed order (Roll + a separate
    /// panel stream), so the hash-to-body mapping is stable and portable.
    /// </summary>
    public static class HiveShipMesh
    {
        const int Rings = 26;   // hull loft samples
        const int Segs = 16;    // profile points per ring

        class Genome
        {
            public float L, W, H, Head, Waist, Abdomen, Nose;
            public float AntLen, AntUp, AntCurve;
            public float WingSpan, WingSweep, LegScale, Panels, StripePhase;
            public float Hue, Sat, Val, SurfAmp, SurfPhase;
            public int Legs, Stripes, Greebles;
            public float[] LegLen1 = new float[4], LegLen2 = new float[4];
            public Color Gold;
        }

        static Genome Roll(string hash)
        {
            var rng = Rng.Stream("hivebody:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new Genome();
            g.L = R(5.0f, 6.2f);
            g.W = R(0.78f, 1.08f);
            g.H = R(0.60f, 0.85f);
            g.Head = R(0.90f, 1.15f);
            g.Waist = R(0.42f, 0.60f);
            g.Abdomen = R(0.90f, 1.12f);
            g.Nose = R(0.50f, 0.95f);
            g.AntLen = R(3.4f, 5.2f);
            g.AntUp = R(0.35f, 0.65f);
            g.AntCurve = R(0.20f, 0.60f);
            g.WingSpan = R(1.7f, 2.9f);
            g.WingSweep = R(0.9f, 1.9f);
            g.Legs = rng.NextDouble() < 0.5 ? 1 : 2;
            g.LegScale = R(0.9f, 1.2f);
            g.Panels = R(0.05f, 0.14f);
            g.Stripes = 2 + rng.Next(3);
            g.StripePhase = R(0f, 1f);
            g.Hue = R(0.09f, 0.135f);
            g.Sat = R(0.75f, 0.95f);
            g.Val = R(0.72f, 0.92f);
            g.SurfAmp = R(0f, 1f);
            g.SurfPhase = R(0f, 1f);
            g.Greebles = 3 + rng.Next(4);
            for (int i = 0; i < 4; i++) g.LegLen1[i] = R(1.0f, 1.4f);
            for (int i = 0; i < 4; i++) g.LegLen2[i] = R(1.5f, 2.2f);
            g.Gold = Color.HSVToRGB(g.Hue, g.Sat, g.Val);
            return g;
        }

        class Builder
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<int>[] Sub = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };

            public int Add(Vector3 v) { V.Add(v); return V.Count - 1; }

            public void Face(int a, int b, int c, int mat)
            {
                Sub[mat].Add(a); Sub[mat].Add(b); Sub[mat].Add(c);
            }

            // a=front-upper, b=front-lower, c=back-lower, d=back-upper (outward CW).
            public void FaceQ(int a, int b, int c, int d, int mat)
            {
                Face(a, d, c, mat); Face(a, c, b, mat);
            }

            // Unshared (crisp/flat) primitives; DS = double-sided.
            public void TriU(Vector3 a, Vector3 b, Vector3 c, int mat)
            {
                Face(Add(a), Add(b), Add(c), mat);
            }

            public void TriUDS(Vector3 a, Vector3 b, Vector3 c, int mat)
            {
                TriU(a, b, c, mat); TriU(a, c, b, mat);
            }

            public void QuadUDS(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int mat)
            {
                TriUDS(a, d, c, mat); TriUDS(a, c, b, mat);
            }
        }

        // Catmull-Rom over ordered control points.
        static float CrSample(float[] ts, float[] vs, float t)
        {
            int n = ts.Length;
            if (t <= ts[0]) return vs[0];
            if (t >= ts[n - 1]) return vs[n - 1];
            int i = 0;
            while (i < n - 2 && t > ts[i + 1]) i++;
            float u = (t - ts[i]) / (ts[i + 1] - ts[i]);
            float p0 = vs[Mathf.Max(0, i - 1)], p1 = vs[i], p2 = vs[i + 1], p3 = vs[Mathf.Min(n - 1, i + 2)];
            float u2 = u * u, u3 = u2 * u;
            return 0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * u3);
        }

        // Rounded-square profile point k of Segs; x right, y up (front view).
        static Vector2 ProfilePt(int k)
        {
            float ang = k / (float)Segs * Mathf.PI * 2f;
            float s = Mathf.Sin(ang), c = Mathf.Cos(ang);
            float x = Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 0.78f);
            float y = Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 0.82f);
            if (y < 0f) y *= 0.85f; // flattened belly
            return new Vector2(x, y);
        }

        static void Basis(Vector3 dir, out Vector3 right, out Vector3 up)
        {
            var refUp = Mathf.Abs(dir.y) > 0.93f ? Vector3.forward : Vector3.up;
            right = Vector3.Cross(refUp, dir).normalized;
            up = Vector3.Cross(dir, right).normalized;
        }

        // Flat-shaded double-sided tube along a path (mechanical limb look).
        static void Tube(Builder b, Vector3[] path, float[] radii, int sides, int mat, bool capEnd)
        {
            var prev = new Vector3[sides];
            for (int i = 0; i < path.Length; i++)
            {
                Vector3 dir = i == 0 ? path[1] - path[0]
                    : i == path.Length - 1 ? path[i] - path[i - 1]
                    : path[i + 1] - path[i - 1];
                Basis(dir.normalized, out var right, out var up);
                var ring = new Vector3[sides];
                for (int k = 0; k < sides; k++)
                {
                    float a = k / (float)sides * Mathf.PI * 2f;
                    ring[k] = path[i] + right * (Mathf.Cos(a) * radii[i]) + up * (Mathf.Sin(a) * radii[i]);
                }
                if (i > 0)
                    for (int k = 0; k < sides; k++)
                        b.QuadUDS(prev[k], prev[(k + 1) % sides], ring[(k + 1) % sides], ring[k], mat);
                for (int k = 0; k < sides; k++) prev[k] = ring[k];
            }
            if (capEnd)
                for (int k = 0; k < sides; k++)
                    b.TriUDS(path[path.Length - 1], prev[k], prev[(k + 1) % sides], mat);
        }

        // Small flat-shaded ball (joints, antenna beads).
        static void Ball(Builder b, Vector3 c, float r, int mat)
        {
            const int lat = 4, lon = 6;
            var pts = new Vector3[lat + 1][];
            for (int i = 0; i <= lat; i++)
            {
                pts[i] = new Vector3[lon];
                float phi = i / (float)lat * Mathf.PI;
                for (int k = 0; k < lon; k++)
                {
                    float th = k / (float)lon * Mathf.PI * 2f;
                    pts[i][k] = c + new Vector3(
                        Mathf.Sin(phi) * Mathf.Cos(th) * r,
                        Mathf.Cos(phi) * r,
                        Mathf.Sin(phi) * Mathf.Sin(th) * r);
                }
            }
            for (int i = 0; i < lat; i++)
                for (int k = 0; k < lon; k++)
                    b.QuadUDS(pts[i][k], pts[i][(k + 1) % lon], pts[i + 1][(k + 1) % lon], pts[i + 1][k], mat);
        }

        // Axis-aligned greeble box.
        static void Box(Builder b, Vector3 c, Vector3 half, int mat)
        {
            var p000 = c + new Vector3(-half.x, -half.y, -half.z);
            var p001 = c + new Vector3(-half.x, -half.y, half.z);
            var p010 = c + new Vector3(-half.x, half.y, -half.z);
            var p011 = c + new Vector3(-half.x, half.y, half.z);
            var p100 = c + new Vector3(half.x, -half.y, -half.z);
            var p101 = c + new Vector3(half.x, -half.y, half.z);
            var p110 = c + new Vector3(half.x, half.y, -half.z);
            var p111 = c + new Vector3(half.x, half.y, half.z);
            b.QuadUDS(p011, p111, p110, p010, mat); // top
            b.QuadUDS(p001, p101, p100, p000, mat); // bottom
            b.QuadUDS(p011, p001, p101, p111, mat); // front (+z)
            b.QuadUDS(p010, p000, p100, p110, mat); // back (-z)
            b.QuadUDS(p111, p101, p100, p110, mat); // +x
            b.QuadUDS(p011, p001, p000, p010, mat); // -x
        }

        public static GameObject Build(string hash, Transform shipRoot)
        {
            var g = Roll(hash);
            var b = new Builder();

            // Pre-consume the panel stream in a fixed order.
            var panelRng = Rng.Stream("hivepanels:" + hash);
            var sparse = new bool[Rings - 1, Segs];
            for (int i = 0; i < Rings - 1; i++)
                for (int j = 0; j < Segs; j++)
                    sparse[i, j] = panelRng.NextDouble() < g.Panels;
            var greebleJit = new float[12];
            for (int i = 0; i < 12; i++) greebleJit[i] = (float)panelRng.NextDouble();

            // ---- hull loft: wasp silhouette control stations ----
            float[] cts = { 0.00f, 0.05f, 0.13f, 0.21f, 0.32f, 0.43f, 0.53f, 0.60f, 0.68f, 0.78f, 0.88f, 1.00f };
            float[] csc =
            {
                0.05f, 0.30f, 0.62f * g.Head, 0.50f * g.Head, 0.86f, 1.00f, 0.80f,
                g.Waist, 0.82f * g.Abdomen, 0.90f * g.Abdomen, 0.58f * g.Abdomen, 0.14f,
            };
            float[] clf = { 0.08f, 0.10f, 0.15f, 0.11f, 0.06f, 0.03f, 0.01f, 0.00f, -0.01f, -0.03f, -0.02f, 0.00f };

            var ringStart = new int[Rings];
            var ringT = new float[Rings];
            for (int i = 0; i < Rings; i++)
            {
                float t = i / (float)(Rings - 1);
                ringT[i] = t;
                float sc = CrSample(cts, csc, t);
                sc *= 1f + g.SurfAmp * 0.025f * Mathf.Sin((t * 5.5f + g.SurfPhase) * Mathf.PI * 2f);
                float lift = CrSample(cts, clf, t) * g.H;
                float z = (0.5f - t) * g.L;
                ringStart[i] = b.V.Count;
                for (int j = 0; j < Segs; j++)
                {
                    var p = ProfilePt(j);
                    b.Add(new Vector3(p.x * g.W * sc, p.y * g.H * sc + lift, z));
                }
            }

            // Panel scheme on the smooth hull (color only — normals stay smooth).
            for (int i = 0; i < Rings - 1; i++)
            {
                float tm = (ringT[i] + ringT[i + 1]) * 0.5f;
                for (int j = 0; j < Segs; j++)
                {
                    int j2 = (j + 1) % Segs;
                    bool topSeg = j >= Segs - 2 || j <= 1; // 14,15,0,1
                    bool canopy = topSeg && tm > 0.10f && tm < 0.26f;
                    bool stripe = false;
                    if (tm > 0.62f && tm < 0.96f)
                    {
                        float u = (tm - 0.62f) / 0.34f;
                        int band = (int)(u * g.Stripes * 2 + g.StripePhase * 2f);
                        stripe = band % 2 == 0;
                    }
                    bool side = sparse[i, j] && tm > 0.28f && tm < 0.58f;
                    int mat = canopy || stripe || side ? 1 : 0;
                    b.FaceQ(ringStart[i] + j, ringStart[i] + j2, ringStart[i + 1] + j2, ringStart[i + 1] + j, mat);
                }
            }

            // Nose cone and stinger (crisp black).
            var noseTip = new Vector3(0f, 0.06f * g.H, 0.5f * g.L + g.Nose);
            for (int j = 0; j < Segs; j++)
                b.TriU(noseTip, b.V[ringStart[0] + (j + 1) % Segs], b.V[ringStart[0] + j], 1);
            var stinger = new Vector3(0f, 0f, -0.5f * g.L - 0.55f);
            for (int j = 0; j < Segs; j++)
                b.TriU(stinger, b.V[ringStart[Rings - 1] + j], b.V[ringStart[Rings - 1] + (j + 1) % Segs], 1);

            // ---- antennae: curved segmented feelers with beads ----
            for (int side = -1; side <= 1; side += 2)
            {
                var mount = new Vector3(side * g.W * 0.26f, g.H * 0.5f, 0.30f * g.L);
                var dir = new Vector3(side * 0.16f, g.AntUp, -0.85f).normalized;
                var bend = new Vector3(0f, g.AntCurve, 0f);
                const int segsA = 5;
                var path = new Vector3[segsA + 1];
                var radii = new float[segsA + 1];
                for (int i = 0; i <= segsA; i++)
                {
                    float u = i / (float)segsA;
                    path[i] = mount + dir * (g.AntLen * u) + bend * (u * u);
                    radii[i] = Mathf.Lerp(0.06f, 0.014f, u);
                }
                Tube(b, path, radii, 6, 0, false);
                Ball(b, mount, 0.10f, 1);                      // base joint
                Ball(b, path[segsA], 0.045f, 1);               // tip bead
            }

            // ---- wing blades with real thickness ----
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                var rootF = new Vector3(s * g.W * 0.8f, 0.12f * g.H, 0.10f * g.L);
                var rootB = new Vector3(s * g.W * 0.72f, 0.06f * g.H, -0.12f * g.L);
                var tipB = new Vector3(s * (g.W * 0.8f + g.WingSpan), -0.22f * g.H, -0.12f * g.L - g.WingSweep);
                var tipF = new Vector3(s * (g.W * 0.8f + g.WingSpan * 0.75f), -0.13f * g.H, 0.02f * g.L - g.WingSweep * 0.4f);
                var th = new Vector3(0f, 0.05f, 0f);
                // top + bottom skins
                b.QuadUDS(rootF + th, rootB + th, tipB + th, tipF + th, 0);
                b.QuadUDS(rootF - th, rootB - th, tipB - th, tipF - th, 0);
                // edge strips (black rim)
                b.QuadUDS(rootF + th, rootF - th, rootB - th, rootB + th, 1);
                b.QuadUDS(rootB + th, rootB - th, tipB - th, tipB + th, 1);
                b.QuadUDS(tipB + th, tipB - th, tipF - th, tipF + th, 1);
                b.QuadUDS(tipF + th, tipF - th, rootF - th, rootF + th, 1);
            }

            // ---- legs: coxa tube, femur tube, black tarsus spike, joint balls ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int leg = 0; leg < g.Legs; leg++)
                {
                    int li = (side < 0 ? 0 : 2) + leg;
                    float zOff = (0.02f - leg * 0.16f) * g.L;
                    var mount = new Vector3(side * g.W * 0.55f, -0.32f * g.H, zOff);
                    var d1 = new Vector3(side * 0.55f, 0.35f, -0.5f).normalized;
                    float len1 = g.LegLen1[li] * g.LegScale;
                    var elbow = mount + d1 * len1;
                    Tube(b, new[] { mount, elbow }, new[] { 0.11f, 0.08f }, 6, 0, false);
                    Ball(b, elbow, 0.12f, 1);
                    var d2 = new Vector3(side * 0.12f, -0.82f, -0.45f).normalized;
                    float len2 = g.LegLen2[li] * g.LegScale;
                    var knee = elbow + d2 * (len2 * 0.45f);
                    var tip = elbow + d2 * len2 + new Vector3(0f, -0.1f, -0.25f);
                    Tube(b, new[] { elbow, knee }, new[] { 0.07f, 0.055f }, 6, 0, false);
                    Ball(b, knee, 0.08f, 1);
                    Tube(b, new[] { knee, tip }, new[] { 0.05f, 0.008f }, 5, 1, true);
                }
            }

            // ---- turret drum + barrel on the thorax spine (the hardpoint) ----
            var drumC = new Vector3(0f, g.H * 0.95f, 0.06f * g.L);
            Tube(b, new[] { drumC + Vector3.up * -0.06f, drumC + Vector3.up * 0.10f },
                new[] { 0.17f, 0.15f }, 8, 1, true);
            Tube(b, new[] { drumC + new Vector3(0f, 0.05f, 0.05f), drumC + new Vector3(0f, 0.05f, 0.95f) },
                new[] { 0.05f, 0.04f }, 6, 1, true);

            // ---- web emitter ring on the belly (teal glow) ----
            var webC = new Vector3(0f, -g.H * 0.78f, -0.02f * g.L);
            for (int k = 0; k < 8; k++)
            {
                float a0 = k / 8f * Mathf.PI * 2f, a1 = (k + 1) / 8f * Mathf.PI * 2f;
                var r0 = 0.14f; var r1 = 0.22f;
                Vector3 pA = webC + new Vector3(Mathf.Cos(a0) * r0, 0f, Mathf.Sin(a0) * r0);
                Vector3 pB = webC + new Vector3(Mathf.Cos(a0) * r1, 0f, Mathf.Sin(a0) * r1);
                Vector3 pC = webC + new Vector3(Mathf.Cos(a1) * r1, 0f, Mathf.Sin(a1) * r1);
                Vector3 pD = webC + new Vector3(Mathf.Cos(a1) * r0, 0f, Mathf.Sin(a1) * r0);
                b.QuadUDS(pA, pB, pC, pD, 3);
            }

            // ---- spine greebles ----
            for (int i = 0; i < g.Greebles; i++)
            {
                float t = 0.30f + i * (0.24f / Mathf.Max(1, g.Greebles - 1));
                float jx = (greebleJit[i * 2] - 0.5f) * 0.2f;
                float jz = (greebleJit[i * 2 + 1] - 0.5f) * 0.04f;
                float sc = CrSample(cts, csc, t);
                var c = new Vector3(jx, g.H * sc * 0.92f + 0.05f, (0.5f - t + jz) * g.L);
                Box(b, c, new Vector3(0.10f, 0.045f, 0.14f), i % 2 == 0 ? 1 : 0);
            }

            // ---- twin engines: casing, inset nozzle, glow disc ----
            for (int side = -1; side <= 1; side += 2)
            {
                var ec = new Vector3(side * g.W * 0.30f, -0.02f * g.H, -0.5f * g.L + 0.25f);
                var back = ec + Vector3.forward * -0.95f;
                Tube(b, new[] { ec, back }, new[] { 0.30f, 0.26f }, 10, 1, false);
                Tube(b, new[] { back, back + Vector3.forward * 0.18f }, new[] { 0.26f, 0.15f }, 10, 1, false);
                // glow disc
                var gc = back + Vector3.forward * 0.10f;
                for (int k = 0; k < 10; k++)
                {
                    float a0 = k / 10f * Mathf.PI * 2f, a1 = (k + 1) / 10f * Mathf.PI * 2f;
                    b.TriUDS(gc,
                        gc + new Vector3(Mathf.Cos(a0) * 0.14f, Mathf.Sin(a0) * 0.14f, 0f),
                        gc + new Vector3(Mathf.Cos(a1) * 0.14f, Mathf.Sin(a1) * 0.14f, 0f), 2);
                }
            }

            // ---- assemble ----
            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "hive_" + hash };
            mesh.SetVertices(b.V);
            mesh.subMeshCount = 4;
            for (int m = 0; m < 4; m++) mesh.SetTriangles(b.Sub[m], m);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.materials = new[]
            {
                Metal(g.Gold, 0.92f, 0.85f),
                Metal(new Color(0.07f, 0.07f, 0.09f), 0.88f, 0.7f),
                SystemView.Mat(new Color(1f, 0.62f, 0.25f), true),
                SystemView.Mat(new Color(0.35f, 0.95f, 0.85f), true),
            };
            return go;
        }

        static Material Metal(Color c, float metallic, float smooth)
        {
            var m = SystemView.Mat(c);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glossiness", smooth);  // Built-in Standard
            m.SetFloat("_Smoothness", smooth);  // URP Lit
            return m;
        }
    }
}
