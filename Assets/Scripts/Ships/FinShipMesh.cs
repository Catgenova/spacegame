using UnityEngine;
using static SpaceGame.MeshKit;

namespace SpaceGame
{
    /// <summary>
    /// Fin-class hull meshes — aquatic destroyers where Hive is wasp.
    /// C1 "Shark", matched to its reference: flat wide arrowhead body in
    /// white with cobalt mottle camo (countershaded — blue up top, white
    /// belly), black glass teardrop canopy, a rake of swept dorsal fins,
    /// broad pectoral wings, tail planes, ventral fins, twin underslung
    /// segmented gun barrels, and twin engine drums with blue wake.
    /// Submeshes: 0 white metal, 1 cobalt metal, 2 blue glow, 3 dark glass.
    /// Streams: finbody / finpanels.
    /// </summary>
    public static class FinShipMesh
    {
        const int LoopPts = 24;
        const int Spans = 24;
        static readonly int[] StripStart = { 0, 3, 6, 9, 12, 15, 18, 21 };

        public static GameObject Build(string hash, int cls, Transform shipRoot)
            => cls == 2 ? BuildC2(hash, shipRoot) : BuildC1(hash, shipRoot);

        class GenomeF1
        {
            public float L, W, H, Nose, CanopyStart, CanopyLen;
            public float GunLen, GunSpread;
            public int DorsalCount;
            public float DorsalSize, DorsalSweep;
            public float PectSpan, PectSweep, TailSpan, TailSweep, VentSize;
            public int EngineSegs;
            public float Hue, Sat, Val, WhiteVal, CamoOdds, SurfAmp, SurfPhase;
            public int BandMode, BandCount;
            public float BandPhase;
            public Color Blue, White;
        }

        static GenomeF1 RollF1(string hash)
        {
            var rng = Rng.Stream("finbody:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new GenomeF1();
            g.L = R(6.8f, 7.4f);
            g.W = R(0.95f, 1.10f);
            g.H = R(0.80f, 0.92f);
            g.Nose = R(0.60f, 0.90f);
            g.CanopyStart = R(0.18f, 0.24f);
            g.CanopyLen = R(0.16f, 0.22f);
            g.GunLen = R(2.2f, 2.8f);
            g.GunSpread = R(0.30f, 0.40f);
            g.DorsalCount = 2 + rng.Next(2);
            g.DorsalSize = R(0.90f, 1.20f);
            g.DorsalSweep = R(1.10f, 1.50f);
            g.PectSpan = R(1.9f, 2.4f);
            g.PectSweep = R(1.6f, 2.2f);
            g.TailSpan = R(1.2f, 1.5f);
            g.TailSweep = R(1.0f, 1.4f);
            g.VentSize = R(0.35f, 0.55f);
            g.EngineSegs = 4 + rng.Next(2);
            g.Hue = R(0.560f, 0.630f);
            g.Sat = R(0.75f, 0.90f);
            g.Val = R(0.70f, 0.85f);
            g.WhiteVal = R(0.86f, 0.94f);
            g.CamoOdds = R(0.40f, 0.65f);
            g.SurfAmp = R(0f, 0.5f);
            g.SurfPhase = R(0f, 1f);
            g.BandMode = rng.Next(3);
            g.BandCount = 2 + rng.Next(3);
            g.BandPhase = R(0f, 1f);
            g.Blue = Color.HSVToRGB(g.Hue, g.Sat, g.Val);
            g.White = new Color(g.WhiteVal, g.WhiteVal + 0.02f, g.WhiteVal + 0.05f);
            return g;
        }

        // Flat, wide shark-arrow sections: thin sliver nose, broad chined
        // midbody, engine bulkhead stern.
        static readonly float[,] NoseF =
        {
            {0.00f, 0.14f}, {0.30f, 0.13f}, {0.55f, 0.11f}, {0.75f, 0.08f},
            {0.90f, 0.04f}, {0.98f, 0.00f}, {1.00f, -0.03f},
            {0.85f, -0.07f}, {0.62f, -0.10f}, {0.38f, -0.12f},
            {0.24f, -0.13f}, {0.10f, -0.14f}, {0.00f, -0.14f},
        };
        static readonly float[,] MidF =
        {
            {0.00f, 0.55f}, {0.35f, 0.52f}, {0.62f, 0.44f}, {0.82f, 0.30f},
            {0.94f, 0.14f}, {1.00f, -0.02f}, {0.96f, -0.16f},
            {0.84f, -0.28f}, {0.64f, -0.36f}, {0.42f, -0.42f},
            {0.26f, -0.44f}, {0.10f, -0.46f}, {0.00f, -0.46f},
        };
        static readonly float[,] SternF =
        {
            {0.00f, 0.62f}, {0.38f, 0.58f}, {0.64f, 0.50f}, {0.82f, 0.36f},
            {0.93f, 0.18f}, {1.00f, 0.00f}, {0.96f, -0.18f},
            {0.84f, -0.32f}, {0.63f, -0.40f}, {0.41f, -0.46f},
            {0.25f, -0.48f}, {0.10f, -0.50f}, {0.00f, -0.50f},
        };

        static Vector2 HalfPtF(int k, float t)
        {
            float wMid = Smooth01(t / 0.38f);
            float wStern = Smooth01((t - 0.60f) / 0.40f);
            float x = Mathf.Lerp(NoseF[k, 0], MidF[k, 0], wMid);
            float y = Mathf.Lerp(NoseF[k, 1], MidF[k, 1], wMid);
            x = Mathf.Lerp(x, SternF[k, 0], wStern);
            y = Mathf.Lerp(y, SternF[k, 1], wStern);
            return new Vector2(x, y);
        }

        static Vector2 LoopPtF(int li, float t)
        {
            if (li <= 12) return HalfPtF(li, t);
            var p = HalfPtF(LoopPts - li, t);
            return new Vector2(-p.x, p.y);
        }


        // Deep shark sections for the C1: rounded back, full flanks, keel —
        // taller than wide once W/H scale in. The flat F set above stays
        // with the Manta.
        static readonly float[,] NoseS =
        {
            {0.00f, 0.20f}, {0.30f, 0.18f}, {0.55f, 0.15f}, {0.75f, 0.11f},
            {0.90f, 0.06f}, {0.98f, 0.00f}, {1.00f, -0.05f},
            {0.85f, -0.10f}, {0.62f, -0.14f}, {0.38f, -0.17f},
            {0.24f, -0.18f}, {0.10f, -0.19f}, {0.00f, -0.19f},
        };
        static readonly float[,] MidS =
        {
            {0.00f, 0.72f}, {0.32f, 0.68f}, {0.58f, 0.56f}, {0.78f, 0.38f},
            {0.92f, 0.16f}, {1.00f, -0.06f}, {0.94f, -0.28f},
            {0.80f, -0.44f}, {0.60f, -0.54f}, {0.40f, -0.60f},
            {0.25f, -0.63f}, {0.10f, -0.65f}, {0.00f, -0.66f},
        };
        static readonly float[,] SternS =
        {
            {0.00f, 0.66f}, {0.34f, 0.62f}, {0.60f, 0.52f}, {0.80f, 0.36f},
            {0.92f, 0.16f}, {1.00f, -0.04f}, {0.94f, -0.26f},
            {0.80f, -0.40f}, {0.60f, -0.50f}, {0.40f, -0.56f},
            {0.25f, -0.58f}, {0.10f, -0.60f}, {0.00f, -0.60f},
        };

        static Vector2 HalfPtS(int k, float t)
        {
            float wMid = Smooth01(t / 0.38f);
            float wStern = Smooth01((t - 0.60f) / 0.40f);
            float x = Mathf.Lerp(NoseS[k, 0], MidS[k, 0], wMid);
            float y = Mathf.Lerp(NoseS[k, 1], MidS[k, 1], wMid);
            x = Mathf.Lerp(x, SternS[k, 0], wStern);
            y = Mathf.Lerp(y, SternS[k, 1], wStern);
            return new Vector2(x, y);
        }

        static Vector2 LoopPtS(int li, float t)
        {
            if (li <= 12) return HalfPtS(li, t);
            var p = HalfPtS(LoopPts - li, t);
            return new Vector2(-p.x, p.y);
        }

        // Countershaded shark camo: white base, cobalt spine/chines/mottle
        // up top, clean white belly.
        static int PaintMatF1(GenomeF1 g, int s, int p, float tm, bool[] camoCell, bool microHit)
        {
            bool deck = s == 0 || s == 7;
            bool upper = s == 1 || s == 6;
            bool lower = s == 2 || s == 5;
            bool belly = s == 3 || s == 4;
            int side = s <= 3 ? 0 : 1;

            // Cobalt nose tip.
            if (tm < 0.06f) return 1;

            // Blue spine stripe running forward.
            if (deck && p == 0 && tm < 0.50f) return 1;

            // Blue chine edging along the widest line.
            bool chineSpan = (s == 1 && p == 2) || (s == 2 && p == 0)
                || (s == 6 && p == 0) || (s == 5 && p == 2);
            if (chineSpan && tm > 0.15f && tm < 0.85f) return 1;

            // Mottle camo cells — dense on deck/upper, sparse low, none on belly.
            if (deck || upper || lower)
            {
                float skew = (deck ? 0f : upper ? 0.03f : 0.06f) + p * 0.02f;
                float ft = tm - 0.10f - skew;
                if (ft >= 0f && ft < 0.60f)
                {
                    int cell = Mathf.Min(4, (int)(ft / 0.121f));
                    int band = deck ? 0 : upper ? 1 : 2;
                    bool hit = camoCell[(side * 15 + band * 5 + cell) % 30];
                    if (hit && !(lower && p == 1)) return 1;
                }
            }

            // Band families: tail rings or nose chevrons.
            if (g.BandMode == 1 && tm > 0.78f && tm < 0.95f)
            {
                float u = (tm - 0.78f) / 0.17f;
                if ((int)(u * g.BandCount * 2 + g.BandPhase * 2f) % 2 == 0) return 1;
            }
            else if (g.BandMode == 2 && !belly && tm > 0.10f && tm < 0.26f)
            {
                float u = (tm - 0.10f) / 0.16f;
                if ((int)(u * g.BandCount * 2.5f + g.BandPhase * 2f) % 2 == 0) return 1;
            }

            if (microHit && !belly && tm > 0.10f && tm < 0.92f) return 1;
            return 0;
        }

        static GameObject BuildC1(string hash, Transform shipRoot)
        {
            var g = RollF1(hash);
            var b = new Builder();
            float L = g.L, W = g.W, H = g.H;
            const int rings = 56;

            var panelRng = Rng.Stream("finpanels:" + hash);
            var camoCell = new bool[30];
            for (int i = 0; i < 30; i++) camoCell[i] = panelRng.NextDouble() < g.CamoOdds;
            var micro = new bool[rings - 1, Spans];
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.03;

            float[] cts = { 0.00f, 0.08f, 0.20f, 0.36f, 0.55f, 0.74f, 0.90f, 1.00f };
            float[] csc = { 0.04f, 0.20f, 0.44f, 0.72f, 1.00f, 0.88f, 0.66f, 0.48f };
            float[] clf = { 0.00f, 0.01f, 0.03f, 0.05f, 0.06f, 0.05f, 0.03f, 0.01f };

            var stripVerts = new int[8][][];
            var ringT = new float[rings];
            for (int s = 0; s < 8; s++) stripVerts[s] = new int[rings][];
            for (int i = 0; i < rings; i++)
            {
                float t = i / (float)(rings - 1);
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
                        var pt = LoopPtS(li, t);
                        stripVerts[s][i][p] = b.Add(new Vector3(pt.x * W * sc, pt.y * H * sc + lift, z));
                    }
                }
            }
            for (int i = 0; i < rings - 1; i++)
            {
                float tm = (ringT[i] + ringT[i + 1]) * 0.5f;
                for (int s = 0; s < 8; s++)
                    for (int p = 0; p < 3; p++)
                    {
                        int mat = PaintMatF1(g, s, p, tm, camoCell, micro[i, s * 3 + p]);
                        b.FaceQ(stripVerts[s][i][p], stripVerts[s][i + 1][p],
                            stripVerts[s][i + 1][p + 1], stripVerts[s][i][p + 1], mat);
                    }
            }

            // Snout point and stern cap.
            var noseTip = new Vector3(0f, -0.01f * H, 0.5f * L + g.Nose);
            for (int s = 0; s < 8; s++)
                for (int p = 0; p < 3; p++)
                    b.TriU(noseTip, b.V[stripVerts[s][0][p + 1]], b.V[stripVerts[s][0][p]], 1);
            var sternC = new Vector3(0f, 0f, -0.5f * L - 0.05f);
            for (int s = 0; s < 8; s++)
                for (int p = 0; p < 3; p++)
                    b.TriU(sternC, b.V[stripVerts[s][rings - 1][p]], b.V[stripVerts[s][rings - 1][p + 1]], 1);

            // Long, low teardrop cockpit: dark glass in a cobalt frame,
            // flush with the snout line.
            {
                float tCan = g.CanopyStart + g.CanopyLen * 0.5f;
                float zC = (0.5f - tCan) * L;
                float halfLen = g.CanopyLen * L * 0.72f;
                System.Func<float, float> deckAt = z =>
                {
                    float t = Mathf.Clamp01(0.5f - z / L);
                    float sc = CrSample(cts, csc, t);
                    return HalfPtS(0, t).y * H * sc + CrSample(cts, clf, t) * H;
                };
                Canopy(b, zC + halfLen, zC - halfLen, 0.30f, 0.20f, deckAt, 3, 1, 1);
            }

            // Rake of swept dorsal fins along the spine, tallest first.
            for (int f = 0; f < g.DorsalCount; f++)
            {
                float t0 = 0.30f + f * 0.15f;
                float size = g.DorsalSize * (1f - f * 0.22f);
                float scD = CrSample(cts, csc, t0);
                float liftD = CrSample(cts, clf, t0) * H;
                float y0 = HalfPtS(0, t0).y * H * scD + liftD;
                float zA = (0.5f - t0) * L;
                float zB = zA - 0.85f * (1f - f * 0.15f);
                var rootF2 = new Vector3(0f, y0 - 0.03f, zA);
                var rootB2 = new Vector3(0f, y0 - 0.03f, zB);
                var tipF2 = new Vector3(0f, y0 + size, zA - g.DorsalSweep * 0.72f);
                var tipB2 = new Vector3(0f, y0 + size * 0.88f, zB - g.DorsalSweep);
                var lead = new[] { rootF2, Vector3.Lerp(rootF2, tipF2, 0.5f) + new Vector3(0f, 0f, 0.10f), tipF2 };
                var trail = new[] { rootB2, Vector3.Lerp(rootB2, tipB2, 0.5f) + new Vector3(0f, 0f, -0.08f), tipB2 };
                var ts = new[] { 0f, 0.5f, 1f };
                LoftWing(b, lead, ts, trail, ts, 0.055f, 0.015f, 7, true, 1);
                WingPlate(b, lead, ts, trail, ts, 0.055f, 0.015f, 0.25f, 0.45f, 0);
            }

            // Broad pectoral wings off the chines: white skin, cobalt camo.
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                var rootF2 = new Vector3(s * W * 0.55f, -0.05f * H, (0.5f - 0.38f) * L);
                var rootB2 = new Vector3(s * W * 0.60f, -0.08f * H, (0.5f - 0.62f) * L);
                var tipB2 = rootB2 + new Vector3(s * g.PectSpan * 0.85f, -0.05f, -g.PectSweep);
                var tipF2 = rootF2 + new Vector3(s * g.PectSpan, 0.02f, -g.PectSweep * 0.45f);
                var lead = new[] { rootF2, Vector3.Lerp(rootF2, tipF2, 0.5f) + new Vector3(0f, 0f, 0.14f), tipF2 };
                var trail = new[] { rootB2, Vector3.Lerp(rootB2, tipB2, 0.5f) + new Vector3(0f, 0f, -0.12f), tipB2 };
                var ts = new[] { 0f, 0.5f, 1f };
                LoftWing(b, lead, ts, trail, ts, 0.10f, 0.030f, 8, side < 0, 0);
                WingPlate(b, lead, ts, trail, ts, 0.10f, 0.030f, 0.18f, 0.34f, 1);
                WingPlate(b, lead, ts, trail, ts, 0.10f, 0.030f, 0.48f, 0.60f, 1);
                WingPlate(b, lead, ts, trail, ts, 0.10f, 0.030f, 0.72f, 0.80f, 1);
            }

            // Tail planes, cobalt, angled slightly up.
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                var rootF2 = new Vector3(s * W * 0.45f, 0.05f * H, (0.5f - 0.80f) * L);
                var rootB2 = new Vector3(s * W * 0.42f, 0.02f * H, (0.5f - 0.92f) * L);
                var tipB2 = rootB2 + new Vector3(s * g.TailSpan, 0.10f, -g.TailSweep);
                var tipF2 = rootF2 + new Vector3(s * g.TailSpan * 0.88f, 0.12f, -g.TailSweep * 0.5f);
                var lead = new[] { rootF2, Vector3.Lerp(rootF2, tipF2, 0.5f) + new Vector3(0f, 0f, 0.10f), tipF2 };
                var trail = new[] { rootB2, Vector3.Lerp(rootB2, tipB2, 0.5f) + new Vector3(0f, 0f, -0.08f), tipB2 };
                var ts = new[] { 0f, 0.5f, 1f };
                LoftWing(b, lead, ts, trail, ts, 0.07f, 0.020f, 6, side < 0, 1);
                WingPlate(b, lead, ts, trail, ts, 0.07f, 0.020f, 0.30f, 0.48f, 0);
            }

            // Small ventral fins angled down and out.
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                var rootF2 = new Vector3(s * W * 0.22f, -0.36f * H, (0.5f - 0.66f) * L);
                var rootB2 = new Vector3(s * W * 0.22f, -0.36f * H, (0.5f - 0.78f) * L);
                var tipF2 = rootF2 + new Vector3(s * g.VentSize * 0.7f, -g.VentSize, -0.30f);
                var tipB2 = rootB2 + new Vector3(s * g.VentSize * 0.6f, -g.VentSize * 0.85f, -0.45f);
                var lead = new[] { rootF2, tipF2 };
                var trail = new[] { rootB2, tipB2 };
                var ts = new[] { 0f, 1f };
                LoftWing(b, lead, ts, trail, ts, 0.045f, 0.012f, 5, side < 0, 1);
            }

            // Twin underslung segmented gun barrels — the turret hardpoints.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * W * g.GunSpread;
                float y = -0.30f * H;
                var housing0 = new Vector3(x, y, (0.5f - 0.35f) * L);
                var housing1 = new Vector3(x, y, (0.5f - 0.10f) * L);
                Tube(b, new[] { housing0, housing1 }, new[] { 0.10f, 0.09f }, 8, 1, false);
                Tube(b, new[] { housing1 - Vector3.forward * 0.05f, housing1 + Vector3.forward * 0.05f },
                    new[] { 0.105f, 0.105f }, 8, 2, false);
                const int gunSegs = 4;
                var path = new Vector3[gunSegs + 1];
                var radii = new float[gunSegs + 1];
                for (int i = 0; i <= gunSegs; i++)
                {
                    path[i] = new Vector3(x, y, (0.5f - 0.10f) * L + i * (g.GunLen * 0.6f / gunSegs));
                    radii[i] = i % 2 == 0 ? 0.065f : 0.050f;
                }
                Tube(b, path, radii, 8, 1, false);
                var tip0 = path[gunSegs];
                Tube(b, new[] { tip0, tip0 + Vector3.forward * (g.GunLen * 0.4f) },
                    new[] { 0.035f, 0.005f }, 6, 1, true);
            }

            // Twin engine drums with white collars and blue wake discs.
            for (int side = -1; side <= 1; side += 2)
            {
                var ec = new Vector3(side * W * 0.45f, 0f, -0.5f * L + 0.35f);
                int n = g.EngineSegs;
                var path = new Vector3[n + 1];
                var radii = new float[n + 1];
                for (int i = 0; i <= n; i++)
                {
                    path[i] = ec + Vector3.forward * (-i * 0.32f);
                    radii[i] = i % 2 == 0 ? 0.34f : 0.29f;
                }
                Tube(b, path, radii, 16, 1, false);
                for (int i = 1; i < n; i += 2)
                    Tube(b, new[] { path[i] + Vector3.forward * 0.04f, path[i] - Vector3.forward * 0.04f },
                        new[] { 0.36f, 0.36f }, 16, 0, false);
                var gc = path[n] + Vector3.forward * -0.03f;
                for (int k = 0; k < 16; k++)
                {
                    float a0 = k / 16f * Mathf.PI * 2f, a1 = (k + 1) / 16f * Mathf.PI * 2f;
                    b.TriUDS(gc,
                        gc + new Vector3(Mathf.Cos(a0) * 0.21f, Mathf.Sin(a0) * 0.21f, 0f),
                        gc + new Vector3(Mathf.Cos(a1) * 0.21f, Mathf.Sin(a1) * 0.21f, 0f), 2);
                }
            }

            // Glowing intake slits along the lower flanks.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float t = 0.24f + i * 0.06f;
                    float sc = CrSample(cts, csc, t);
                    var c = new Vector3(side * W * sc * 0.80f, -0.24f * H, (0.5f - t) * L);
                    Box(b, c, new Vector3(0.035f, 0.025f, 0.06f), 2);
                }
            }

            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "fin1_" + hash };
            mesh.SetVertices(b.V);
            mesh.subMeshCount = 4;
            for (int m = 0; m < 4; m++) mesh.SetTriangles(b.Sub[m], m);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.materials = new[]
            {
                Metal(g.White, 0.85f, 0.80f),
                Metal(g.Blue, 0.92f, 0.88f),
                SystemView.Mat(new Color(0.30f, 0.65f, 1f), true),
                Metal(new Color(0.03f, 0.04f, 0.06f), 0.9f, 0.95f),
            };
            return go;
        }

        // ================= CLASS 2 — "Manta" =================
        // A hull that is mostly wing, matched to its reference: broad flat
        // center body flowing into two huge crescent wings with up-curled
        // tips, a long dark-glass canopy, three segmented lance guns (two
        // chin, one keel), small upswept rear fins, and twin engine drums
        // tucked at the wing roots. Blue-dominant camo, white streaks.
        // Streams: fin2body / fin2panels.

        class GenomeF2
        {
            public float L, W, H, Nose, CanopyStart, CanopyLen;
            public float GunLen, GunSpread;
            public float WingSpan, WingCurl, WingSweep, RearFinSize;
            public int EngineSegs;
            public float Hue, Sat, Val, WhiteVal, CamoOdds, SurfAmp, SurfPhase;
            public int BandMode, BandCount;
            public float BandPhase;
            public Color Blue, White;
        }

        static GenomeF2 RollF2(string hash)
        {
            var rng = Rng.Stream("fin2body:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new GenomeF2();
            g.L = R(7.6f, 8.2f);
            g.W = R(1.70f, 1.90f);
            g.H = R(0.60f, 0.70f);
            g.Nose = R(0.50f, 0.80f);
            g.CanopyStart = R(0.16f, 0.22f);
            g.CanopyLen = R(0.20f, 0.26f);
            g.GunLen = R(2.6f, 3.2f);
            g.GunSpread = R(0.20f, 0.28f);
            g.WingSpan = R(3.6f, 4.4f);
            g.WingCurl = R(0.50f, 0.80f);
            g.WingSweep = R(2.4f, 3.0f);
            g.RearFinSize = R(0.50f, 0.80f);
            g.EngineSegs = 4 + rng.Next(2);
            g.Hue = R(0.560f, 0.630f);
            g.Sat = R(0.75f, 0.90f);
            g.Val = R(0.70f, 0.85f);
            g.WhiteVal = R(0.86f, 0.94f);
            g.CamoOdds = R(0.55f, 0.75f);
            g.SurfAmp = R(0f, 0.5f);
            g.SurfPhase = R(0f, 1f);
            g.BandMode = rng.Next(3);
            g.BandCount = 2 + rng.Next(3);
            g.BandPhase = R(0f, 1f);
            g.Blue = Color.HSVToRGB(g.Hue, g.Sat, g.Val);
            g.White = new Color(g.WhiteVal, g.WhiteVal + 0.02f, g.WhiteVal + 0.05f);
            return g;
        }

        // Denser mottle than the Shark; same countershading rules.
        static int PaintMatF2(GenomeF2 g, int s, int p, float tm, bool[] camoCell, bool microHit)
        {
            bool deck = s == 0 || s == 7;
            bool upper = s == 1 || s == 6;
            bool lower = s == 2 || s == 5;
            bool belly = s == 3 || s == 4;
            int side = s <= 3 ? 0 : 1;

            if (tm < 0.08f) return 1;
            if (deck && p == 0 && tm < 0.55f) return 1;
            bool chineSpan = (s == 1 && p == 2) || (s == 2 && p == 0)
                || (s == 6 && p == 0) || (s == 5 && p == 2);
            if (chineSpan && tm > 0.12f && tm < 0.88f) return 1;
            if (deck || upper || lower)
            {
                float skew = (deck ? 0f : upper ? 0.03f : 0.06f) + p * 0.02f;
                float ft = tm - 0.10f - skew;
                if (ft >= 0f && ft < 0.70f)
                {
                    int cell = Mathf.Min(4, (int)(ft / 0.141f));
                    int band = deck ? 0 : upper ? 1 : 2;
                    bool hit = camoCell[(side * 15 + band * 5 + cell) % 30];
                    if (hit && !(lower && p == 1)) return 1;
                }
            }
            if (g.BandMode == 1 && tm > 0.78f && tm < 0.95f)
            {
                float u = (tm - 0.78f) / 0.17f;
                if ((int)(u * g.BandCount * 2 + g.BandPhase * 2f) % 2 == 0) return 1;
            }
            else if (g.BandMode == 2 && !belly && tm > 0.12f && tm < 0.30f)
            {
                float u = (tm - 0.12f) / 0.18f;
                if ((int)(u * g.BandCount * 2.5f + g.BandPhase * 2f) % 2 == 0) return 1;
            }
            if (microHit && !belly && tm > 0.10f && tm < 0.92f) return 1;
            return 0;
        }

        static GameObject BuildC2(string hash, Transform shipRoot)
        {
            var g = RollF2(hash);
            var b = new Builder();
            float L = g.L, W = g.W, H = g.H;
            const int rings = 52;

            var panelRng = Rng.Stream("fin2panels:" + hash);
            var camoCell = new bool[30];
            for (int i = 0; i < 30; i++) camoCell[i] = panelRng.NextDouble() < g.CamoOdds;
            var micro = new bool[rings - 1, Spans];
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.03;

            // Rounder plan than the Shark: widest just before midship, long
            // clean taper into the tail.
            float[] cts = { 0.00f, 0.08f, 0.20f, 0.34f, 0.50f, 0.70f, 0.88f, 1.00f };
            float[] csc = { 0.05f, 0.24f, 0.52f, 0.82f, 1.00f, 0.84f, 0.60f, 0.42f };
            float[] clf = { 0.00f, 0.01f, 0.03f, 0.05f, 0.06f, 0.05f, 0.03f, 0.01f };

            var stripVerts = new int[8][][];
            var ringT = new float[rings];
            for (int s = 0; s < 8; s++) stripVerts[s] = new int[rings][];
            for (int i = 0; i < rings; i++)
            {
                float t = i / (float)(rings - 1);
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
                        var pt = LoopPtF(li, t);
                        stripVerts[s][i][p] = b.Add(new Vector3(pt.x * W * sc, pt.y * H * sc + lift, z));
                    }
                }
            }
            for (int i = 0; i < rings - 1; i++)
            {
                float tm = (ringT[i] + ringT[i + 1]) * 0.5f;
                for (int s = 0; s < 8; s++)
                    for (int p = 0; p < 3; p++)
                    {
                        int mat = PaintMatF2(g, s, p, tm, camoCell, micro[i, s * 3 + p]);
                        b.FaceQ(stripVerts[s][i][p], stripVerts[s][i + 1][p],
                            stripVerts[s][i + 1][p + 1], stripVerts[s][i][p + 1], mat);
                    }
            }

            var noseTip = new Vector3(0f, -0.01f * H, 0.5f * L + g.Nose);
            for (int s = 0; s < 8; s++)
                for (int p = 0; p < 3; p++)
                    b.TriU(noseTip, b.V[stripVerts[s][0][p + 1]], b.V[stripVerts[s][0][p]], 1);
            var sternC = new Vector3(0f, 0f, -0.5f * L - 0.05f);
            for (int s = 0; s < 8; s++)
                for (int p = 0; p < 3; p++)
                    b.TriU(sternC, b.V[stripVerts[s][rings - 1][p]], b.V[stripVerts[s][rings - 1][p + 1]], 1);

            // Long dark-glass canopy — the manta's eye.
            {
                float tCan = g.CanopyStart + g.CanopyLen * 0.5f;
                float zC = (0.5f - tCan) * L;
                float halfLen = g.CanopyLen * L * 0.78f;
                System.Func<float, float> deckAt = z =>
                {
                    float t = Mathf.Clamp01(0.5f - z / L);
                    float sc = CrSample(cts, csc, t);
                    return HalfPtF(0, t).y * H * sc + CrSample(cts, clf, t) * H;
                };
                Canopy(b, zC + halfLen, zC - halfLen, 0.42f, 0.20f, deckAt, 3, 1, 1);
            }

            // Crescent wings: leading edge bows forward, trailing edge
            // scoops, tips curl up and sweep hard aft.
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                float z20 = (0.5f - 0.20f) * L;
                float z72 = (0.5f - 0.72f) * L;
                var tip = new Vector3(s * (W * 0.55f + g.WingSpan * 1.05f), 0.05f * H + g.WingCurl, z20 - g.WingSweep * 1.05f);
                var lead = new[]
                {
                    new Vector3(s * W * 0.55f, 0.05f * H, z20),
                    new Vector3(s * (W * 0.55f + g.WingSpan * 0.45f), 0.08f * H, z20 + 0.15f),
                    new Vector3(s * (W * 0.55f + g.WingSpan * 0.85f), 0.05f * H + g.WingCurl * 0.35f, z20 - g.WingSweep * 0.45f),
                    tip,
                };
                var leadT = new[] { 0f, 0.35f, 0.72f, 1f };
                var trail = new[]
                {
                    new Vector3(s * W * 0.60f, 0f, z72),
                    new Vector3(s * (W * 0.60f + g.WingSpan * 0.35f), 0.02f * H, z72 + 0.55f),
                    new Vector3(s * (W * 0.60f + g.WingSpan * 0.75f), 0.03f * H + g.WingCurl * 0.30f, z72 - g.WingSweep * 0.25f),
                    tip + new Vector3(-s * 0.06f, -0.02f, -0.10f),
                };
                var trailT = new[] { 0f, 0.40f, 0.75f, 1f };
                LoftWing(b, lead, leadT, trail, trailT, 0.14f, 0.020f, 10, side < 0, 1);
                WingPlate(b, lead, leadT, trail, trailT, 0.14f, 0.020f, 0.16f, 0.30f, 0);
                WingPlate(b, lead, leadT, trail, trailT, 0.14f, 0.020f, 0.44f, 0.56f, 0);
                WingPlate(b, lead, leadT, trail, trailT, 0.14f, 0.020f, 0.70f, 0.78f, 0);
            }

            // Small upswept rear fins over the engines.
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                var rootF2 = new Vector3(s * W * 0.32f, 0.12f * H, (0.5f - 0.78f) * L);
                var rootB2 = new Vector3(s * W * 0.30f, 0.10f * H, (0.5f - 0.90f) * L);
                var tipF2 = rootF2 + new Vector3(s * g.RearFinSize * 0.5f, g.RearFinSize * 0.8f, -g.RearFinSize * 0.9f);
                var tipB2 = rootB2 + new Vector3(s * g.RearFinSize * 0.42f, g.RearFinSize * 0.66f, -g.RearFinSize * 1.15f);
                var lead = new[] { rootF2, tipF2 };
                var trail = new[] { rootB2, tipB2 };
                var ts = new[] { 0f, 1f };
                LoftWing(b, lead, ts, trail, ts, 0.05f, 0.014f, 5, side < 0, 1);
            }

            // Three segmented lance guns: two chin, one keel — the turret
            // hardpoints made visible.
            for (int gun = 0; gun < 3; gun++)
            {
                float x = gun == 2 ? 0f : (gun == 0 ? -1f : 1f) * W * g.GunSpread;
                float y = gun == 2 ? -0.34f * H : -0.16f * H;
                float t0 = gun == 2 ? 0.30f : 0.18f;
                float len = g.GunLen * (gun == 2 ? 1.1f : 1f);
                var housing0 = new Vector3(x, y, (0.5f - t0 - 0.14f) * L);
                var housing1 = new Vector3(x, y, (0.5f - t0) * L);
                Tube(b, new[] { housing0, housing1 }, new[] { 0.11f, 0.095f }, 8, 1, false);
                Tube(b, new[] { housing1 - Vector3.forward * 0.05f, housing1 + Vector3.forward * 0.05f },
                    new[] { 0.115f, 0.115f }, 8, 2, false);
                const int gunSegs = 5;
                var path = new Vector3[gunSegs + 1];
                var radii = new float[gunSegs + 1];
                for (int i = 0; i <= gunSegs; i++)
                {
                    path[i] = new Vector3(x, y, (0.5f - t0) * L + i * (len * 0.6f / gunSegs));
                    radii[i] = i % 2 == 0 ? 0.070f : 0.052f;
                }
                Tube(b, path, radii, 8, 1, false);
                var tip0 = path[gunSegs];
                Tube(b, new[] { tip0, tip0 + Vector3.forward * (len * 0.4f) },
                    new[] { 0.035f, 0.005f }, 6, 1, true);
            }

            // Twin engine drums tucked high at the wing roots.
            for (int side = -1; side <= 1; side += 2)
            {
                var ec = new Vector3(side * W * 0.42f, 0.06f * H, -0.5f * L + 0.55f);
                int n = g.EngineSegs;
                var path = new Vector3[n + 1];
                var radii = new float[n + 1];
                for (int i = 0; i <= n; i++)
                {
                    path[i] = ec + Vector3.forward * (-i * 0.30f);
                    radii[i] = i % 2 == 0 ? 0.36f : 0.30f;
                }
                Tube(b, path, radii, 16, 1, false);
                for (int i = 1; i < n; i += 2)
                    Tube(b, new[] { path[i] + Vector3.forward * 0.04f, path[i] - Vector3.forward * 0.04f },
                        new[] { 0.38f, 0.38f }, 16, 0, false);
                var gc = path[n] + Vector3.forward * -0.03f;
                for (int k = 0; k < 16; k++)
                {
                    float a0 = k / 16f * Mathf.PI * 2f, a1 = (k + 1) / 16f * Mathf.PI * 2f;
                    b.TriUDS(gc,
                        gc + new Vector3(Mathf.Cos(a0) * 0.22f, Mathf.Sin(a0) * 0.22f, 0f),
                        gc + new Vector3(Mathf.Cos(a1) * 0.22f, Mathf.Sin(a1) * 0.22f, 0f), 2);
                }
            }

            // Glowing gill slits behind the canopy.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    float t = 0.40f + i * 0.05f;
                    float sc = CrSample(cts, csc, t);
                    var c = new Vector3(side * W * sc * 0.72f, 0.10f * H, (0.5f - t) * L);
                    Box(b, c, new Vector3(0.03f, 0.02f, 0.07f), 2);
                }
            }

            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "fin2_" + hash };
            mesh.SetVertices(b.V);
            mesh.subMeshCount = 4;
            for (int m = 0; m < 4; m++) mesh.SetTriangles(b.Sub[m], m);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.materials = new[]
            {
                Metal(g.White, 0.85f, 0.80f),
                Metal(g.Blue, 0.92f, 0.88f),
                SystemView.Mat(new Color(0.30f, 0.65f, 1f), true),
                Metal(new Color(0.03f, 0.04f, 0.06f), 0.9f, 0.95f),
            };
            return go;
        }
    }
}
