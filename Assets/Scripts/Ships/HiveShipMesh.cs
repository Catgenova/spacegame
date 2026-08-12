using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Generates the Hive body mesh from a 10-digit hash — Mk.III, aligned to
    /// the type reference: one continuous dart-shaped hull with a long bladed
    /// nose, flat belly, dark canopy plate on the rear-top, bold angular black
    /// inset panels on the flanks, a segmented piston stack at the stern, two
    /// long straight antennae raking up-back, one kite wing blade per side
    /// with a black claw tip, two fold-forward segmented legs per side, a
    /// ventral fin, a low dorsal turret drum (the hardpoint) and a teal
    /// web-emitter ring on the belly (the web slot).
    ///
    /// Bodies of one type must read as the SAME ship: proportions vary only a
    /// few percent; individuality comes from the flank panel patchwork, hue,
    /// antenna rake, and limb pose. ~6k triangles; the hull shades smooth,
    /// machinery stays crisp. Submeshes: 0 gold, 1 black, 2 amber, 3 teal.
    /// </summary>
    public static class HiveShipMesh
    {
        const int Rings = 33;
        const int Segs = 20;

        class Genome
        {
            public float L, W, H, Nose, CanopyStart, CanopyLen;
            public float AntLen, AntRake, AntCurve;
            public float WingSpan, WingSweep, LegScale, FlankOdds;
            public float Hue, Sat, Val, SurfAmp, SurfPhase;
            public int StackSegs;
            public float[] LegL = new float[4], LegA = new float[4];
            public Color Gold;
        }

        static Genome Roll(string hash)
        {
            var rng = Rng.Stream("hivebody:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new Genome();
            g.L = R(5.6f, 6.1f);
            g.W = R(0.95f, 1.10f);
            g.H = R(0.50f, 0.58f);
            g.Nose = R(0.25f, 0.45f);
            g.CanopyStart = R(0.48f, 0.55f);
            g.CanopyLen = R(0.22f, 0.28f);
            g.AntLen = R(4.2f, 5.0f);
            g.AntRake = R(0.32f, 0.48f);
            g.AntCurve = R(0.00f, 0.15f);
            g.WingSpan = R(2.1f, 2.6f);
            g.WingSweep = R(1.15f, 1.55f);
            g.LegScale = R(0.92f, 1.10f);
            g.FlankOdds = R(0.45f, 0.70f);
            g.Hue = R(0.100f, 0.125f);
            g.Sat = R(0.82f, 0.92f);
            g.Val = R(0.78f, 0.90f);
            g.SurfAmp = R(0f, 0.5f);
            g.SurfPhase = R(0f, 1f);
            g.StackSegs = 4 + rng.Next(2);
            for (int i = 0; i < 4; i++) g.LegL[i] = R(0.90f, 1.10f);
            for (int i = 0; i < 4; i++) g.LegA[i] = R(-0.08f, 0.08f);
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

            public void FaceQ(int a, int b, int c, int d, int mat)
            {
                Face(a, d, c, mat); Face(a, c, b, mat);
            }

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

        // Wide, flat-bellied angular profile. k=0 top, clockwise (front view).
        static Vector2 ProfilePt(int k)
        {
            float ang = k / (float)Segs * Mathf.PI * 2f;
            float s = Mathf.Sin(ang), c = Mathf.Cos(ang);
            float x = Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 0.72f);
            float y = Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 0.90f);
            if (y < 0f) y *= 0.72f; // flat belly
            return new Vector2(x, y);
        }

        static void Basis(Vector3 dir, out Vector3 right, out Vector3 up)
        {
            var refUp = Mathf.Abs(dir.y) > 0.93f ? Vector3.forward : Vector3.up;
            right = Vector3.Cross(refUp, dir).normalized;
            up = Vector3.Cross(dir, right).normalized;
        }

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

        static void Ball(Builder b, Vector3 c, float r, int mat, int lat, int lon)
        {
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

        public static GameObject Build(string hash, Transform shipRoot)
        {
            var g = Roll(hash);
            var b = new Builder();
            float L = g.L, W = g.W, H = g.H;

            // Panel stream (fixed consumption order): 12 flank cells, then micro panels.
            var panelRng = Rng.Stream("hivepanels:" + hash);
            var flankCell = new bool[12];
            for (int i = 0; i < 12; i++) flankCell[i] = panelRng.NextDouble() < g.FlankOdds;
            var micro = new bool[Rings - 1, Segs];
            for (int i = 0; i < Rings - 1; i++)
                for (int j = 0; j < Segs; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.04;

            // ---- hull: one continuous dart, widest near the stern ----
            float[] cts = { 0.00f, 0.10f, 0.25f, 0.42f, 0.60f, 0.75f, 0.88f, 1.00f };
            float[] csc = { 0.035f, 0.20f, 0.42f, 0.68f, 0.90f, 1.00f, 0.96f, 0.78f };
            float[] clf = { 0.00f, 0.01f, 0.02f, 0.05f, 0.09f, 0.11f, 0.07f, 0.02f };

            var ringStart = new int[Rings];
            var ringT = new float[Rings];
            for (int i = 0; i < Rings; i++)
            {
                float t = i / (float)(Rings - 1);
                ringT[i] = t;
                float sc = CrSample(cts, csc, t);
                sc *= 1f + g.SurfAmp * 0.015f * Mathf.Sin((t * 6f + g.SurfPhase) * Mathf.PI * 2f);
                float lift = CrSample(cts, clf, t) * H;
                float z = (0.5f - t) * L;
                ringStart[i] = b.V.Count;
                for (int j = 0; j < Segs; j++)
                {
                    var p = ProfilePt(j);
                    b.Add(new Vector3(p.x * W * sc, p.y * H * sc + lift, z));
                }
            }

            // Paint scheme, reference-faithful: big bold shapes, minimal noise.
            for (int i = 0; i < Rings - 1; i++)
            {
                float tm = (ringT[i] + ringT[i + 1]) * 0.5f;
                for (int j = 0; j < Segs; j++)
                {
                    int j2 = (j + 1) % Segs;
                    int mat = 0;

                    // Dark chisel nose.
                    if (tm < 0.07f) mat = 1;

                    // Nose-top accent strip.
                    bool noseTop = (j >= Segs - 1 || j <= 0) && tm > 0.09f && tm < 0.18f;
                    if (noseTop) mat = 1;

                    // Canopy plate on the rear-top.
                    bool topSeg = j >= Segs - 3 || j <= 2;
                    if (topSeg && tm > g.CanopyStart && tm < g.CanopyStart + g.CanopyLen) mat = 1;

                    // Angular flank patchwork: 3 t-cells x 2 j-bands per side,
                    // cell edges skewed per j for a chevron look.
                    bool flankUpper = j >= 3 && j <= 4, flankLower = j >= 5 && j <= 6;
                    bool flankUpperM = j >= Segs - 5 && j <= Segs - 4, flankLowerM = j >= Segs - 7 && j <= Segs - 6;
                    if (flankUpper || flankLower || flankUpperM || flankLowerM)
                    {
                        float skew = (flankUpper || flankUpperM ? 0f : 0.045f) + (j % 2) * 0.02f;
                        float ft = tm - 0.12f - skew;
                        if (ft >= 0f && ft < 0.40f)
                        {
                            int cell = Mathf.Min(2, (int)(ft / 0.1334f));
                            int band = flankUpper || flankUpperM ? 0 : 1;
                            int side = flankUpper || flankLower ? 0 : 1;
                            if (flankCell[side * 6 + band * 3 + cell]) mat = 1;
                        }
                    }

                    // Belly recess under the canopy.
                    bool belly = j >= 9 && j <= 11;
                    if (belly && tm > 0.52f && tm < 0.78f) mat = 1;

                    // Rare micro panels for wear.
                    if (mat == 0 && micro[i, j] && tm > 0.15f && tm < 0.9f) mat = 1;

                    b.FaceQ(ringStart[i] + j, ringStart[i] + j2, ringStart[i + 1] + j2, ringStart[i + 1] + j, mat);
                }
            }

            // Nose point and stern cap (dark).
            var noseTip = new Vector3(0f, 0f, 0.5f * L + g.Nose);
            for (int j = 0; j < Segs; j++)
                b.TriU(noseTip, b.V[ringStart[0] + (j + 1) % Segs], b.V[ringStart[0] + j], 1);
            var sternC = new Vector3(0f, 0.03f * H, -0.5f * L - 0.05f);
            for (int j = 0; j < Segs; j++)
                b.TriU(sternC, b.V[ringStart[Rings - 1] + j], b.V[ringStart[Rings - 1] + (j + 1) % Segs], 1);

            // ---- stern piston stacks (segmented machinery) ----
            {
                int segsN = g.StackSegs;
                var path = new Vector3[segsN + 1];
                var radii = new float[segsN + 1];
                for (int i = 0; i <= segsN; i++)
                {
                    path[i] = new Vector3(0f, 0.02f * H, -0.5f * L - 0.05f - i * 0.22f);
                    radii[i] = i % 2 == 0 ? 0.27f : 0.21f;
                }
                Tube(b, path, radii, 12, 1, false);
                // gold collar rings on the even segments
                for (int i = 1; i < segsN; i += 2)
                    Tube(b, new[] { path[i] + Vector3.forward * 0.03f, path[i] - Vector3.forward * 0.03f },
                        new[] { 0.285f, 0.285f }, 12, 0, false);
                // amber glow disc at the very back
                var gc = path[segsN] + Vector3.forward * -0.02f;
                for (int k = 0; k < 12; k++)
                {
                    float a0 = k / 12f * Mathf.PI * 2f, a1 = (k + 1) / 12f * Mathf.PI * 2f;
                    b.TriUDS(gc,
                        gc + new Vector3(Mathf.Cos(a0) * 0.17f, 0.02f * H + Mathf.Sin(a0) * 0.17f, 0f),
                        gc + new Vector3(Mathf.Cos(a1) * 0.17f, 0.02f * H + Mathf.Sin(a1) * 0.17f, 0f), 2);
                }
                // smaller offset piston, like the reference's secondary cylinder
                var p2 = new Vector3[4];
                var r2 = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    p2[i] = new Vector3(0f, 0.42f * H, -0.5f * L - 0.02f - i * 0.19f);
                    r2[i] = i % 2 == 0 ? 0.13f : 0.10f;
                }
                Tube(b, p2, r2, 10, 1, true);
            }

            // ---- antennae: long straight spikes, up-back ----
            for (int side = -1; side <= 1; side += 2)
            {
                var mount = new Vector3(side * W * 0.22f, H * 0.55f, (0.5f - 0.70f) * L);
                var dir = new Vector3(side * 0.05f, g.AntRake, -0.90f).normalized;
                const int segsA = 4;
                var path = new Vector3[segsA + 1];
                var radii = new float[segsA + 1];
                for (int i = 0; i <= segsA; i++)
                {
                    float u = i / (float)segsA;
                    path[i] = mount + dir * (g.AntLen * u) + new Vector3(0f, g.AntCurve * u * u, 0f);
                    radii[i] = Mathf.Lerp(0.055f, 0.010f, u);
                }
                Tube(b, path, radii, 8, 0, true);
                Ball(b, mount, 0.09f, 1, 4, 8); // dark socket
            }

            // ---- wing blades: one big angular kite per side, black claw tip ----
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                var rootF = new Vector3(s * W * 0.85f, 0.05f * H, (0.5f - 0.50f) * L);
                var rootB = new Vector3(s * W * 0.80f, 0.00f, (0.5f - 0.78f) * L);
                var tipB = new Vector3(s * (W * 0.85f + g.WingSpan), -0.35f * H, (0.5f - 0.78f) * L - g.WingSweep);
                var claw = new Vector3(s * (W * 0.85f + g.WingSpan * 1.12f), -0.42f * H, (0.5f - 0.66f) * L - g.WingSweep * 0.75f);
                var tipF = new Vector3(s * (W * 0.85f + g.WingSpan * 0.72f), -0.28f * H, (0.5f - 0.55f) * L - g.WingSweep * 0.35f);
                var th = new Vector3(0f, 0.05f, 0f);

                // top and bottom skins (fan from rootF)
                b.TriUDS(rootF + th, rootB + th, tipB + th, 0);
                b.TriUDS(rootF + th, tipB + th, claw + th, 0);
                b.TriUDS(rootF + th, claw + th, tipF + th, 0);
                b.TriUDS(rootF - th, rootB - th, tipB - th, 0);
                b.TriUDS(rootF - th, tipB - th, claw - th, 0);
                b.TriUDS(rootF - th, claw - th, tipF - th, 0);
                // black edge rims
                b.QuadUDS(rootF + th, rootF - th, rootB - th, rootB + th, 1);
                b.QuadUDS(rootB + th, rootB - th, tipB - th, tipB + th, 1);
                b.QuadUDS(tipB + th, tipB - th, claw - th, claw + th, 1);
                b.QuadUDS(claw + th, claw - th, tipF - th, tipF + th, 1);
                b.QuadUDS(tipF + th, tipF - th, rootF - th, rootF + th, 1);
                // black claw spike off the outer point
                var clawDir = new Vector3(s * 0.55f, -0.35f, -0.75f).normalized;
                Tube(b, new[] { claw, claw + clawDir * 0.55f }, new[] { 0.06f, 0.008f }, 5, 1, true);
            }

            // ---- legs: two per side, folding down-forward, black claws ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int leg = 0; leg < 2; leg++)
                {
                    int li = (side < 0 ? 0 : 2) + leg;
                    float tm = leg == 0 ? 0.78f : 0.92f;
                    var mount = new Vector3(side * W * 0.45f, -0.50f * H, (0.5f - tm) * L);
                    // coxa: short dark link down-out-back
                    var d1 = new Vector3(side * (0.55f + g.LegA[li]), -0.55f, -0.35f).normalized;
                    float len1 = 0.55f * g.LegScale;
                    var j1 = mount + d1 * len1;
                    Tube(b, new[] { mount, j1 }, new[] { 0.09f, 0.075f }, 8, 1, false);
                    Ball(b, j1, 0.115f, 1, 4, 8);
                    // femur: long angular gold blade, down-forward
                    var d2 = new Vector3(side * 0.18f, -0.72f, 0.42f + g.LegA[li]).normalized;
                    float len2 = 1.7f * g.LegL[li] * g.LegScale;
                    var j2 = j1 + d2 * len2;
                    Tube(b, new[] { j1, j1 + d2 * (len2 * 0.5f), j2 }, new[] { 0.13f, 0.11f, 0.07f }, 4, 0, false);
                    Ball(b, j2, 0.09f, 1, 4, 8);
                    // tarsus: black claw, curving slightly back-down
                    var d3 = new Vector3(side * 0.05f, -0.80f, -0.25f).normalized;
                    float len3 = 0.9f * g.LegL[li] * g.LegScale;
                    Tube(b, new[] { j2, j2 + d3 * (len3 * 0.55f), j2 + d3 * len3 + new Vector3(0f, -0.05f, 0.18f) },
                        new[] { 0.06f, 0.045f, 0.006f }, 5, 1, true);
                }
            }

            // ---- ventral fin ----
            {
                float z0 = (0.5f - 0.72f) * L;
                var a = new Vector3(0f, -H * 0.68f, z0);
                var bb = new Vector3(0f, -H * 1.35f, z0 - 0.55f);
                var c = new Vector3(0f, -H * 0.62f, z0 - 0.45f);
                b.TriUDS(a, bb, c, 0);
                b.TriUDS(a + new Vector3(0, 0, -0.04f), bb + new Vector3(0, 0, -0.04f), c + new Vector3(0, 0, -0.04f), 1);
            }

            // ---- dorsal turret drum + barrel (the hardpoint) ----
            {
                float sc = CrSample(cts, csc, 0.40f);
                float lift = CrSample(cts, clf, 0.40f) * H;
                var dc = new Vector3(0f, H * sc * 0.98f + lift, (0.5f - 0.40f) * L);
                Tube(b, new[] { dc, dc + Vector3.up * 0.09f }, new[] { 0.13f, 0.115f }, 10, 1, true);
                Tube(b, new[] { dc + new Vector3(0f, 0.055f, 0.06f), dc + new Vector3(0f, 0.055f, 0.62f) },
                    new[] { 0.035f, 0.028f }, 6, 1, true);
            }

            // ---- belly web-emitter ring (the web slot, teal) ----
            {
                var wc = new Vector3(0f, -H * 0.70f, (0.5f - 0.62f) * L);
                for (int k = 0; k < 10; k++)
                {
                    float a0 = k / 10f * Mathf.PI * 2f, a1 = (k + 1) / 10f * Mathf.PI * 2f;
                    const float r0 = 0.10f, r1 = 0.165f;
                    b.QuadUDS(
                        wc + new Vector3(Mathf.Cos(a0) * r0, 0f, Mathf.Sin(a0) * r0),
                        wc + new Vector3(Mathf.Cos(a0) * r1, 0f, Mathf.Sin(a0) * r1),
                        wc + new Vector3(Mathf.Cos(a1) * r1, 0f, Mathf.Sin(a1) * r1),
                        wc + new Vector3(Mathf.Cos(a1) * r0, 0f, Mathf.Sin(a1) * r0), 3);
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
            m.SetFloat("_Glossiness", smooth);
            m.SetFloat("_Smoothness", smooth);
            return m;
        }
    }
}
