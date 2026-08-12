using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Generates the Hive body mesh from a 10-digit hash — Mk.IV.
    ///
    /// The hull is no longer a tapered oval: the cross-section is a creased
    /// blade that MORPHS along the length, blending three hand-authored
    /// sections — a knife-edged flat blade at the nose, a faceted deck-dome
    /// with a hard side chine amidships, and a shouldered block at the stern.
    /// It is built as 8 longitudinal strips with duplicated vertices at the
    /// center ridge, deck edge, chine, and keel edge, so surfaces shade
    /// smooth while the crease lines stay razor sharp. The chine band paints
    /// black (the reference's long dark side stripe) and a raised canopy dome
    /// sits on the deck.
    ///
    /// Wings are detailed assemblies: root pylon, stepped 7-point outline
    /// with a trailing notch, raised ridge plates, black rims, and a jointed
    /// two-segment claw finger. Stern keeps the segmented piston stack; legs,
    /// antennae, turret drum (hardpoint) and teal web ring (web slot) carry
    /// over, denser. ~6,000 triangles per body.
    ///
    /// The genome roll is IDENTICAL to Mk.III (same fields, same order), so a
    /// hash keeps its stats and proportions; only geometry interpretation
    /// changed. Submeshes: 0 gold, 1 black, 2 amber glow, 3 teal glow.
    /// </summary>
    public static class HiveShipMesh
    {
        const int Rings = 52;
        const int HalfPts = 13;          // p0 ridge .. p12 keel, per side
        const int LoopPts = 24;          // 13 + 11 mirrored
        const int Spans = 24;            // paintable quads per ring pair
        static readonly int[] StripStart = { 0, 3, 6, 9, 12, 15, 18, 21 };

        // Half-profiles (x, y) normalized; creases at p0, p3, p6, p9, p12.
        // p3 = deck edge, p6 = chine (max width), p9 = keel edge.
        static readonly float[,] NoseP =
        {
            {0.00f, 0.22f}, {0.35f, 0.20f}, {0.68f, 0.16f}, {0.88f, 0.10f},
            {0.96f, 0.05f}, {1.00f, 0.01f}, {1.00f, -0.02f},
            {0.82f, -0.08f}, {0.55f, -0.12f}, {0.30f, -0.14f},
            {0.18f, -0.15f}, {0.08f, -0.16f}, {0.00f, -0.16f},
        };
        static readonly float[,] MidP =
        {
            {0.00f, 0.95f}, {0.30f, 0.88f}, {0.55f, 0.72f}, {0.72f, 0.52f},
            {0.88f, 0.30f}, {0.98f, 0.08f}, {1.00f, -0.12f},
            {0.85f, -0.32f}, {0.65f, -0.45f}, {0.42f, -0.52f},
            {0.28f, -0.55f}, {0.12f, -0.58f}, {0.00f, -0.58f},
        };
        static readonly float[,] SternP =
        {
            {0.00f, 0.80f}, {0.38f, 0.78f}, {0.62f, 0.70f}, {0.80f, 0.55f},
            {0.92f, 0.32f}, {1.00f, 0.05f}, {0.98f, -0.18f},
            {0.82f, -0.38f}, {0.60f, -0.50f}, {0.40f, -0.55f},
            {0.26f, -0.57f}, {0.10f, -0.60f}, {0.00f, -0.60f},
        };

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

        // UNCHANGED since Mk.III — a hash keeps its proportions.
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

        static float Smooth01(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        // Blended half-profile point k at hull fraction t.
        static Vector2 HalfPt(int k, float t)
        {
            float wMid = Smooth01(t / 0.42f);
            float wStern = Smooth01((t - 0.66f) / 0.34f);
            float x = Mathf.Lerp(NoseP[k, 0], MidP[k, 0], wMid);
            float y = Mathf.Lerp(NoseP[k, 1], MidP[k, 1], wMid);
            x = Mathf.Lerp(x, SternP[k, 0], wStern);
            y = Mathf.Lerp(y, SternP[k, 1], wStern);
            return new Vector2(x, y);
        }

        // Loop point li (0..23) clockwise from the top ridge.
        static Vector2 LoopPt(int li, float t)
        {
            if (li <= 12) return HalfPt(li, t);
            var p = HalfPt(LoopPts - li, t);
            return new Vector2(-p.x, p.y);
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

            // Panel stream: 12 flank cells then micro panels per span.
            var panelRng = Rng.Stream("hivepanels:" + hash);
            var flankCell = new bool[12];
            for (int i = 0; i < 12; i++) flankCell[i] = panelRng.NextDouble() < g.FlankOdds;
            var micro = new bool[Rings - 1, Spans];
            for (int i = 0; i < Rings - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.03;

            // Plan-form taper and dorsal lift.
            float[] cts = { 0.00f, 0.08f, 0.20f, 0.35f, 0.50f, 0.72f, 0.88f, 1.00f };
            float[] csc = { 0.06f, 0.22f, 0.40f, 0.60f, 0.82f, 1.00f, 0.94f, 0.72f };
            float[] clf = { 0.00f, 0.01f, 0.02f, 0.04f, 0.07f, 0.10f, 0.06f, 0.00f };

            // ---- hull: 8 crease-separated strips over morphing sections ----
            // stripVerts[s][ring][pt 0..3]
            var stripVerts = new int[8][][];
            var ringT = new float[Rings];
            for (int s = 0; s < 8; s++) stripVerts[s] = new int[Rings][];
            for (int i = 0; i < Rings; i++)
            {
                float t = i / (float)(Rings - 1);
                ringT[i] = t;
                float sc = CrSample(cts, csc, t);
                sc *= 1f + g.SurfAmp * 0.012f * Mathf.Sin((t * 6f + g.SurfPhase) * Mathf.PI * 2f);
                float lift = CrSample(cts, clf, t) * H;
                float z = (0.5f - t) * L;
                for (int s = 0; s < 8; s++)
                {
                    stripVerts[s][i] = new int[4];
                    for (int p = 0; p < 4; p++)
                    {
                        int li = (StripStart[s] + p) % LoopPts;
                        var pt = LoopPt(li, t);
                        stripVerts[s][i][p] = b.Add(new Vector3(pt.x * W * sc, pt.y * H * sc + lift, z));
                    }
                }
            }

            for (int i = 0; i < Rings - 1; i++)
            {
                float tm = (ringT[i] + ringT[i + 1]) * 0.5f;
                for (int s = 0; s < 8; s++)
                {
                    for (int p = 0; p < 3; p++)
                    {
                        int span = s * 3 + p;
                        int mat = PaintMat(g, s, p, tm, flankCell, micro[i, span]);
                        b.FaceQ(stripVerts[s][i][p], stripVerts[s][i][p + 1],
                            stripVerts[s][i + 1][p + 1], stripVerts[s][i + 1][p], mat);
                    }
                }
            }

            // Nose point and stern cap (dark, crisp).
            var noseTip = new Vector3(0f, 0.01f * H, 0.5f * L + g.Nose);
            for (int s = 0; s < 8; s++)
                for (int p = 0; p < 3; p++)
                    b.TriU(noseTip, b.V[stripVerts[s][0][p + 1]], b.V[stripVerts[s][0][p]], 1);
            var sternC = new Vector3(0f, 0.02f * H, -0.5f * L - 0.05f);
            for (int s = 0; s < 8; s++)
                for (int p = 0; p < 3; p++)
                    b.TriU(sternC, b.V[stripVerts[s][Rings - 1][p]], b.V[stripVerts[s][Rings - 1][p + 1]], 1);

            // Canopy dome on the deck (dark glass blister).
            {
                float tCan = g.CanopyStart + g.CanopyLen * 0.5f;
                float sc = CrSample(cts, csc, tCan);
                float lift = CrSample(cts, clf, tCan) * H;
                float deckY = HalfPt(0, tCan).y * H * sc + lift;
                var c = new Vector3(0f, deckY - 0.02f, (0.5f - tCan) * L);
                const int lat = 3, lon = 10;
                var rows = new Vector3[lat + 1][];
                for (int i = 0; i <= lat; i++)
                {
                    rows[i] = new Vector3[lon];
                    float phi = i / (float)lat * (Mathf.PI * 0.5f);
                    for (int k = 0; k < lon; k++)
                    {
                        float th = k / (float)lon * Mathf.PI * 2f;
                        rows[i][k] = c + new Vector3(
                            Mathf.Sin(phi) * Mathf.Cos(th) * 0.30f,
                            Mathf.Cos(phi) * 0.16f,
                            Mathf.Sin(phi) * Mathf.Sin(th) * (g.CanopyLen * L * 0.42f));
                    }
                }
                for (int i = 0; i < lat; i++)
                    for (int k = 0; k < lon; k++)
                        b.QuadUDS(rows[i][k], rows[i][(k + 1) % lon], rows[i + 1][(k + 1) % lon], rows[i + 1][k], 1);
            }

            // ---- stern piston stacks ----
            {
                int segsN = g.StackSegs;
                var path = new Vector3[segsN + 1];
                var radii = new float[segsN + 1];
                for (int i = 0; i <= segsN; i++)
                {
                    path[i] = new Vector3(0f, 0.02f * H, -0.5f * L - 0.05f - i * 0.22f);
                    radii[i] = i % 2 == 0 ? 0.27f : 0.21f;
                }
                Tube(b, path, radii, 16, 1, false);
                for (int i = 1; i < segsN; i += 2)
                    Tube(b, new[] { path[i] + Vector3.forward * 0.03f, path[i] - Vector3.forward * 0.03f },
                        new[] { 0.285f, 0.285f }, 16, 0, false);
                var gc = path[segsN] + Vector3.forward * -0.02f;
                for (int k = 0; k < 16; k++)
                {
                    float a0 = k / 16f * Mathf.PI * 2f, a1 = (k + 1) / 16f * Mathf.PI * 2f;
                    b.TriUDS(gc,
                        gc + new Vector3(Mathf.Cos(a0) * 0.17f, Mathf.Sin(a0) * 0.17f, 0f),
                        gc + new Vector3(Mathf.Cos(a1) * 0.17f, Mathf.Sin(a1) * 0.17f, 0f), 2);
                }
                var p2 = new Vector3[4];
                var r2 = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    p2[i] = new Vector3(0f, 0.42f * H, -0.5f * L - 0.02f - i * 0.19f);
                    r2[i] = i % 2 == 0 ? 0.13f : 0.10f;
                }
                Tube(b, p2, r2, 10, 1, true);
            }

            // ---- antennae with collar detail ----
            for (int side = -1; side <= 1; side += 2)
            {
                var mount = new Vector3(side * W * 0.22f, H * 0.55f, (0.5f - 0.70f) * L);
                var dir = new Vector3(side * 0.05f, g.AntRake, -0.90f).normalized;
                const int segsA = 6;
                var path = new Vector3[segsA + 1];
                var radii = new float[segsA + 1];
                for (int i = 0; i <= segsA; i++)
                {
                    float u = i / (float)segsA;
                    path[i] = mount + dir * (g.AntLen * u) + new Vector3(0f, g.AntCurve * u * u, 0f);
                    radii[i] = Mathf.Lerp(0.055f, 0.010f, u);
                }
                Tube(b, path, radii, 8, 0, true);
                var collar = mount + dir * (g.AntLen * 0.33f);
                Tube(b, new[] { collar - dir * 0.05f, collar + dir * 0.05f }, new[] { 0.055f, 0.055f }, 8, 1, false);
                Ball(b, mount, 0.09f, 1, 4, 8);
            }

            // ---- wings: pylon, stepped blade, ridge plates, claw finger ----
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                var hullPt = new Vector3(s * W * 0.70f, 0.02f * H, (0.5f - 0.60f) * L);
                var rootMid = new Vector3(s * W * 1.02f, -0.02f * H, (0.5f - 0.63f) * L);
                Tube(b, new[] { hullPt, rootMid }, new[] { 0.16f, 0.12f }, 4, 0, false);

                var rootF = new Vector3(s * W * 1.00f, 0.02f * H, (0.5f - 0.52f) * L);
                var rootB = new Vector3(s * W * 0.95f, -0.05f * H, (0.5f - 0.80f) * L);
                var notch = new Vector3(s * (W + g.WingSpan * 0.45f), -0.18f * H, (0.5f - 0.83f) * L - g.WingSweep * 0.55f);
                var tipB = new Vector3(s * (W + g.WingSpan), -0.33f * H, (0.5f - 0.80f) * L - g.WingSweep);
                var clawB = new Vector3(s * (W + g.WingSpan * 1.10f), -0.40f * H, (0.5f - 0.70f) * L - g.WingSweep * 0.80f);
                var clawF = new Vector3(s * (W + g.WingSpan * 0.95f), -0.36f * H, (0.5f - 0.60f) * L - g.WingSweep * 0.55f);
                var midF = new Vector3(s * (W + g.WingSpan * 0.50f), -0.18f * H, (0.5f - 0.52f) * L - g.WingSweep * 0.20f);
                var th = new Vector3(0f, 0.055f, 0f);

                var rim = new[] { rootF, rootB, notch, tipB, clawB, clawF, midF };
                // skins: fan from rootF, both faces
                for (int k = 1; k < rim.Length - 1; k++)
                {
                    b.TriUDS(rim[0] + th, rim[k] + th, rim[k + 1] + th, 0);
                    b.TriUDS(rim[0] - th, rim[k] - th, rim[k + 1] - th, 0);
                }
                // black edge rims
                for (int k = 0; k < rim.Length; k++)
                {
                    var e0 = rim[k];
                    var e1 = rim[(k + 1) % rim.Length];
                    b.QuadUDS(e0 + th, e0 - th, e1 - th, e1 + th, 1);
                }
                // raised ridge plates (panel-line detail on the top skin)
                for (int plate = 0; plate < 2; plate++)
                {
                    float f0 = plate == 0 ? 0.22f : 0.55f;
                    float f1 = f0 + 0.22f;
                    var up2 = new Vector3(0f, 0.085f, 0f);
                    var a1 = Vector3.LerpUnclamped(rootF, clawF, f0) + up2;
                    var a2 = Vector3.LerpUnclamped(rootF, clawF, f1) + up2;
                    var b2 = Vector3.LerpUnclamped(rootB, tipB, f1) + up2;
                    var b1 = Vector3.LerpUnclamped(rootB, tipB, f0) + up2;
                    b.QuadUDS(a1, a2, b2, b1, plate == 0 ? 1 : 0);
                }
                // claw finger: gold segment, joint ball, black hook
                var clawMid = (clawB + clawF) * 0.5f;
                var d1 = new Vector3(s * 0.50f, -0.30f, -0.70f).normalized;
                var joint = clawMid + d1 * 0.45f;
                Tube(b, new[] { clawMid, joint }, new[] { 0.07f, 0.05f }, 4, 0, false);
                Ball(b, joint, 0.07f, 1, 3, 6);
                Tube(b, new[] { joint, joint + d1 * 0.5f + new Vector3(0f, -0.12f, 0.1f) },
                    new[] { 0.045f, 0.006f }, 4, 1, true);
            }

            // ---- legs: two per side ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int leg = 0; leg < 2; leg++)
                {
                    int li = (side < 0 ? 0 : 2) + leg;
                    float tm = leg == 0 ? 0.78f : 0.92f;
                    var mount = new Vector3(side * W * 0.42f, -0.50f * H, (0.5f - tm) * L);
                    var d1 = new Vector3(side * (0.55f + g.LegA[li]), -0.55f, -0.35f).normalized;
                    float len1 = 0.55f * g.LegScale;
                    var j1 = mount + d1 * len1;
                    Tube(b, new[] { mount, j1 }, new[] { 0.09f, 0.075f }, 8, 1, false);
                    Ball(b, j1, 0.115f, 1, 4, 8);
                    var d2 = new Vector3(side * 0.18f, -0.72f, 0.42f + g.LegA[li]).normalized;
                    float len2 = 1.7f * g.LegL[li] * g.LegScale;
                    var j2 = j1 + d2 * len2;
                    Tube(b, new[] { j1, j1 + d2 * (len2 * 0.5f), j2 }, new[] { 0.13f, 0.11f, 0.07f }, 4, 0, false);
                    Ball(b, j2, 0.09f, 1, 4, 8);
                    var d3 = new Vector3(side * 0.05f, -0.80f, -0.25f).normalized;
                    float len3 = 0.9f * g.LegL[li] * g.LegScale;
                    Tube(b, new[] { j2, j2 + d3 * (len3 * 0.55f), j2 + d3 * len3 + new Vector3(0f, -0.05f, 0.18f) },
                        new[] { 0.06f, 0.045f, 0.006f }, 5, 1, true);
                }
            }

            // ---- ventral fin ----
            {
                float z0 = (0.5f - 0.72f) * L;
                var a = new Vector3(0f, -H * 0.62f, z0);
                var bb = new Vector3(0f, -H * 1.30f, z0 - 0.55f);
                var c = new Vector3(0f, -H * 0.58f, z0 - 0.45f);
                b.TriUDS(a, bb, c, 0);
                b.TriUDS(a + new Vector3(0, 0, -0.04f), bb + new Vector3(0, 0, -0.04f), c + new Vector3(0, 0, -0.04f), 1);
            }

            // ---- dorsal turret drum + barrel (the hardpoint) ----
            {
                float sc = CrSample(cts, csc, 0.40f);
                float lift = CrSample(cts, clf, 0.40f) * H;
                float deckY = HalfPt(0, 0.40f).y * H * sc + lift;
                var dc = new Vector3(0f, deckY - 0.01f, (0.5f - 0.40f) * L);
                Tube(b, new[] { dc, dc + Vector3.up * 0.09f }, new[] { 0.13f, 0.115f }, 12, 1, true);
                Tube(b, new[] { dc + new Vector3(0f, 0.055f, 0.06f), dc + new Vector3(0f, 0.055f, 0.62f) },
                    new[] { 0.035f, 0.028f }, 8, 1, true);
            }

            // ---- belly web-emitter ring (the web slot, teal) ----
            {
                float keelY = HalfPt(12, 0.62f).y * H * CrSample(cts, csc, 0.62f);
                var wc = new Vector3(0f, keelY - 0.02f, (0.5f - 0.62f) * L);
                for (int k = 0; k < 12; k++)
                {
                    float a0 = k / 12f * Mathf.PI * 2f, a1 = (k + 1) / 12f * Mathf.PI * 2f;
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

        // Paint per strip/span. Strips: 0 R-deck, 1 R-upper, 2 R-lower,
        // 3 R-belly, 4 L-belly, 5 L-lower, 6 L-upper, 7 L-deck.
        static int PaintMat(Genome g, int s, int p, float tm, bool[] flankCell, bool microHit)
        {
            // Dark chisel nose.
            if (tm < 0.06f) return 1;

            bool deck = s == 0 || s == 7;
            bool upper = s == 1 || s == 6;
            bool lower = s == 2 || s == 5;
            bool belly = s == 3 || s == 4;
            int side = s <= 3 ? 0 : 1;

            // Nose-top accent.
            if (deck && tm > 0.09f && tm < 0.18f) return 1;

            // Canopy deck paint around the dome.
            if (deck && tm > g.CanopyStart && tm < g.CanopyStart + g.CanopyLen) return 1;

            // Chine stripe: the spans hugging the crease run dark.
            bool chineSpan = (s == 1 && p == 2) || (s == 2 && p == 0)
                || (s == 6 && p == 0) || (s == 5 && p == 2);
            if (chineSpan && tm > 0.10f && tm < 0.52f) return 1;

            // Flank patchwork.
            if (upper || lower)
            {
                float skew = (upper ? 0f : 0.045f) + p * 0.018f;
                float ft = tm - 0.12f - skew;
                if (ft >= 0f && ft < 0.40f)
                {
                    int cell = Mathf.Min(2, (int)(ft / 0.1334f));
                    int band = upper ? 0 : 1;
                    if (flankCell[side * 6 + band * 3 + cell]) return 1;
                }
            }

            // Belly recess.
            if (belly && tm > 0.52f && tm < 0.78f) return 1;

            // Rare micro panels.
            if (microHit && tm > 0.15f && tm < 0.9f) return 1;
            return 0;
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
