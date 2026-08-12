using System.Collections.Generic;
using UnityEngine;
using static SpaceGame.MeshKit;

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
        const int Rings = 104;
        const int HalfPts = 13;          // p0 ridge .. p12 keel, per side
        const int LoopPts = 24;          // 13 + 11 mirrored
        const int Spans = 24;            // paintable quads per ring pair

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
            {0.00f, 0.92f}, {0.42f, 0.90f}, {0.66f, 0.74f}, {0.84f, 0.48f},
            {0.96f, 0.18f}, {1.00f, -0.06f}, {0.97f, -0.22f},
            {0.82f, -0.36f}, {0.62f, -0.46f}, {0.40f, -0.52f},
            {0.26f, -0.55f}, {0.12f, -0.58f}, {0.00f, -0.58f},
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
            public float Splay, BandPhase;
            public int BandMode, BandCount;
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
            // Mk.IV.1 additions, appended so earlier rolls keep their values.
            g.Splay = R(0.00f, 0.18f);
            g.BandMode = rng.Next(4);   // 0 none, 1 tail rings, 2 nose rings, 3 deck stripe
            g.BandCount = 2 + rng.Next(3);
            g.BandPhase = R(0f, 1f);
            g.Gold = Color.HSVToRGB(g.Hue, g.Sat, g.Val);
            return g;
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

        static Vector2 HalfPtF(float k, float t)
        {
            float wMid = Smooth01(t / 0.42f);
            float wStern = Smooth01((t - 0.66f) / 0.34f);
            var v = Vector2.Lerp(ProfCR(NoseP, k), ProfCR(MidP, k), wMid);
            return Vector2.Lerp(v, ProfCR(SternP, k), wStern);
        }

        // ---- volumetric wings ----
        // Sections along the span are airfoil-like: rounded spar peaking at
        // a third of chord, cambered skins, thin edges. Leading/trailing
        // edges follow Catmull-Rom chains so the plan-form silhouette stays
        // identical to the flat wings these replaced.




        // Point on the top skin (chord fraction c, span fraction u), raised.

        // Raised armor plate that follows the cambered top skin.




        public static GameObject Build(string hash, int cls, Transform shipRoot)
            => cls == 3 ? BuildC3(hash, shipRoot)
             : cls == 2 ? BuildC2(hash, shipRoot) : BuildC1(hash, shipRoot);

        static GameObject BuildC1(string hash, Transform shipRoot)
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

            // ---- hull: 48-pt smoothed loft over morphing sections ----
            var stripVerts = HullLoft48(b, Rings, HalfPtF,
                t => CrSample(cts, csc, t)
                    * (1f + g.SurfAmp * 0.012f * Mathf.Sin((t * 6f + g.SurfPhase) * Mathf.PI * 2f)),
                t => CrSample(cts, clf, t) * H, t => (0.5f - t) * L, t => 1f, t => 1f, W, H,
                (so, po, tm, i) => PaintMat(g, so, po, tm, flankCell, micro[i, so * 3 + po]));

            // Nose point and stern cap (dark, crisp).
            var noseTip = new Vector3(0f, 0.01f * H, 0.5f * L + g.Nose);
            CapFan(b, noseTip, stripVerts, 0, true, 1);
            var sternC = new Vector3(0f, 0.02f * H, -0.5f * L - 0.05f);
            CapFan(b, sternC, stripVerts, Rings - 1, false, 1);

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
                            Mathf.Sin(phi) * Mathf.Cos(th) * 0.26f,
                            Mathf.Cos(phi) * 0.10f,
                            Mathf.Sin(phi) * Mathf.Sin(th) * (g.CanopyLen * L * 0.48f));
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
                Nozzle(b, gc, Vector3.back, 0.17f * 1.55f, 0.17f * 1.30f, 16, 1, 2);
                var p2 = new Vector3[4];
                var r2 = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    p2[i] = new Vector3(0f, 0.42f * H, -0.5f * L - 0.02f - i * 0.19f);
                    r2[i] = i % 2 == 0 ? 0.13f : 0.10f;
                }
                Tube(b, p2, r2, 10, 1, true);

                // twin banded drive drums high on the rear flanks
                for (int side = -1; side <= 1; side += 2)
                {
                    var ec = new Vector3(side * W * 0.52f, 0.30f * H, -0.5f * L + 0.28f);
                    var pd = new Vector3[5];
                    var rd = new float[5];
                    for (int i = 0; i < 5; i++)
                    {
                        pd[i] = ec + Vector3.forward * (-i * 0.20f);
                        rd[i] = i % 2 == 0 ? 0.20f : 0.16f;
                    }
                    Tube(b, pd, rd, 12, 1, false);
                    for (int i = 1; i <= 3; i += 2)
                        Tube(b, new[] { pd[i] + Vector3.forward * 0.03f, pd[i] - Vector3.forward * 0.03f },
                            new[] { 0.21f, 0.21f }, 12, 0, false);
                    Nozzle(b, pd[4] + Vector3.forward * -0.02f, Vector3.back, 0.20f, 0.24f, 12, 1, 2);
                }
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
                var rootB = new Vector3(s * W * 0.95f, -0.08f * H, (0.5f - 0.80f) * L);
                var notch = new Vector3(s * (W + g.WingSpan * 0.45f), -0.30f * H, (0.5f - 0.83f) * L - g.WingSweep * 0.55f);
                var tipB = new Vector3(s * (W + g.WingSpan), -0.52f * H, (0.5f - 0.80f) * L - g.WingSweep);
                var clawB = new Vector3(s * (W + g.WingSpan * 1.10f), -0.62f * H, (0.5f - 0.70f) * L - g.WingSweep * 0.80f);
                var clawF = new Vector3(s * (W + g.WingSpan * 0.95f), -0.56f * H, (0.5f - 0.60f) * L - g.WingSweep * 0.55f);
                var midF = new Vector3(s * (W + g.WingSpan * 0.50f), -0.28f * H, (0.5f - 0.52f) * L - g.WingSweep * 0.20f);

                // volumetric loft over the same plan-form outline
                var lead = new[] { rootF, midF, clawF };
                var leadT = new[] { 0f, 0.55f, 1f };
                var trail = new[] { rootB, notch, tipB, clawB };
                var trailT = new[] { 0f, 0.45f, 0.80f, 1f };
                LoftWing(b, lead, leadT, trail, trailT, 0.105f, 0.030f, 9, side < 0);
                WingPlate(b, lead, leadT, trail, trailT, 0.105f, 0.030f, 0.20f, 0.42f, 1);
                WingPlate(b, lead, leadT, trail, trailT, 0.105f, 0.030f, 0.53f, 0.75f, 0);
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
                    var mount = new Vector3(side * W * 0.50f, -0.48f * H, (0.5f - tm) * L);
                    var d1 = new Vector3(side * (0.90f + g.Splay + g.LegA[li]), -0.40f, -0.30f).normalized;
                    float len1 = 0.60f * g.LegScale;
                    var j1 = mount + d1 * len1;
                    Fairing(b, mount - d1 * 0.02f, d1, 0.105f, 0.19f, 0.09f, 8, 1);
                    Tube(b, new[] { mount, j1 }, new[] { 0.09f, 0.075f }, 8, 1, false);
                    Ball(b, j1, 0.115f, 0, 4, 8);
                    var d2 = new Vector3(side * (0.48f + g.Splay), -0.60f, 0.40f + g.LegA[li]).normalized;
                    float len2 = 1.7f * g.LegL[li] * g.LegScale;
                    var j2 = j1 + d2 * len2;
                    Tube(b, new[] { j1, j1 + d2 * (len2 * 0.5f), j2 }, new[] { 0.15f, 0.12f, 0.08f }, 4, 0, false);
                    Ball(b, j2, 0.10f, 0, 4, 8);
                    var d3 = new Vector3(side * (0.22f + g.Splay * 0.5f), -0.75f, -0.22f).normalized;
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

            // Big black dorsal saddle wrapping the canopy and running aft,
            // with a raked edge that steps down the upper flank.
            float sadF = g.CanopyStart - 0.10f, sadB = g.CanopyStart + g.CanopyLen + 0.16f;
            if (deck && tm > sadF && tm < sadB) return 1;
            if (upper && p <= 1 && tm > sadF + 0.05f + p * 0.04f && tm < sadB - 0.05f - p * 0.04f) return 1;

            // Slim dark chine slash along the forward blade.
            bool chineSpan = (s == 1 && p == 2) || (s == 2 && p == 0)
                || (s == 6 && p == 0) || (s == 5 && p == 2);
            if (chineSpan && tm > 0.10f && tm < 0.55f) return 1;

            // Angular dark inlays on the fore flanks.
            if (upper || lower)
            {
                float skew = (upper ? 0f : 0.05f) + p * 0.02f;
                float ft = tm - 0.10f - skew;
                if (ft >= 0f && ft < 0.30f)
                {
                    int cell = Mathf.Min(2, (int)(ft / 0.1001f));
                    int band = upper ? 0 : 1;
                    if (flankCell[side * 6 + band * 3 + cell]) return 1;
                }
            }

            // Dark belly recess under the rear machinery.
            if (belly && tm > 0.55f && tm < 0.85f) return 1;

            // A single subtle stern ring is all that survives of the old
            // banding families — the reference is paneled, not striped.
            if (g.BandMode == 1 && tm > 0.86f && tm < 0.91f) return 1;

            // Rare micro panels.
            if (microHit && tm > 0.12f && tm < 0.9f) return 1;
            return 0;
        }


        // ================= CLASS 2 — "Striker" =================
        // Twin-cannon fighter: needle nose, amber glass canopy, black spine
        // armor, four swept blade wings (raked upper pair, dropped lower
        // pair), twin segmented engine drums. No legs, no antennae — the
        // cannons carry the silhouette. Fresh streams: hive2body/hive2panels.

        class GenomeC2
        {
            public float L, W, H, Nose, CanopyStart, CanopyLen;
            public float CannonLen, CannonSpread;
            public float WingUpSpan, WingUpRake, WingUpSweep;
            public float WingLowSpan, WingLowDrop, WingLowSweep;
            public float FlankOdds, Hue, Sat, Val, SurfAmp, SurfPhase;
            public int EngineSegs, BandMode, BandCount;
            public float BandPhase, FinSize;
            public Color Gold;
        }

        static GenomeC2 RollC2(string hash)
        {
            var rng = Rng.Stream("hive2body:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new GenomeC2();
            g.L = R(7.2f, 7.8f);
            g.W = R(0.85f, 1.00f);
            g.H = R(0.62f, 0.72f);
            g.Nose = R(0.50f, 0.80f);
            g.CanopyStart = R(0.16f, 0.20f);
            g.CanopyLen = R(0.15f, 0.19f);
            g.CannonLen = R(2.3f, 2.9f);
            g.CannonSpread = R(0.26f, 0.36f);
            g.WingUpSpan = R(2.6f, 3.2f);
            g.WingUpRake = R(0.55f, 0.75f);
            g.WingUpSweep = R(1.6f, 2.0f);
            g.WingLowSpan = R(1.8f, 2.3f);
            g.WingLowDrop = R(0.35f, 0.50f);
            g.WingLowSweep = R(1.2f, 1.6f);
            g.FlankOdds = R(0.45f, 0.70f);
            g.Hue = R(0.100f, 0.125f);
            g.Sat = R(0.82f, 0.92f);
            g.Val = R(0.78f, 0.90f);
            g.SurfAmp = R(0f, 0.5f);
            g.SurfPhase = R(0f, 1f);
            g.EngineSegs = 3 + rng.Next(2);
            g.BandMode = rng.Next(4);
            g.BandCount = 2 + rng.Next(3);
            g.BandPhase = R(0f, 1f);
            g.FinSize = R(0.30f, 0.50f);
            g.Gold = Color.HSVToRGB(g.Hue, g.Sat, g.Val);
            return g;
        }

        // C2 sections: needle nose, tall cockpit fuselage, engine bulkhead.
        static readonly float[,] NoseP2 =
        {
            {0.00f, 0.30f}, {0.30f, 0.28f}, {0.55f, 0.24f}, {0.75f, 0.16f},
            {0.88f, 0.06f}, {0.95f, -0.02f}, {0.92f, -0.10f},
            {0.78f, -0.18f}, {0.55f, -0.24f}, {0.32f, -0.27f},
            {0.20f, -0.28f}, {0.10f, -0.29f}, {0.00f, -0.30f},
        };
        static readonly float[,] MidP2 =
        {
            {0.00f, 1.00f}, {0.28f, 0.96f}, {0.52f, 0.84f}, {0.70f, 0.62f},
            {0.85f, 0.34f}, {0.96f, 0.06f}, {1.00f, -0.22f},
            {0.88f, -0.44f}, {0.68f, -0.58f}, {0.45f, -0.66f},
            {0.28f, -0.69f}, {0.12f, -0.72f}, {0.00f, -0.72f},
        };
        static readonly float[,] SternP2 =
        {
            {0.00f, 0.85f}, {0.40f, 0.83f}, {0.66f, 0.76f}, {0.84f, 0.60f},
            {0.95f, 0.36f}, {1.00f, 0.06f}, {0.98f, -0.26f},
            {0.86f, -0.48f}, {0.66f, -0.62f}, {0.44f, -0.68f},
            {0.28f, -0.70f}, {0.10f, -0.72f}, {0.00f, -0.72f},
        };

        static Vector2 HalfPt2(int k, float t)
        {
            float wMid = Smooth01(t / 0.30f);
            float wStern = Smooth01((t - 0.60f) / 0.40f);
            float x = Mathf.Lerp(NoseP2[k, 0], MidP2[k, 0], wMid);
            float y = Mathf.Lerp(NoseP2[k, 1], MidP2[k, 1], wMid);
            x = Mathf.Lerp(x, SternP2[k, 0], wStern);
            y = Mathf.Lerp(y, SternP2[k, 1], wStern);
            return new Vector2(x, y);
        }

        static Vector2 LoopPt2(int li, float t)
        {
            if (li <= 12) return HalfPt2(li, t);
            var p = HalfPt2(LoopPts - li, t);
            return new Vector2(-p.x, p.y);
        }

        static Vector2 HalfPt2F(float k, float t)
        {
            float wMid = Smooth01(t / 0.30f);
            float wStern = Smooth01((t - 0.60f) / 0.40f);
            var v = Vector2.Lerp(ProfCR(NoseP2, k), ProfCR(MidP2, k), wMid);
            return Vector2.Lerp(v, ProfCR(SternP2, k), wStern);
        }

        static int PaintMatC2(GenomeC2 g, int s, int p, float tm, bool[] flankCell, bool microHit)
        {
            if (tm < 0.05f) return 1;
            bool deck = s == 0 || s == 7;
            bool upper = s == 1 || s == 6;
            bool lower = s == 2 || s == 5;
            bool belly = s == 3 || s == 4;
            int side = s <= 3 ? 0 : 1;

            // Gold blade nose with a black chin panel and a side inlay slash.
            if (tm < 0.24f)
            {
                if (belly && tm > 0.06f) return 1;
                if (lower && p == 2 && tm > 0.08f) return 1;
                if (microHit) return 1;
                return 0;
            }

            // Aft of the canopy the fuselage runs black; gold saddle armor
            // plates cap the spine and upper flanks (cells vary per hash).
            if (deck || (upper && p == 0))
            {
                float ft = tm - 0.30f;
                if (ft >= 0f)
                {
                    int cell = Mathf.Min(2, (int)(ft / 0.18f));
                    float fu = ft - cell * 0.18f;
                    bool goldPlate = cell == 1 || flankCell[side * 6 + cell];
                    if (goldPlate && fu > 0.025f && fu < 0.155f) return 0;
                }
            }

            // Gold stern collar ahead of the drum bulkhead.
            if (tm > 0.90f && tm < 0.955f) return 0;

            // Gold machinery flecks in the black.
            if (microHit && tm > 0.28f) return 0;
            return 1;
        }

        // Swept blade wing (both C2 pairs): volumetric loft with a gentle
        // lens bow on both edges, black edge bands, three spar plates.
        static void BladeWing(Builder b, Vector3 rootF, Vector3 rootB, Vector3 tipB, Vector3 tipF, bool flip)
        {
            var mF = Vector3.Lerp(rootF, tipF, 0.5f) + new Vector3(0f, 0f, 0.14f);
            var mB = Vector3.Lerp(rootB, tipB, 0.5f) + new Vector3(0f, 0f, -0.12f);
            var lead = new[] { rootF, mF, tipF };
            var trail = new[] { rootB, mB, tipB };
            var ts = new[] { 0f, 0.5f, 1f };
            LoftWing(b, lead, ts, trail, ts, 0.11f, 0.035f, 8, flip);
            for (int plate = 0; plate < 3; plate++)
            {
                float f0 = 0.18f + plate * 0.24f;
                WingPlate(b, lead, ts, trail, ts, 0.11f, 0.035f, f0, f0 + 0.15f, plate == 1 ? 0 : 1);
            }
        }

        static GameObject BuildC2(string hash, Transform shipRoot)
        {
            var g = RollC2(hash);
            var b = new Builder();
            float L = g.L, W = g.W, H = g.H;
            const int rings2 = 120;

            var panelRng = Rng.Stream("hive2panels:" + hash);
            var flankCell = new bool[12];
            for (int i = 0; i < 12; i++) flankCell[i] = panelRng.NextDouble() < g.FlankOdds;
            var micro = new bool[rings2 - 1, Spans];
            for (int i = 0; i < rings2 - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.03;

            float[] cts = { 0.00f, 0.06f, 0.16f, 0.30f, 0.48f, 0.68f, 0.86f, 1.00f };
            float[] csc = { 0.04f, 0.18f, 0.40f, 0.62f, 0.84f, 1.00f, 0.98f, 0.88f };
            float[] clf = { 0.00f, 0.01f, 0.04f, 0.08f, 0.09f, 0.07f, 0.04f, 0.00f };

            var stripVerts = HullLoft48(b, rings2, HalfPt2F,
                t => CrSample(cts, csc, t)
                    * (1f + g.SurfAmp * 0.012f * Mathf.Sin((t * 6f + g.SurfPhase) * Mathf.PI * 2f)),
                t => CrSample(cts, clf, t) * H, t => (0.5f - t) * L, t => 1f, t => 1f, W, H,
                (so, po, tm, i) => PaintMatC2(g, so, po, tm, flankCell, micro[i, so * 3 + po]));

            // Needle nose and stern cap.
            var noseTip = new Vector3(0f, 0f, 0.5f * L + g.Nose);
            CapFan(b, noseTip, stripVerts, 0, true, 1);
            var sternC = new Vector3(0f, 0f, -0.5f * L - 0.04f);
            CapFan(b, sternC, stripVerts, rings2 - 1, false, 1);

            // Raked teardrop cockpit: amber glass in a black frame, faired
            // into the deck.
            {
                float tCan = g.CanopyStart + g.CanopyLen * 0.5f;
                float zC = (0.5f - tCan) * L;
                float halfLen = g.CanopyLen * L * 0.75f;
                System.Func<float, float> deckAt = z =>
                {
                    float t = Mathf.Clamp01(0.5f - z / L);
                    float sc = CrSample(cts, csc, t);
                    return HalfPt2(0, t).y * H * sc + CrSample(cts, clf, t) * H;
                };
                Canopy(b, zC + halfLen * 1.12f, zC - halfLen * 1.05f, 0.34f, 0.30f, deckAt, 2, 1, 1);
            }

            // Twin forward cannons — the two turret hardpoints, made visible.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * W * g.CannonSpread;
                float y = -0.14f * H;
                var housing0 = new Vector3(x, y, (0.5f - 0.22f) * L);
                var housing1 = new Vector3(x, y, (0.5f - 0.02f) * L);
                Tube(b, new[] { housing0, housing1 }, new[] { 0.085f, 0.075f }, 8, 1, false);
                var muzzleBase = new Vector3(x, y, 0.5f * L + g.Nose * 0.4f);
                var muzzleEnd = new Vector3(x, y, 0.5f * L + g.Nose * 0.4f + g.CannonLen * 1.15f);
                Tube(b, new[] { housing1, muzzleBase, muzzleEnd }, new[] { 0.055f, 0.045f, 0.030f }, 8, 1, false);
                Tube(b, new[] { muzzleEnd, muzzleEnd + Vector3.forward * 0.14f }, new[] { 0.05f, 0.045f }, 8, 1, true);
                Tube(b, new[] { housing1 + Vector3.forward * -0.04f, housing1 + Vector3.forward * 0.04f },
                    new[] { 0.09f, 0.09f }, 8, 0, false);
                // segmented barrel collars
                for (int cl = 0; cl < 2; cl++)
                {
                    var cc = Vector3.Lerp(muzzleBase, muzzleEnd, 0.22f + cl * 0.30f);
                    Tube(b, new[] { cc + Vector3.forward * 0.05f, cc - Vector3.forward * 0.05f },
                        new[] { 0.058f, 0.058f }, 8, 0, false);
                }
            }

            // Upper wing pair: big blades raked up and back.
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                var rootF = new Vector3(s * W * 0.45f, H * 0.60f, (0.5f - 0.60f) * L);
                var rootB = new Vector3(s * W * 0.40f, H * 0.55f, (0.5f - 0.80f) * L);
                var tipB = rootB + new Vector3(s * g.WingUpSpan * 0.72f, g.WingUpSpan * g.WingUpRake, -g.WingUpSweep);
                var tipF = rootF + new Vector3(s * g.WingUpSpan * 0.60f, g.WingUpSpan * g.WingUpRake * 0.92f, -g.WingUpSweep * 0.55f);
                BladeWing(b, rootF, rootB, tipB, tipF, side < 0);
            }

            // Lower wing pair: shorter blades dropped out and down.
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                var rootF = new Vector3(s * W * 0.85f, -H * 0.08f, (0.5f - 0.58f) * L);
                var rootB = new Vector3(s * W * 0.80f, -H * 0.12f, (0.5f - 0.78f) * L);
                var tipB = rootB + new Vector3(s * g.WingLowSpan, -g.WingLowSpan * g.WingLowDrop * 0.45f, -g.WingLowSweep);
                var tipF = rootF + new Vector3(s * g.WingLowSpan * 0.85f, -g.WingLowSpan * g.WingLowDrop * 0.40f, -g.WingLowSweep * 0.5f);
                BladeWing(b, rootF, rootB, tipB, tipF, side < 0);
            }

            // Twin segmented engine drums with collars and amber discs.
            for (int side = -1; side <= 1; side += 2)
            {
                var ec = new Vector3(side * W * 0.60f, 0.06f * H, -0.5f * L + 0.45f);
                int n = g.EngineSegs;
                var path = new Vector3[n + 1];
                var radii = new float[n + 1];
                for (int i = 0; i <= n; i++)
                {
                    path[i] = ec + Vector3.forward * (-i * 0.38f);
                    radii[i] = i % 2 == 0 ? 0.48f : 0.40f;
                }
                Tube(b, path, radii, 16, 1, false);
                for (int i = 1; i < n; i += 2)
                    Tube(b, new[] { path[i] + Vector3.forward * 0.045f, path[i] - Vector3.forward * 0.045f },
                        new[] { 0.505f, 0.505f }, 16, 0, false);
                var gc = path[n] + Vector3.forward * -0.03f;
                Nozzle(b, gc, Vector3.back, 0.30f * 1.55f, 0.30f * 1.30f, 16, 1, 2);
            }

            // Twin ventral strakes.
            for (int side = -1; side <= 1; side += 2)
            {
                float z0 = (0.5f - 0.80f) * L;
                var a = new Vector3(side * W * 0.30f, -H * 0.62f, z0);
                var bb = new Vector3(side * W * 0.34f, -H * (0.62f + g.FinSize), z0 - 0.45f);
                var c = new Vector3(side * W * 0.30f, -H * 0.58f, z0 - 0.40f);
                b.TriUDS(a, bb, c, 0);
                b.TriUDS(a + new Vector3(0, 0, -0.04f), bb + new Vector3(0, 0, -0.04f), c + new Vector3(0, 0, -0.04f), 1);
            }

            // Spine greeble boxes over the black armor.
            for (int i = 0; i < 3; i++)
            {
                float t = 0.32f + i * 0.13f;
                float sc = CrSample(cts, csc, t);
                float lift = CrSample(cts, clf, t) * H;
                float deckY = HalfPt2(0, t).y * H * sc + lift;
                var c = new Vector3(0f, deckY + 0.04f, (0.5f - t) * L);
                BevelBox(b, c, new Vector3(0.09f, 0.04f, 0.16f), i % 2 == 0 ? 0 : 1);
            }

            // Belly web-emitter ring (teal).
            {
                float sc = CrSample(cts, csc, 0.55f);
                float keelY = HalfPt2(12, 0.55f).y * H * sc;
                var wc = new Vector3(0f, keelY - 0.02f, (0.5f - 0.55f) * L);
                for (int k = 0; k < 12; k++)
                {
                    float a0 = k / 12f * Mathf.PI * 2f, a1 = (k + 1) / 12f * Mathf.PI * 2f;
                    const float r0 = 0.12f, r1 = 0.19f;
                    b.QuadUDS(
                        wc + new Vector3(Mathf.Cos(a0) * r0, 0f, Mathf.Sin(a0) * r0),
                        wc + new Vector3(Mathf.Cos(a0) * r1, 0f, Mathf.Sin(a0) * r1),
                        wc + new Vector3(Mathf.Cos(a1) * r1, 0f, Mathf.Sin(a1) * r1),
                        wc + new Vector3(Mathf.Cos(a1) * r0, 0f, Mathf.Sin(a1) * r0), 3);
                }
            }

            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "hive2_" + hash };
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


        // ================= CLASS 3 — "Interdictor" =================
        // Apex of the line, matched to the C3 reference: black-dominant
        // chitin with gold accent panels, long chisel nose, amber canopy,
        // two swept-back antennae, twin segmented lance cannons (the turret
        // hardpoints), four amber honeycomb membrane wings, six gold legs,
        // and twin banded engine drums per side. Fresh streams:
        // hive3body/hive3panels.

        class GenomeC3
        {
            public float L, W, H, Nose, CanopyStart, CanopyLen;
            public float AntLen, AntRake;
            public float LanceLen, LanceSpread;
            public float WingUpSpan, WingUpRake, WingUpSweep;
            public float WingLowSpan, WingLowRake, WingLowSweep;
            public float LegScale, FlankOdds, Hue, Sat, Val, SurfAmp, SurfPhase;
            public int EngineSegs, BandMode, BandCount;
            public float BandPhase, Splay;
            public float[] LegL, LegA;
            public Color Gold;
        }

        static GenomeC3 RollC3(string hash)
        {
            var rng = Rng.Stream("hive3body:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new GenomeC3();
            g.L = R(8.8f, 9.6f);
            g.W = R(1.05f, 1.20f);
            g.H = R(0.72f, 0.82f);
            g.Nose = R(0.85f, 1.25f);
            g.CanopyStart = R(0.14f, 0.18f);
            g.CanopyLen = R(0.13f, 0.17f);
            g.AntLen = R(3.6f, 4.6f);
            g.AntRake = R(0.55f, 0.75f);
            g.LanceLen = R(3.0f, 3.8f);
            g.LanceSpread = R(0.22f, 0.32f);
            g.WingUpSpan = R(3.4f, 4.0f);
            g.WingUpRake = R(0.70f, 0.90f);
            g.WingUpSweep = R(2.0f, 2.6f);
            g.WingLowSpan = R(2.0f, 2.5f);
            g.WingLowRake = R(0.10f, 0.25f);
            g.WingLowSweep = R(1.5f, 2.0f);
            g.LegScale = R(1.10f, 1.30f);
            g.FlankOdds = R(0.45f, 0.70f);
            g.Hue = R(0.100f, 0.125f);
            g.Sat = R(0.82f, 0.92f);
            g.Val = R(0.78f, 0.90f);
            g.SurfAmp = R(0f, 0.5f);
            g.SurfPhase = R(0f, 1f);
            g.EngineSegs = 4 + rng.Next(2);
            g.BandMode = rng.Next(4);
            g.BandCount = 2 + rng.Next(3);
            g.BandPhase = R(0f, 1f);
            g.Splay = R(0.05f, 0.20f);
            g.LegL = new float[6];
            g.LegA = new float[6];
            for (int i = 0; i < 6; i++) g.LegL[i] = R(0.90f, 1.10f);
            for (int i = 0; i < 6; i++) g.LegA[i] = R(-0.06f, 0.06f);
            g.Gold = Color.HSVToRGB(g.Hue, g.Sat, g.Val);
            return g;
        }

        // C3 sections: chisel head, armored thorax, rounded abdomen bulkhead.
        static readonly float[,] NoseP3 =
        {
            {0.00f, 0.26f}, {0.28f, 0.24f}, {0.52f, 0.20f}, {0.72f, 0.13f},
            {0.86f, 0.04f}, {0.93f, -0.04f}, {0.90f, -0.12f},
            {0.76f, -0.20f}, {0.54f, -0.26f}, {0.32f, -0.30f},
            {0.20f, -0.31f}, {0.10f, -0.32f}, {0.00f, -0.33f},
        };
        static readonly float[,] MidP3 =
        {
            {0.00f, 1.00f}, {0.30f, 0.95f}, {0.55f, 0.80f}, {0.74f, 0.56f},
            {0.88f, 0.28f}, {0.97f, 0.00f}, {1.00f, -0.26f},
            {0.87f, -0.48f}, {0.66f, -0.62f}, {0.44f, -0.70f},
            {0.28f, -0.74f}, {0.12f, -0.77f}, {0.00f, -0.78f},
        };
        static readonly float[,] SternP3 =
        {
            {0.00f, 0.82f}, {0.38f, 0.80f}, {0.64f, 0.72f}, {0.82f, 0.55f},
            {0.94f, 0.30f}, {1.00f, 0.02f}, {0.97f, -0.28f},
            {0.85f, -0.50f}, {0.64f, -0.63f}, {0.42f, -0.70f},
            {0.26f, -0.73f}, {0.10f, -0.76f}, {0.00f, -0.76f},
        };

        static Vector2 HalfPt3(int k, float t)
        {
            float wMid = Smooth01(t / 0.34f);
            float wStern = Smooth01((t - 0.62f) / 0.38f);
            float x = Mathf.Lerp(NoseP3[k, 0], MidP3[k, 0], wMid);
            float y = Mathf.Lerp(NoseP3[k, 1], MidP3[k, 1], wMid);
            x = Mathf.Lerp(x, SternP3[k, 0], wStern);
            y = Mathf.Lerp(y, SternP3[k, 1], wStern);
            return new Vector2(x, y);
        }

        static Vector2 LoopPt3(int li, float t)
        {
            if (li <= 12) return HalfPt3(li, t);
            var p = HalfPt3(LoopPts - li, t);
            return new Vector2(-p.x, p.y);
        }

        static Vector2 HalfPt3F(float k, float t)
        {
            float wMid = Smooth01(t / 0.34f);
            float wStern = Smooth01((t - 0.62f) / 0.38f);
            var v = Vector2.Lerp(ProfCR(NoseP3, k), ProfCR(MidP3, k), wMid);
            return Vector2.Lerp(v, ProfCR(SternP3, k), wStern);
        }

        // Black-dominant paint: gold saddles, flank cells, keel strip, and
        // banding families glint against dark chitin.
        static int PaintMatC3(GenomeC3 g, int s, int p, float tm, bool[] flankCell, bool microHit)
        {
            bool deck = s == 0 || s == 7;
            bool upper = s == 1 || s == 6;
            bool lower = s == 2 || s == 5;
            bool belly = s == 3 || s == 4;
            int side = s <= 3 ? 0 : 1;

            // Chisel tip stays black; a gold slash marks the head.
            if (tm < 0.05f) return 1;
            if (upper && tm > 0.06f && tm < 0.12f) return 0;

            // Gold deck saddles over thorax and abdomen.
            if (deck && ((tm > 0.30f && tm < 0.40f) || (tm > 0.55f && tm < 0.64f))) return 0;

            // Gold flank cells with chevron skew.
            if (upper || lower)
            {
                float skew = (upper ? 0f : 0.045f) + p * 0.018f;
                float ft = tm - 0.26f - skew;
                if (ft >= 0f && ft < 0.40f)
                {
                    int cell = Mathf.Min(2, (int)(ft / 0.1334f));
                    int band = upper ? 0 : 1;
                    if (flankCell[side * 6 + band * 3 + cell]) return 0;
                }
            }

            // Gold belly keel.
            if (belly && p == 2 && tm > 0.34f && tm < 0.60f) return 0;

            // Banding family — gold rings on black chitin.
            if (g.BandMode == 1 && tm > 0.68f && tm < 0.94f)
            {
                float u = (tm - 0.68f) / 0.26f;
                if ((int)(u * g.BandCount * 2 + g.BandPhase * 2f) % 2 == 0) return 0;
            }
            else if (g.BandMode == 2 && tm > 0.18f && tm < 0.34f)
            {
                float u = (tm - 0.18f) / 0.16f;
                if ((int)(u * g.BandCount * 2.5f + g.BandPhase * 2f) % 2 == 0) return 0;
            }
            else if (g.BandMode == 3 && deck && p == 0 && tm > 0.24f && tm < 0.72f)
            {
                return 0;
            }

            if (microHit && tm > 0.12f && tm < 0.92f) return 0;
            return 1;
        }

        // Honeycomb membrane wing: amber glow skin inside black edge spars,
        // dark cell ribs, one gold root spar.
        static void MembraneWing(Builder b, Vector3 rootF, Vector3 rootB, Vector3 tipB, Vector3 tipF, bool flip)
        {
            var mF = Vector3.Lerp(rootF, tipF, 0.5f) + new Vector3(0f, 0f, 0.16f);
            var mB = Vector3.Lerp(rootB, tipB, 0.5f) + new Vector3(0f, 0f, -0.14f);
            var lead = new[] { rootF, mF, tipF };
            var trail = new[] { rootB, mB, tipB };
            var ts = new[] { 0f, 0.5f, 1f };
            LoftWing(b, lead, ts, trail, ts, 0.07f, 0.020f, 9, flip, 2);
            for (int rib = 0; rib < 4; rib++)
            {
                float f0 = 0.14f + rib * 0.20f;
                WingPlate(b, lead, ts, trail, ts, 0.07f, 0.020f, f0, f0 + 0.05f, 1);
            }
            WingPlate(b, lead, ts, trail, ts, 0.07f, 0.020f, 0.02f, 0.10f, 0);
        }

        static GameObject BuildC3(string hash, Transform shipRoot)
        {
            var g = RollC3(hash);
            var b = new Builder();
            float L = g.L, W = g.W, H = g.H;
            const int rings3 = 128;

            var panelRng = Rng.Stream("hive3panels:" + hash);
            var flankCell = new bool[12];
            for (int i = 0; i < 12; i++) flankCell[i] = panelRng.NextDouble() < g.FlankOdds;
            var micro = new bool[rings3 - 1, Spans];
            for (int i = 0; i < rings3 - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.03;

            float[] cts = { 0.00f, 0.07f, 0.18f, 0.34f, 0.52f, 0.72f, 0.88f, 1.00f };
            float[] csc = { 0.05f, 0.22f, 0.48f, 0.75f, 1.00f, 0.96f, 0.86f, 0.70f };
            float[] clf = { 0.00f, 0.02f, 0.05f, 0.08f, 0.10f, 0.08f, 0.05f, 0.02f };

            var stripVerts = HullLoft48(b, rings3, HalfPt3F,
                t => CrSample(cts, csc, t)
                    * (1f + g.SurfAmp * 0.012f * Mathf.Sin((t * 6f + g.SurfPhase) * Mathf.PI * 2f)),
                t => CrSample(cts, clf, t) * H, t => (0.5f - t) * L, t => 1f, t => 1f, W, H,
                (so, po, tm, i) => PaintMatC3(g, so, po, tm, flankCell, micro[i, so * 3 + po]));

            // Chisel nose spike and stern cap.
            var noseTip = new Vector3(0f, -0.02f * H, 0.5f * L + g.Nose);
            CapFan(b, noseTip, stripVerts, 0, true, 1);
            var sternC = new Vector3(0f, 0f, -0.5f * L - 0.05f);
            CapFan(b, sternC, stripVerts, rings3 - 1, false, 1);

            // Raked teardrop cockpit: amber glass, heavy black framing,
            // twin ribs — faired into the chisel deck.
            {
                float tCan = g.CanopyStart + g.CanopyLen * 0.5f;
                float zC = (0.5f - tCan) * L;
                float halfLen = g.CanopyLen * L * 0.72f;
                System.Func<float, float> deckAt = z =>
                {
                    float t = Mathf.Clamp01(0.5f - z / L);
                    float sc = CrSample(cts, csc, t);
                    return HalfPt3(0, t).y * H * sc + CrSample(cts, clf, t) * H;
                };
                Canopy(b, zC + halfLen, zC - halfLen, 0.30f, 0.26f, deckAt, 2, 1, 2);
            }

            // Two swept-back antennae off the head.
            for (int side = -1; side <= 1; side += 2)
            {
                float scA = CrSample(cts, csc, 0.13f);
                float deckY = HalfPt3(0, 0.13f).y * H * scA + CrSample(cts, clf, 0.13f) * H;
                var mount = new Vector3(side * W * 0.14f, deckY - 0.02f, (0.5f - 0.13f) * L);
                var dir = new Vector3(side * 0.08f, g.AntRake * 0.55f, -0.85f).normalized;
                const int segsA = 6;
                var path = new Vector3[segsA + 1];
                var radii = new float[segsA + 1];
                for (int i = 0; i <= segsA; i++)
                {
                    float u = i / (float)segsA;
                    path[i] = mount + dir * (g.AntLen * u) + new Vector3(0f, 0.35f * u * u, 0f);
                    radii[i] = Mathf.Lerp(0.06f, 0.012f, u);
                }
                Tube(b, path, radii, 8, 0, true);
                var collar = mount + dir * (g.AntLen * 0.30f);
                Tube(b, new[] { collar - dir * 0.05f, collar + dir * 0.05f }, new[] { 0.06f, 0.06f }, 8, 1, false);
                Ball(b, mount, 0.10f, 1, 4, 8);
            }

            // Twin segmented lance cannons — the two turret hardpoints.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * W * g.LanceSpread;
                float y = -0.24f * H;
                var housing0 = new Vector3(x, y, (0.5f - 0.30f) * L);
                var housing1 = new Vector3(x, y, (0.5f - 0.04f) * L);
                Tube(b, new[] { housing0, housing1 }, new[] { 0.105f, 0.09f }, 8, 1, false);
                Tube(b, new[] { housing1 - Vector3.forward * 0.05f, housing1 + Vector3.forward * 0.05f },
                    new[] { 0.115f, 0.115f }, 8, 0, false);
                const int lanceSegs = 4;
                var path = new Vector3[lanceSegs + 1];
                var radii = new float[lanceSegs + 1];
                for (int i = 0; i <= lanceSegs; i++)
                {
                    path[i] = new Vector3(x, y, (0.5f - 0.04f) * L + i * (g.LanceLen * 0.55f / lanceSegs));
                    radii[i] = i % 2 == 0 ? 0.070f : 0.055f;
                }
                Tube(b, path, radii, 8, 1, false);
                var tip0 = path[lanceSegs];
                Tube(b, new[] { tip0, tip0 + Vector3.forward * (g.LanceLen * 0.45f) },
                    new[] { 0.040f, 0.005f }, 6, 1, true);
            }

            // Upper membrane wings: huge, raked steeply up and back.
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                var rootF = new Vector3(s * W * 0.40f, H * 0.62f, (0.5f - 0.30f) * L);
                var rootB = new Vector3(s * W * 0.36f, H * 0.56f, (0.5f - 0.48f) * L);
                var tipB = rootB + new Vector3(s * g.WingUpSpan * 0.62f, g.WingUpSpan * g.WingUpRake, -g.WingUpSweep);
                var tipF = rootF + new Vector3(s * g.WingUpSpan * 0.72f, g.WingUpSpan * g.WingUpRake * 0.94f, -g.WingUpSweep * 0.50f);
                MembraneWing(b, rootF, rootB, tipB, tipF, side < 0);
            }

            // Lower membrane wings: smaller, flatter, swept back.
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                var rootF = new Vector3(s * W * 0.55f, H * 0.05f, (0.5f - 0.52f) * L);
                var rootB = new Vector3(s * W * 0.50f, H * 0.00f, (0.5f - 0.68f) * L);
                var tipB = rootB + new Vector3(s * g.WingLowSpan, g.WingLowSpan * g.WingLowRake, -g.WingLowSweep);
                var tipF = rootF + new Vector3(s * g.WingLowSpan * 0.88f, g.WingLowSpan * g.WingLowRake * 0.9f, -g.WingLowSweep * 0.5f);
                MembraneWing(b, rootF, rootB, tipB, tipF, side < 0);
            }

            // Six legs — three per side, gold segments, black claw hooks.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int leg = 0; leg < 3; leg++)
                {
                    int li = (side < 0 ? 0 : 3) + leg;
                    float tm = 0.42f + leg * 0.16f;
                    var mount = new Vector3(side * W * 0.55f, -0.50f * H, (0.5f - tm) * L);
                    var d1 = new Vector3(side * (0.95f + g.Splay + g.LegA[li]), -0.35f, -0.25f).normalized;
                    float len1 = 0.75f * g.LegScale;
                    var j1 = mount + d1 * len1;
                    Fairing(b, mount - d1 * 0.02f, d1, 0.125f, 0.22f, 0.10f, 8, 0);
                    Tube(b, new[] { mount, j1 }, new[] { 0.11f, 0.09f }, 8, 0, false);
                    Ball(b, j1, 0.13f, 1, 4, 8);
                    var d2 = new Vector3(side * (0.50f + g.Splay), -0.62f, 0.35f + g.LegA[li]).normalized;
                    float len2 = 1.9f * g.LegL[li] * g.LegScale;
                    var j2 = j1 + d2 * len2;
                    Tube(b, new[] { j1, j1 + d2 * (len2 * 0.5f), j2 }, new[] { 0.15f, 0.12f, 0.08f }, 4, 0, false);
                    Ball(b, j2, 0.10f, 1, 4, 8);
                    var d3 = new Vector3(side * (0.24f + g.Splay * 0.5f), -0.75f, -0.20f).normalized;
                    float len3 = 1.0f * g.LegL[li] * g.LegScale;
                    Tube(b, new[] { j2, j2 + d3 * (len3 * 0.55f), j2 + d3 * len3 + new Vector3(0f, -0.05f, 0.16f) },
                        new[] { 0.07f, 0.05f, 0.006f }, 5, 1, true);
                }
            }

            // Twin main engine drums plus smaller dorsal auxiliaries.
            for (int side = -1; side <= 1; side += 2)
            {
                var ec = new Vector3(side * W * 0.55f, -0.08f * H, -0.5f * L + 0.40f);
                int n = g.EngineSegs;
                var path = new Vector3[n + 1];
                var radii = new float[n + 1];
                for (int i = 0; i <= n; i++)
                {
                    path[i] = ec + Vector3.forward * (-i * 0.36f);
                    radii[i] = i % 2 == 0 ? 0.48f : 0.40f;
                }
                Tube(b, path, radii, 16, 1, false);
                for (int i = 1; i < n; i += 2)
                    Tube(b, new[] { path[i] + Vector3.forward * 0.04f, path[i] - Vector3.forward * 0.04f },
                        new[] { 0.50f, 0.50f }, 16, 0, false);
                var gc = path[n] + Vector3.forward * -0.03f;
                Nozzle(b, gc, Vector3.back, 0.30f * 1.55f, 0.30f * 1.30f, 16, 1, 2);

                var ac = new Vector3(side * W * 0.28f, 0.42f * H, -0.5f * L + 0.25f);
                var path2 = new Vector3[4];
                var radii2 = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    path2[i] = ac + Vector3.forward * (-i * 0.26f);
                    radii2[i] = i % 2 == 0 ? 0.22f : 0.18f;
                }
                Tube(b, path2, radii2, 12, 1, false);
                Tube(b, new[] { path2[1] + Vector3.forward * 0.03f, path2[1] - Vector3.forward * 0.03f },
                    new[] { 0.23f, 0.23f }, 12, 0, false);
                var gc2 = path2[3] + Vector3.forward * -0.02f;
                Nozzle(b, gc2, Vector3.back, 0.13f * 1.55f, 0.13f * 1.30f, 12, 1, 2);
            }

            // Belly web-emitter ring (teal).
            {
                float sc = CrSample(cts, csc, 0.50f);
                float keelY = HalfPt3(12, 0.50f).y * H * sc;
                var wc = new Vector3(0f, keelY - 0.02f, (0.5f - 0.50f) * L);
                for (int k = 0; k < 12; k++)
                {
                    float a0 = k / 12f * Mathf.PI * 2f, a1 = (k + 1) / 12f * Mathf.PI * 2f;
                    const float r0 = 0.14f, r1 = 0.22f;
                    b.QuadUDS(
                        wc + new Vector3(Mathf.Cos(a0) * r0, 0f, Mathf.Sin(a0) * r0),
                        wc + new Vector3(Mathf.Cos(a0) * r1, 0f, Mathf.Sin(a0) * r1),
                        wc + new Vector3(Mathf.Cos(a1) * r1, 0f, Mathf.Sin(a1) * r1),
                        wc + new Vector3(Mathf.Cos(a1) * r0, 0f, Mathf.Sin(a1) * r0), 3);
                }
            }

            // Dorsal warp-disruptor emitter: pedestal, four gold prongs,
            // amber core.
            {
                float td = 0.36f;
                float sc = CrSample(cts, csc, td);
                float lift = CrSample(cts, clf, td) * H;
                float deckY = HalfPt3(0, td).y * H * sc + lift;
                var dc = new Vector3(0f, deckY + 0.01f, (0.5f - td) * L);
                Tube(b, new[] { dc, dc + new Vector3(0f, 0.26f, 0f) }, new[] { 0.10f, 0.075f }, 8, 1, false);
                Fairing(b, dc, Vector3.up, 0.115f, 0.19f, 0.05f, 8, 1);
                // coil stack: five fat rings climbing the mast to the core
                for (int r2 = 0; r2 < 5; r2++)
                {
                    float y = 0.05f + r2 * 0.048f;
                    float rr = 0.115f - r2 * 0.008f;
                    Tube(b, new[] { dc + new Vector3(0f, y - 0.014f, 0f), dc + new Vector3(0f, y + 0.014f, 0f) },
                        new[] { rr, rr }, 8, r2 % 2 == 0 ? 0 : 3, false);
                }
                var core = dc + new Vector3(0f, 0.32f, 0f);
                Ball(b, core, 0.10f, 2, 4, 8);
                for (int k = 0; k < 4; k++)
                {
                    float a = (k + 0.5f) / 4f * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Cos(a) * 0.60f, 0.62f, Mathf.Sin(a) * 0.60f).normalized;
                    Tube(b, new[] { core + dir * 0.06f, core + dir * 0.48f }, new[] { 0.035f, 0.008f }, 4, 0, true);
                }
            }

            // Spine greeble boxes over the gold saddles.
            for (int i = 0; i < 3; i++)
            {
                float t = 0.30f + i * 0.14f;
                float sc = CrSample(cts, csc, t);
                float lift = CrSample(cts, clf, t) * H;
                float deckY = HalfPt3(0, t).y * H * sc + lift;
                var c = new Vector3(0f, deckY + 0.04f, (0.5f - t) * L);
                BevelBox(b, c, new Vector3(0.10f, 0.045f, 0.18f), i % 2 == 0 ? 1 : 0);
            }

            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "hive3_" + hash };
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


    }
}
