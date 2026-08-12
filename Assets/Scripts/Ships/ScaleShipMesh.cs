using UnityEngine;
using static SpaceGame.MeshKit;

namespace SpaceGame
{
    /// <summary>
    /// Scale-class hull meshes — reptilian battleships. C1 "Python",
    /// matched to its reference: a long slab-sided serpent hull sheathed
    /// in overlapping scale shingles, charcoal armor broken by dark-red
    /// scale fields and bronze trim, twin segmented bronze fang prongs off
    /// the prow, a raised bridge block with glowing red windows, rows of
    /// red running lights down the flanks, a serpent spine ridge, plated
    /// stern fins, and a triple engine bank with red exhaust.
    /// Submeshes: 0 charcoal, 1 dark red, 2 red glow, 3 bronze.
    /// Streams: scalebody / scalepanels.
    /// </summary>
    public static class ScaleShipMesh
    {
        const int LoopPts = 24;
        const int Spans = 24;
        static readonly int[] StripStart = { 0, 3, 6, 9, 12, 15, 18, 21 };

        public static GameObject Build(string hash, int cls, Transform shipRoot)
            => BuildC1(hash, shipRoot); // single class so far

        class GenomeS1
        {
            public float L, W, H, Nose;
            public float FangLen;
            public int FangSegs;
            public float BridgeStart, BridgeLen, GunLen;
            public float FinSize, FinSweep;
            public int EngineSegs;
            public float Hue, Sat, Val, DarkVal, MarkOdds, SurfAmp, SurfPhase;
            public int BandMode, BandCount;
            public float BandPhase;
            public Color Red, Dark;
        }

        static GenomeS1 RollS1(string hash)
        {
            var rng = Rng.Stream("scalebody:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new GenomeS1();
            g.L = R(13.5f, 14.5f);
            g.W = R(1.60f, 1.80f);
            g.H = R(1.30f, 1.50f);
            g.Nose = R(0.8f, 1.2f);
            g.FangLen = R(2.6f, 3.2f);
            g.FangSegs = 4 + rng.Next(2);
            g.BridgeStart = R(0.16f, 0.20f);
            g.BridgeLen = R(0.14f, 0.18f);
            g.GunLen = R(1.5f, 1.9f);
            g.FinSize = R(1.1f, 1.5f);
            g.FinSweep = R(1.0f, 1.4f);
            g.EngineSegs = 3 + rng.Next(2);
            g.Hue = R(0.00f, 0.03f);
            g.Sat = R(0.70f, 0.85f);
            g.Val = R(0.30f, 0.42f);
            g.DarkVal = R(0.10f, 0.16f);
            g.MarkOdds = R(0.45f, 0.70f);
            g.SurfAmp = R(0f, 0.5f);
            g.SurfPhase = R(0f, 1f);
            g.BandMode = rng.Next(3);
            g.BandCount = 2 + rng.Next(3);
            g.BandPhase = R(0f, 1f);
            g.Red = Color.HSVToRGB(g.Hue, g.Sat, g.Val);
            g.Dark = new Color(g.DarkVal, g.DarkVal, g.DarkVal + 0.02f);
            return g;
        }

        // Battleship sections: sharp armored prow, slab-sided midship,
        // stepped stern.
        static readonly float[,] NoseS =
        {
            {0.00f, 0.30f}, {0.30f, 0.28f}, {0.55f, 0.24f}, {0.75f, 0.17f},
            {0.90f, 0.08f}, {0.98f, -0.02f}, {1.00f, -0.10f},
            {0.86f, -0.16f}, {0.64f, -0.21f}, {0.40f, -0.24f},
            {0.25f, -0.26f}, {0.10f, -0.27f}, {0.00f, -0.27f},
        };
        static readonly float[,] MidS =
        {
            {0.00f, 0.72f}, {0.34f, 0.70f}, {0.62f, 0.62f}, {0.82f, 0.45f},
            {0.94f, 0.22f}, {1.00f, -0.02f}, {0.96f, -0.26f},
            {0.84f, -0.44f}, {0.62f, -0.56f}, {0.40f, -0.63f},
            {0.25f, -0.66f}, {0.10f, -0.68f}, {0.00f, -0.69f},
        };
        static readonly float[,] SternS =
        {
            {0.00f, 0.62f}, {0.34f, 0.60f}, {0.60f, 0.53f}, {0.80f, 0.39f},
            {0.92f, 0.19f}, {0.98f, -0.02f}, {0.94f, -0.22f},
            {0.82f, -0.36f}, {0.60f, -0.46f}, {0.38f, -0.52f},
            {0.24f, -0.55f}, {0.10f, -0.57f}, {0.00f, -0.57f},
        };

        static Vector2 HalfPtS(int k, float t)
        {
            float wMid = Smooth01(t / 0.40f);
            float wStern = Smooth01((t - 0.62f) / 0.38f);
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

        // Serpent hide: charcoal base, dark-red scale fields alternating
        // like laid shingles on the flanks, red saddle patches on the
        // spine, bronze micro-patches, and bronze prow or stern banding.
        static int PaintMatS(GenomeS1 g, int s, int p, float tm, bool[] markCell, bool microHit)
        {
            bool deck = s == 0 || s == 7;
            bool upper = s == 1 || s == 6;
            bool lower = s == 2 || s == 5;
            int side = s <= 3 ? 0 : 1;

            if (microHit && tm > 0.06f && tm < 0.94f) return 3;

            // Alternating scale-field cells along the flanks.
            if ((upper || lower) && tm > 0.10f && tm < 0.88f)
            {
                int cell = Mathf.Min(4, (int)((tm - 0.10f) / 0.156f));
                int band = upper ? 0 : 1;
                if (markCell[(side * 15 + band * 5 + cell) % 30] && (cell + p + band) % 2 == 0)
                    return 1;
            }

            // Red saddle patches down the spine.
            if (deck && tm > 0.16f && tm < 0.80f)
            {
                int cell = Mathf.Min(4, (int)((tm - 0.16f) / 0.128f));
                if (markCell[(side * 15 + 10 + cell) % 30]) return 1;
            }

            // Band families: bronze stern rings or bronze prow chevrons.
            if (g.BandMode == 1 && tm > 0.84f && tm < 0.96f)
            {
                float u = (tm - 0.84f) / 0.12f;
                if ((int)(u * g.BandCount * 2 + g.BandPhase * 2f) % 2 == 0) return 3;
            }
            else if (g.BandMode == 2 && tm > 0.05f && tm < 0.15f)
            {
                float u = (tm - 0.05f) / 0.10f;
                if ((int)(u * g.BandCount * 2.5f + g.BandPhase * 2f) % 2 == 0) return 3;
            }

            return 0;
        }

        static GameObject BuildC1(string hash, Transform shipRoot)
        {
            var g = RollS1(hash);
            var b = new Builder();
            float L = g.L, W = g.W, H = g.H;
            const int rings = 56;

            var panelRng = Rng.Stream("scalepanels:" + hash);
            var markCell = new bool[30];
            for (int i = 0; i < 30; i++) markCell[i] = panelRng.NextDouble() < g.MarkOdds;
            var micro = new bool[rings - 1, Spans];
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.04;

            // Long serpent body: swells from the prow to a slab midship
            // and holds its depth almost to the stern.
            float[] cts = { 0.00f, 0.10f, 0.24f, 0.42f, 0.60f, 0.76f, 0.90f, 1.00f };
            float[] csc = { 0.10f, 0.38f, 0.72f, 0.95f, 1.00f, 0.92f, 0.80f, 0.62f };
            float[] clf = { -0.06f, -0.02f, 0.02f, 0.05f, 0.05f, 0.02f, -0.02f, -0.06f };

            System.Func<float, float> zAt = t => (0.52f - 1.04f * t) * L;
            System.Func<int, float, float> hullY = (k, t) =>
            {
                float sc2 = CrSample(cts, csc, t);
                return HalfPtS(k, t).y * H * sc2 + CrSample(cts, clf, t) * H;
            };
            System.Func<int, float, Vector3> surf = (li, t) =>
            {
                float sc2 = CrSample(cts, csc, t);
                float lift2 = CrSample(cts, clf, t) * H;
                var pt = LoopPtS(((li % LoopPts) + LoopPts) % LoopPts, t);
                return new Vector3(pt.x * W * sc2, pt.y * H * sc2 + lift2, zAt(t));
            };

            // ---- main hull loft ----
            var stripVerts = new int[8][][];
            var ringT = new float[rings];
            for (int s = 0; s < 8; s++) stripVerts[s] = new int[rings][];

            for (int j = 0; j < rings; j++)
            {
                float t = j / (float)(rings - 1);
                ringT[j] = t;
                float sc = CrSample(cts, csc, t);
                sc *= 1f + g.SurfAmp * 0.012f * Mathf.Sin((t * 7f + g.SurfPhase) * Mathf.PI * 2f);
                float lift = CrSample(cts, clf, t) * H;
                float z = zAt(t);
                for (int s = 0; s < 8; s++)
                {
                    stripVerts[s][j] = new int[4];
                    for (int p = 0; p < 4; p++)
                    {
                        int li = (StripStart[s] + p) % LoopPts;
                        var pt = LoopPtS(li, t);
                        stripVerts[s][j][p] = b.Add(new Vector3(pt.x * W * sc, pt.y * H * sc + lift, z));
                    }
                }
            }
            for (int i = 0; i < rings - 1; i++)
            {
                float tm = (ringT[i] + ringT[i + 1]) * 0.5f;
                for (int s = 0; s < 8; s++)
                    for (int p = 0; p < 3; p++)
                    {
                        int mat = PaintMatS(g, s, p, tm, markCell, micro[i, s * 3 + p]);
                        b.FaceQ(stripVerts[s][i][p], stripVerts[s][i + 1][p],
                            stripVerts[s][i + 1][p + 1], stripVerts[s][i][p + 1], mat);
                    }
            }

            // Armored prow point and stern cap.
            var prow = new Vector3(0f, CrSample(cts, clf, 0f) * H - 0.02f, 0.52f * L + g.Nose);
            for (int s = 0; s < 8; s++)
                for (int p = 0; p < 3; p++)
                    b.TriU(prow, b.V[stripVerts[s][0][p + 1]], b.V[stripVerts[s][0][p]], 0);
            var sternC = new Vector3(0f, CrSample(cts, clf, 1f) * H, -0.52f * L - 0.06f);
            for (int s = 0; s < 8; s++)
                for (int p = 0; p < 3; p++)
                    b.TriU(sternC, b.V[stripVerts[s][rings - 1][p]], b.V[stripVerts[s][rings - 1][p + 1]], 0);

            // ---- scale shingles: nine overlapping rows down each flank ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int r2 = 0; r2 < 9; r2++)
                {
                    float t0 = 0.14f + r2 * 0.08f;
                    float t1 = t0 + 0.05f;
                    for (int li = 1; li <= 4; li++)
                    {
                        int liA = side > 0 ? li : LoopPts - li;
                        int liB = side > 0 ? li + 1 : LoopPts - li - 1;
                        var a = surf(liA, t0); var b2 = surf(liB, t0);
                        var c = surf(liB, t1); var d = surf(liA, t1);
                        var raise = new Vector3(0f, 0.030f, 0f);
                        a += raise + new Vector3(a.x * 0.025f, 0f, 0f);
                        b2 += raise + new Vector3(b2.x * 0.025f, 0f, 0f);
                        c += raise + new Vector3(c.x * 0.025f, -0.04f, 0f);
                        d += raise + new Vector3(d.x * 0.025f, -0.04f, 0f);
                        int mat = (r2 + li) % 2 == 0 ? 1 : 0;
                        b.QuadUDS(a, b2, c, d, mat);
                        var apex = (c + d) * 0.5f + new Vector3(0f, -0.02f, -0.10f);
                        b.TriUDS(c, d, apex, (r2 + li) % 3 == 0 ? 3 : 1);
                    }
                }
            }

            // ---- twin segmented bronze fang prongs off the prow ----
            for (int side = -1; side <= 1; side += 2)
            {
                var basePt = new Vector3(side * 0.26f, hullY(11, 0.05f) + 0.05f, zAt(0.02f));
                var dir = new Vector3(side * 0.05f, -0.08f, 0.995f).normalized;
                float segLen = g.FangLen / g.FangSegs;
                for (int i = 0; i < g.FangSegs; i++)
                {
                    float r0 = Mathf.Lerp(0.10f, 0.025f, i / (float)g.FangSegs);
                    float r1 = Mathf.Lerp(0.10f, 0.025f, (i + 1) / (float)g.FangSegs);
                    var p0 = basePt + dir * (segLen * i);
                    var p1 = basePt + dir * (segLen * (i + 1));
                    Tube(b, new[] { p0, p1 }, new[] { r0, r1 }, 7, 0, i == g.FangSegs - 1);
                    if (i < g.FangSegs - 1)
                        Tube(b, new[] { p1 - dir * 0.04f, p1 + dir * 0.04f },
                            new[] { r1 + 0.02f, r1 + 0.02f }, 7, 3, false);
                }
            }

            // ---- two dorsal gun mounts amidships ----
            for (int side = -1; side <= 1; side += 2)
            {
                float t0 = 0.34f;
                float z = zAt(t0);
                float y0 = hullY(1, t0);
                var mount = new Vector3(side * 0.30f, y0 + 0.05f, z);
                Box(b, mount, new Vector3(0.12f, 0.07f, 0.17f), 0);
                var gunBase = mount + new Vector3(0f, 0.10f, 0.10f);
                var muzzle = gunBase + new Vector3(0f, 0.02f, g.GunLen);
                Tube(b, new[] { gunBase, muzzle }, new[] { 0.065f, 0.055f }, 8, 0, false);
                Tube(b, new[] { muzzle, muzzle + Vector3.forward * 0.12f },
                    new[] { 0.062f, 0.056f }, 8, 3, true);
            }

            // ---- raised bridge block with glowing red windows ----
            {
                float t0 = g.BridgeStart;
                float zc = zAt(t0 + g.BridgeLen * 0.5f);
                float y0 = hullY(0, t0 + g.BridgeLen * 0.5f);
                float halfLen = g.BridgeLen * L * 0.52f;
                Box(b, new Vector3(0f, y0 + 0.13f, zc), new Vector3(0.36f, 0.14f, halfLen), 0);
                Box(b, new Vector3(0f, y0 + 0.31f, zc - 0.10f), new Vector3(0.24f, 0.09f, halfLen * 0.62f), 1);
                Box(b, new Vector3(0f, y0 + 0.30f, zc - 0.10f + halfLen * 0.62f + 0.02f),
                    new Vector3(0.16f, 0.035f, 0.02f), 2);
                for (int side = -1; side <= 1; side += 2)
                    Box(b, new Vector3(side * 0.37f, y0 + 0.13f, zc),
                        new Vector3(0.012f, 0.030f, halfLen * 0.7f), 2);
                var mast = new Vector3(0f, y0 + 0.40f, zc - halfLen * 0.4f);
                Tube(b, new[] { mast, mast + new Vector3(0f, 0.45f, -0.10f) },
                    new[] { 0.030f, 0.006f }, 5, 3, true);
            }

            // ---- red running lights down both flanks ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 6; i++)
                {
                    float t = 0.24f + i * 0.10f;
                    float sc = CrSample(cts, csc, t);
                    float lift = CrSample(cts, clf, t) * H;
                    var c = new Vector3(side * W * sc * 0.99f, -0.05f * H + lift, zAt(t));
                    Box(b, c, new Vector3(0.025f, 0.035f, 0.10f), 2);
                }
            }

            // ---- serpent spine ridge plates ----
            for (int r2 = 0; r2 < 8; r2++)
            {
                float t0 = 0.18f + r2 * 0.08f;
                float z = zAt(t0);
                float y0 = hullY(0, t0);
                var a = new Vector3(0f, y0 + 0.01f, z + 0.12f);
                var apex = new Vector3(0f, y0 + 0.15f, z - 0.02f);
                var c = new Vector3(0f, y0 + 0.01f, z - 0.16f);
                b.TriUDS(a, apex, c, r2 % 3 == 0 ? 3 : 1);
            }

            // ---- plated stern fins: dorsal blade + two angled per side ----
            {
                float y0 = hullY(0, 0.86f);
                var rootF = new Vector3(0f, y0 - 0.02f, zAt(0.84f));
                var rootB = new Vector3(0f, y0 - 0.02f, zAt(0.95f));
                var tipF = new Vector3(0f, y0 + g.FinSize, zAt(0.84f) - g.FinSweep);
                var tipB = new Vector3(0f, y0 + g.FinSize * 0.85f, zAt(0.95f) - g.FinSweep * 1.1f);
                var lead = new[] { rootF, tipF };
                var trail = new[] { rootB, tipB };
                var ts = new[] { 0f, 1f };
                LoftWing(b, lead, ts, trail, ts, 0.06f, 0.015f, 5, true, 1);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                for (int f = 0; f < 2; f++)
                {
                    float t0 = 0.84f + f * 0.06f;
                    float size = g.FinSize * (1f - f * 0.25f);
                    float lift = CrSample(cts, clf, t0) * H;
                    var rootF = new Vector3(s * W * 0.50f, 0.05f * H + lift, zAt(t0));
                    var rootB = rootF + new Vector3(-s * 0.02f, -0.02f, -0.50f + f * 0.10f);
                    var tipF = rootF + new Vector3(s * size, size * 0.45f, -g.FinSweep);
                    var tipB = rootB + new Vector3(s * size * 0.92f, size * 0.42f, -g.FinSweep * 1.08f);
                    LoftWing(b, new[] { rootF, tipF }, new[] { 0f, 1f },
                        new[] { rootB, tipB }, new[] { 0f, 1f }, 0.06f, 0.015f, 5, side < 0, 1);
                }
            }

            // ---- stepped armor slabs stacked on the stern quarters ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    float lift = CrSample(cts, clf, 0.90f) * H;
                    var c = new Vector3(side * (W * 0.58f + i * 0.10f),
                        0.08f * H + lift - i * 0.02f, zAt(0.90f) - i * 0.06f);
                    Box(b, c, new Vector3(0.08f, 0.18f - i * 0.04f, 0.50f - i * 0.10f), i % 2 == 0 ? 1 : 0);
                }
            }

            // ---- triple engine bank with red exhaust ----
            {
                float lift = CrSample(cts, clf, 0.97f) * H;
                float[] xs = { -0.42f, 0f, 0.42f };
                float[] rs = { 0.18f, 0.22f, 0.18f };
                for (int e = 0; e < 3; e++)
                {
                    var ec = new Vector3(xs[e] * W, 0.0f * H + lift, zAt(0.96f));
                    Tube(b, new[] { ec, ec - Vector3.forward * 0.55f },
                        new[] { rs[e], rs[e] }, 8, 0, false);
                    Tube(b, new[] { ec - Vector3.forward * 0.55f, ec - Vector3.forward * 0.63f },
                        new[] { rs[e] - 0.02f, rs[e] - 0.03f }, 8, 2, true);
                    Tube(b, new[] { ec + Vector3.forward * 0.02f, ec + Vector3.forward * 0.10f },
                        new[] { rs[e] + 0.02f, rs[e] + 0.02f }, 8, 3, false);
                }
                int n = g.EngineSegs;
                for (int i = 0; i < n; i++)
                {
                    float t = 0.90f - i * 0.03f;
                    Tube(b, new[] { new Vector3(0f, hullY(12, t) - 0.02f, zAt(t) + 0.06f),
                            new Vector3(0f, hullY(12, t) - 0.02f, zAt(t) - 0.06f) },
                        new[] { 0.10f, 0.10f }, 7, i % 2 == 0 ? 3 : 0, false);
                }
            }

            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "scale1_" + hash };
            mesh.SetVertices(b.V);
            mesh.subMeshCount = 4;
            for (int m = 0; m < 4; m++) mesh.SetTriangles(b.Sub[m], m);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.materials = new[]
            {
                Metal(g.Dark, 0.90f, 0.80f),
                Metal(g.Red, 0.88f, 0.72f),
                SystemView.Mat(new Color(1.0f, 0.28f, 0.16f), true),
                Metal(new Color(0.55f, 0.40f, 0.20f), 0.85f, 0.70f),
            };
            return go;
        }
    }
}
