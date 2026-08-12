using UnityEngine;
using static SpaceGame.MeshKit;

namespace SpaceGame
{
    /// <summary>
    /// Claw-class hull meshes — industrial urchin miners. C1 "Urchin",
    /// matched to its reference: a round spiked shell in workhorse orange
    /// with bone-white saddle panels and gunmetal machinery bands, a small
    /// glass cockpit up front, three segmented drill arms with ridged auger
    /// tips, spike rakes over the shell, machinery greebles with amber
    /// running lights, and a fat engine cluster astern.
    /// Submeshes: 0 orange metal, 1 gunmetal, 2 amber glow, 3 bone white.
    /// Streams: clawbody / clawpanels.
    /// </summary>
    public static class ClawShipMesh
    {
        const int LoopPts = 24;
        const int Spans = 24;
        static readonly int[] StripStart = { 0, 3, 6, 9, 12, 15, 18, 21 };

        public static GameObject Build(string hash, int cls, Transform shipRoot)
            => BuildC1(hash, shipRoot); // single class so far

        class GenomeU1
        {
            public float L, W, H, Nose, CanopyStart, CanopyLen;
            public float DrillLen, DrillSpread;
            public float SpikeLen;
            public int SpikeRing;
            public int EngineSegs;
            public float Hue, Sat, Val, BoneVal, PanelOdds, SurfAmp, SurfPhase;
            public int BandMode, BandCount;
            public float BandPhase;
            public Color Orange, Bone;
        }

        static GenomeU1 RollU1(string hash)
        {
            var rng = Rng.Stream("clawbody:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new GenomeU1();
            g.L = R(4.6f, 5.2f);
            g.W = R(1.15f, 1.35f);
            g.H = R(1.05f, 1.20f);
            g.Nose = R(0.20f, 0.35f);
            g.CanopyStart = R(0.10f, 0.15f);
            g.CanopyLen = R(0.14f, 0.18f);
            g.DrillLen = R(1.8f, 2.4f);
            g.DrillSpread = R(0.30f, 0.40f);
            g.SpikeLen = R(0.50f, 0.80f);
            g.SpikeRing = 5 + rng.Next(3);
            g.EngineSegs = 3 + rng.Next(2);
            g.Hue = R(0.055f, 0.085f);
            g.Sat = R(0.75f, 0.90f);
            g.Val = R(0.75f, 0.90f);
            g.BoneVal = R(0.82f, 0.90f);
            g.PanelOdds = R(0.40f, 0.65f);
            g.SurfAmp = R(0f, 0.5f);
            g.SurfPhase = R(0f, 1f);
            g.BandMode = rng.Next(3);
            g.BandCount = 2 + rng.Next(3);
            g.BandPhase = R(0f, 1f);
            g.Orange = Color.HSVToRGB(g.Hue, g.Sat, g.Val);
            g.Bone = new Color(g.BoneVal, g.BoneVal - 0.015f, g.BoneVal - 0.05f);
            return g;
        }

        // Round urchin sections: near-circular, slightly flattened keel.
        static readonly float[,] NoseU =
        {
            {0.00f, 0.46f}, {0.24f, 0.44f}, {0.46f, 0.38f}, {0.64f, 0.28f},
            {0.78f, 0.15f}, {0.85f, 0.00f}, {0.82f, -0.14f},
            {0.70f, -0.27f}, {0.52f, -0.36f}, {0.32f, -0.42f},
            {0.20f, -0.44f}, {0.08f, -0.46f}, {0.00f, -0.46f},
        };
        static readonly float[,] MidU =
        {
            {0.00f, 1.00f}, {0.30f, 0.95f}, {0.56f, 0.82f}, {0.76f, 0.62f},
            {0.91f, 0.36f}, {1.00f, 0.06f}, {0.97f, -0.24f},
            {0.85f, -0.50f}, {0.65f, -0.70f}, {0.42f, -0.83f},
            {0.27f, -0.88f}, {0.10f, -0.92f}, {0.00f, -0.93f},
        };
        static readonly float[,] SternU =
        {
            {0.00f, 0.78f}, {0.28f, 0.75f}, {0.52f, 0.66f}, {0.70f, 0.50f},
            {0.84f, 0.28f}, {0.92f, 0.04f}, {0.89f, -0.20f},
            {0.78f, -0.40f}, {0.60f, -0.55f}, {0.38f, -0.65f},
            {0.24f, -0.69f}, {0.09f, -0.72f}, {0.00f, -0.73f},
        };

        static Vector2 HalfPtU(int k, float t)
        {
            float wMid = Smooth01(t / 0.34f);
            float wStern = Smooth01((t - 0.60f) / 0.40f);
            float x = Mathf.Lerp(NoseU[k, 0], MidU[k, 0], wMid);
            float y = Mathf.Lerp(NoseU[k, 1], MidU[k, 1], wMid);
            x = Mathf.Lerp(x, SternU[k, 0], wStern);
            y = Mathf.Lerp(y, SternU[k, 1], wStern);
            return new Vector2(x, y);
        }

        static Vector2 LoopPtU(int li, float t)
        {
            if (li <= 12) return HalfPtU(li, t);
            var p = HalfPtU(LoopPts - li, t);
            return new Vector2(-p.x, p.y);
        }

        // Industrial patchwork: orange shell, bone saddle over the cockpit
        // spine, gunmetal machinery girdle and belly, banded accents.
        static int PaintMatU(GenomeU1 g, int s, int p, float tm, bool[] panelCell, bool microHit)
        {
            bool deck = s == 0 || s == 7;
            bool upper = s == 1 || s == 6;
            bool lower = s == 2 || s == 5;
            bool belly = s == 3 || s == 4;
            int side = s <= 3 ? 0 : 1;

            // Gunmetal snout ring where the drills emerge.
            if (tm < 0.08f) return 1;

            // Bone saddle running back from the cockpit.
            if (deck && tm > 0.08f && tm < 0.45f) return 3;
            if (upper && p == 0 && tm > 0.10f && tm < 0.40f) return 3;

            // Gunmetal machinery girdle along the widest line and belly.
            bool chineSpan = (s == 1 && p == 2) || (s == 2 && p == 0)
                || (s == 6 && p == 0) || (s == 5 && p == 2);
            if (chineSpan && tm > 0.20f && tm < 0.85f) return 1;
            if (belly && tm > 0.25f && tm < 0.85f) return 1;

            // Patchwork panels: bone or gunmetal patches on the orange shell.
            if (upper || lower)
            {
                float skew = (upper ? 0f : 0.04f) + p * 0.02f;
                float ft = tm - 0.14f - skew;
                if (ft >= 0f && ft < 0.60f)
                {
                    int cell = Mathf.Min(4, (int)(ft / 0.121f));
                    int band = upper ? 0 : 1;
                    if (panelCell[(side * 15 + band * 5 + cell) % 30])
                        return cell % 2 == 0 ? 3 : 1;
                }
            }

            // Band families: gunmetal rings.
            if (g.BandMode == 1 && tm > 0.72f && tm < 0.92f)
            {
                float u = (tm - 0.72f) / 0.20f;
                if ((int)(u * g.BandCount * 2 + g.BandPhase * 2f) % 2 == 0) return 1;
            }
            else if (g.BandMode == 2 && tm > 0.10f && tm < 0.26f)
            {
                float u = (tm - 0.10f) / 0.16f;
                if ((int)(u * g.BandCount * 2.5f + g.BandPhase * 2f) % 2 == 0) return 1;
            }

            if (microHit && tm > 0.10f && tm < 0.90f) return 1;
            return 0;
        }

        static GameObject BuildC1(string hash, Transform shipRoot)
        {
            var g = RollU1(hash);
            var b = new Builder();
            float L = g.L, W = g.W, H = g.H;
            const int rings = 44;

            var panelRng = Rng.Stream("clawpanels:" + hash);
            var panelCell = new bool[30];
            for (int i = 0; i < 30; i++) panelCell[i] = panelRng.NextDouble() < g.PanelOdds;
            var micro = new bool[rings - 1, Spans];
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.03;
            // spike rakes: three latitude rows, jittered angles and lengths
            const int spikeRows = 3;
            int perRow = g.SpikeRing;
            var spikeJit = new float[spikeRows, 8];
            var spikeLen = new float[spikeRows, 8];
            for (int r2 = 0; r2 < spikeRows; r2++)
                for (int i = 0; i < 8; i++)
                {
                    spikeJit[r2, i] = (float)panelRng.NextDouble();
                    spikeLen[r2, i] = 0.7f + (float)panelRng.NextDouble() * 0.6f;
                }

            // Bulbous plan: swells fast, fat through the middle.
            float[] cts = { 0.00f, 0.08f, 0.22f, 0.40f, 0.58f, 0.76f, 0.90f, 1.00f };
            float[] csc = { 0.30f, 0.58f, 0.85f, 1.00f, 0.98f, 0.84f, 0.60f, 0.40f };
            float[] clf = { 0.00f, 0.01f, 0.02f, 0.03f, 0.03f, 0.02f, 0.01f, 0.00f };

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
                        var pt = LoopPtU(li, t);
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
                        int mat = PaintMatU(g, s, p, tm, panelCell, micro[i, s * 3 + p]);
                        b.FaceQ(stripVerts[s][i][p], stripVerts[s][i + 1][p],
                            stripVerts[s][i + 1][p + 1], stripVerts[s][i][p + 1], mat);
                    }
            }

            // Blunt gunmetal nose ring and stern cap.
            var noseTip = new Vector3(0f, 0f, 0.5f * L + g.Nose);
            for (int s = 0; s < 8; s++)
                for (int p = 0; p < 3; p++)
                    b.TriU(noseTip, b.V[stripVerts[s][0][p + 1]], b.V[stripVerts[s][0][p]], 1);
            var sternC = new Vector3(0f, 0f, -0.5f * L - 0.05f);
            for (int s = 0; s < 8; s++)
                for (int p = 0; p < 3; p++)
                    b.TriU(sternC, b.V[stripVerts[s][rings - 1][p]], b.V[stripVerts[s][rings - 1][p + 1]], 1);

            // Small glass cockpit high on the front shell.
            {
                float tCan = g.CanopyStart + g.CanopyLen * 0.5f;
                float zC = (0.5f - tCan) * L;
                float halfLen = g.CanopyLen * L * 0.70f;
                System.Func<float, float> deckAt = z =>
                {
                    float t = Mathf.Clamp01(0.5f - z / L);
                    float sc = CrSample(cts, csc, t);
                    return HalfPtU(0, t).y * H * sc + CrSample(cts, clf, t) * H;
                };
                Canopy(b, zC + halfLen, zC - halfLen, 0.26f, 0.15f, deckAt, 2, 1, 1);
            }

            // Spike rakes: three latitude rows around the shell.
            for (int row = 0; row < spikeRows; row++)
            {
                float t = 0.28f + row * 0.20f;
                float sc = CrSample(cts, csc, t);
                float lift = CrSample(cts, clf, t) * H;
                float z = (0.5f - t) * L;
                for (int i = 0; i < perRow; i++)
                {
                    // upper hemisphere fan, jittered; skip the keel
                    float a = Mathf.PI * (0.10f + 0.80f * (i + spikeJit[row, i]) / perRow);
                    var basePt = new Vector3(Mathf.Cos(a) * W * sc, Mathf.Sin(a) * H * sc * 0.95f + lift, z);
                    // ellipsoid-ish outward normal, tilted away from midship
                    var dir = new Vector3(basePt.x / (W * W), (basePt.y - lift) / (H * H), (t - 0.5f) * -0.35f).normalized;
                    float len = g.SpikeLen * spikeLen[row, i];
                    Tube(b, new[] { basePt, basePt + dir * (len * 0.4f), basePt + dir * len },
                        new[] { 0.085f, 0.045f, 0.006f }, 6, 1, true);
                    Tube(b, new[] { basePt - dir * 0.02f, basePt + dir * 0.08f },
                        new[] { 0.11f, 0.10f }, 6, 0, false);
                }
            }

            // Three segmented drill arms with ridged auger tips — the claw
            // hardpoint made visible.
            for (int d = 0; d < 3; d++)
            {
                float x = d == 0 ? 0f : (d == 1 ? -1f : 1f) * W * g.DrillSpread;
                float y = d == 0 ? -0.10f * H : -0.30f * H;
                float scale = d == 0 ? 1f : 0.72f;
                float z0 = 0.5f * L - 0.15f;
                const int armSegs = 5;
                var path = new Vector3[armSegs + 1];
                var radii = new float[armSegs + 1];
                for (int i = 0; i <= armSegs; i++)
                {
                    path[i] = new Vector3(x, y, z0 + i * (g.DrillLen * 0.55f / armSegs));
                    radii[i] = (i % 2 == 0 ? 0.17f : 0.135f) * scale;
                }
                Tube(b, path, radii, 10, 1, false);
                for (int i = 1; i < armSegs; i += 2)
                    Tube(b, new[] { path[i] + Vector3.forward * 0.03f, path[i] - Vector3.forward * 0.03f },
                        new[] { 0.18f * scale, 0.18f * scale }, 10, 0, false);
                // ridged auger: stepped cone to a point
                var tipBase = path[armSegs];
                var auger = new Vector3[5];
                var augerR = new float[5];
                for (int i = 0; i < 5; i++)
                {
                    auger[i] = tipBase + Vector3.forward * (i * (g.DrillLen * 0.45f / 4f));
                    augerR[i] = (i % 2 == 0 ? 0.15f : 0.10f) * scale * (1f - i * 0.20f);
                }
                augerR[4] = 0.008f;
                Tube(b, auger, augerR, 8, 1, true);
                // amber collar where the arm meets the shell
                Tube(b, new[] { new Vector3(x, y, z0 - 0.02f), new Vector3(x, y, z0 + 0.06f) },
                    new[] { 0.19f * scale, 0.19f * scale }, 10, 2, false);
            }

            // Machinery greebles with amber running lights on the flanks.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    float t = 0.34f + i * 0.16f;
                    float sc = CrSample(cts, csc, t);
                    var c = new Vector3(side * W * sc * 0.88f, -0.05f * H, (0.5f - t) * L);
                    Box(b, c, new Vector3(0.14f, 0.11f, 0.16f), 1);
                    Box(b, c + new Vector3(side * 0.10f, 0.02f, 0f), new Vector3(0.05f, 0.03f, 0.08f), 2);
                }
            }

            // Fat engine cluster: one big drum plus two small outriggers.
            {
                var ec = new Vector3(0f, 0f, -0.5f * L + 0.20f);
                int n = g.EngineSegs;
                var path = new Vector3[n + 1];
                var radii = new float[n + 1];
                for (int i = 0; i <= n; i++)
                {
                    path[i] = ec + Vector3.forward * (-i * 0.30f);
                    radii[i] = i % 2 == 0 ? 0.42f : 0.35f;
                }
                Tube(b, path, radii, 16, 1, false);
                for (int i = 1; i < n; i += 2)
                    Tube(b, new[] { path[i] + Vector3.forward * 0.04f, path[i] - Vector3.forward * 0.04f },
                        new[] { 0.44f, 0.44f }, 16, 0, false);
                var gc = path[n] + Vector3.forward * -0.03f;
                for (int k = 0; k < 16; k++)
                {
                    float a0 = k / 16f * Mathf.PI * 2f, a1 = (k + 1) / 16f * Mathf.PI * 2f;
                    b.TriUDS(gc,
                        gc + new Vector3(Mathf.Cos(a0) * 0.26f, Mathf.Sin(a0) * 0.26f, 0f),
                        gc + new Vector3(Mathf.Cos(a1) * 0.26f, Mathf.Sin(a1) * 0.26f, 0f), 2);
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    var oc = new Vector3(side * W * 0.55f, -0.12f * H, -0.5f * L + 0.30f);
                    var p2 = new Vector3[3];
                    var r2 = new float[3];
                    for (int i = 0; i < 3; i++)
                    {
                        p2[i] = oc + Vector3.forward * (-i * 0.24f);
                        r2[i] = i % 2 == 0 ? 0.18f : 0.15f;
                    }
                    Tube(b, p2, r2, 10, 1, false);
                    var gc2 = p2[2] + Vector3.forward * -0.02f;
                    for (int k = 0; k < 10; k++)
                    {
                        float a0 = k / 10f * Mathf.PI * 2f, a1 = (k + 1) / 10f * Mathf.PI * 2f;
                        b.TriUDS(gc2,
                            gc2 + new Vector3(Mathf.Cos(a0) * 0.10f, Mathf.Sin(a0) * 0.10f, 0f),
                            gc2 + new Vector3(Mathf.Cos(a1) * 0.10f, Mathf.Sin(a1) * 0.10f, 0f), 2);
                    }
                }
            }

            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "claw1_" + hash };
            mesh.SetVertices(b.V);
            mesh.subMeshCount = 4;
            for (int m = 0; m < 4; m++) mesh.SetTriangles(b.Sub[m], m);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.materials = new[]
            {
                Metal(g.Orange, 0.80f, 0.65f),
                Metal(new Color(0.16f, 0.15f, 0.14f), 0.90f, 0.60f),
                SystemView.Mat(new Color(1f, 0.62f, 0.25f), true),
                Metal(g.Bone, 0.75f, 0.70f),
            };
            return go;
        }
    }
}
