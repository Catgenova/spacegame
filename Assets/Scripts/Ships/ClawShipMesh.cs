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

        public static GameObject Build(string hash, int cls, Transform shipRoot)
            => cls == 3 ? BuildC3(hash, shipRoot)
             : cls == 2 ? BuildC2(hash, shipRoot) : BuildC1(hash, shipRoot);

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

        static Vector2 HalfPtUF(float k, float t)
        {
            float wMid = Smooth01(t / 0.34f);
            float wStern = Smooth01((t - 0.60f) / 0.40f);
            var v = Vector2.Lerp(ProfCR(NoseU, k), ProfCR(MidU, k), wMid);
            return Vector2.Lerp(v, ProfCR(SternU, k), wStern);
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
            if (lower && p >= 1 && tm > 0.35f && tm < 0.85f) return 1;

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
            // squat urchin shell: shorter, wider, and much taller than rolled
            float L = g.L * 0.80f, W = g.W * 1.12f, H = g.H * 1.30f;
            const int rings = 88;

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

            var stripVerts = HullLoft48(b, rings, HalfPtUF,
                t => CrSample(cts, csc, t)
                    * (1f + g.SurfAmp * 0.012f * Mathf.Sin((t * 6f + g.SurfPhase) * Mathf.PI * 2f)),
                t => CrSample(cts, clf, t) * H, t => (0.5f - t) * L, t => 1f, t => 1f, W, H,
                (so, po, tm, i) => PaintMatU(g, so, po, tm, panelCell, micro[i, so * 3 + po]));

            // Blunt gunmetal nose ring and stern cap.
            var noseTip = new Vector3(0f, 0f, 0.5f * L + g.Nose);
            CapFan(b, noseTip, stripVerts, 0, true, 1);
            var sternC = new Vector3(0f, 0f, -0.5f * L - 0.05f);
            CapFan(b, sternC, stripVerts, rings - 1, false, 1);

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
                    float len = g.SpikeLen * spikeLen[row, i] * 1.5f;
                    var sp = new Vector3[6];
                    var spr = new float[6];
                    for (int k2 = 0; k2 < 6; k2++)
                    {
                        sp[k2] = basePt + dir * (len * k2 / 5f);
                        spr[k2] = (k2 % 2 == 0 ? 0.095f : 0.070f) * (1f - k2 * 0.155f);
                    }
                    spr[5] = 0.006f;
                    Tube(b, sp, spr, 6, 1, true);
                    Tube(b, new[] { basePt - dir * 0.02f, basePt + dir * 0.09f },
                        new[] { 0.12f, 0.11f }, 6, 0, false);
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
                    radii[i] = (i % 2 == 0 ? 0.22f : 0.165f) * scale;
                }
                Tube(b, path, radii, 10, 1, false);
                for (int i = 1; i < armSegs; i += 2)
                    Tube(b, new[] { path[i] + Vector3.forward * 0.03f, path[i] - Vector3.forward * 0.03f },
                        new[] { 0.235f * scale, 0.235f * scale }, 10, 0, false);
                // ridged auger: stepped cone to a point
                var tipBase = path[armSegs];
                var auger = new Vector3[5];
                var augerR = new float[5];
                for (int i = 0; i < 5; i++)
                {
                    auger[i] = tipBase + Vector3.forward * (i * (g.DrillLen * 0.45f / 4f));
                    augerR[i] = (i % 2 == 0 ? 0.19f : 0.125f) * scale * (1f - i * 0.20f);
                }
                augerR[4] = 0.008f;
                Tube(b, auger, augerR, 8, 1, true);
                // amber collar where the arm meets the shell
                Tube(b, new[] { new Vector3(x, y, z0 - 0.02f), new Vector3(x, y, z0 + 0.06f) },
                    new[] { 0.245f * scale, 0.245f * scale }, 10, 2, false);
            }

            // Machinery greebles with amber running lights on the flanks.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    float t = 0.34f + i * 0.16f;
                    float sc = CrSample(cts, csc, t);
                    var c = new Vector3(side * W * sc * 0.88f, -0.05f * H, (0.5f - t) * L);
                    BevelBox(b, c, new Vector3(0.14f, 0.11f, 0.16f), 1);
                    BevelBox(b, c + new Vector3(side * 0.10f, 0.02f, 0f), new Vector3(0.05f, 0.03f, 0.08f), 2);
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
                Nozzle(b, gc, Vector3.back, 0.26f * 1.55f, 0.26f * 1.30f, 16, 1, 2);
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
                    Nozzle(b, gc2, Vector3.back, 0.10f * 1.55f, 0.10f * 1.30f, 10, 1, 2);
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

        // ================= CLASS 2 — "Lobster" =================
        // The ore barge, matched to its reference: long boxy industrial
        // hull with a blunt white cabin face and framed cockpit, two swept
        // antennae, machinery modules with running lights down both flanks,
        // stacked engine drum clusters astern — and the stars of the show,
        // two giant segmented claw arms ending in serrated open pincers.
        // Streams: claw2body / claw2panels.

        class GenomeL2
        {
            public float L, W, H, Nose, CanopyStart, CanopyLen;
            public float ArmLen, ArmSpread, ClawGape;
            public float AntLen, AntRake;
            public int EngineSegs;
            public float Hue, Sat, Val, BoneVal, PanelOdds, SurfAmp, SurfPhase;
            public int BandMode, BandCount;
            public float BandPhase;
            public Color Orange, Bone;
        }

        static GenomeL2 RollL2(string hash)
        {
            var rng = Rng.Stream("claw2body:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new GenomeL2();
            g.L = R(6.4f, 7.0f);
            g.W = R(1.25f, 1.45f);
            g.H = R(1.00f, 1.15f);
            g.Nose = R(0.25f, 0.40f);
            g.CanopyStart = R(0.08f, 0.12f);
            g.CanopyLen = R(0.16f, 0.20f);
            g.ArmLen = R(2.6f, 3.2f);
            g.ArmSpread = R(0.75f, 0.90f);
            g.ClawGape = R(0.28f, 0.40f);
            g.AntLen = R(2.8f, 3.6f);
            g.AntRake = R(0.50f, 0.70f);
            g.EngineSegs = 4 + rng.Next(2);
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

        // Boxy barge sections: blunt tall cabin face, square shoulders,
        // full belly.
        static readonly float[,] NoseL =
        {
            {0.00f, 0.52f}, {0.30f, 0.50f}, {0.55f, 0.44f}, {0.72f, 0.33f},
            {0.84f, 0.18f}, {0.90f, 0.00f}, {0.86f, -0.16f},
            {0.74f, -0.28f}, {0.55f, -0.36f}, {0.34f, -0.41f},
            {0.21f, -0.43f}, {0.08f, -0.45f}, {0.00f, -0.45f},
        };
        static readonly float[,] MidL =
        {
            {0.00f, 0.78f}, {0.38f, 0.76f}, {0.66f, 0.68f}, {0.84f, 0.52f},
            {0.95f, 0.28f}, {1.00f, 0.00f}, {0.96f, -0.26f},
            {0.84f, -0.48f}, {0.62f, -0.62f}, {0.40f, -0.70f},
            {0.25f, -0.73f}, {0.10f, -0.76f}, {0.00f, -0.77f},
        };
        static readonly float[,] SternL =
        {
            {0.00f, 0.68f}, {0.34f, 0.66f}, {0.60f, 0.58f}, {0.78f, 0.44f},
            {0.90f, 0.24f}, {0.96f, 0.00f}, {0.92f, -0.22f},
            {0.80f, -0.40f}, {0.60f, -0.52f}, {0.38f, -0.60f},
            {0.24f, -0.63f}, {0.09f, -0.66f}, {0.00f, -0.66f},
        };

        static Vector2 HalfPtL(int k, float t)
        {
            float wMid = Smooth01(t / 0.30f);
            float wStern = Smooth01((t - 0.62f) / 0.38f);
            float x = Mathf.Lerp(NoseL[k, 0], MidL[k, 0], wMid);
            float y = Mathf.Lerp(NoseL[k, 1], MidL[k, 1], wMid);
            x = Mathf.Lerp(x, SternL[k, 0], wStern);
            y = Mathf.Lerp(y, SternL[k, 1], wStern);
            return new Vector2(x, y);
        }

        static Vector2 LoopPtL(int li, float t)
        {
            if (li <= 12) return HalfPtL(li, t);
            var p = HalfPtL(LoopPts - li, t);
            return new Vector2(-p.x, p.y);
        }

        static Vector2 HalfPtLF(float k, float t)
        {
            float wMid = Smooth01(t / 0.30f);
            float wStern = Smooth01((t - 0.62f) / 0.38f);
            var v = Vector2.Lerp(ProfCR(NoseL, k), ProfCR(MidL, k), wMid);
            return Vector2.Lerp(v, ProfCR(SternL, k), wStern);
        }

        // Same industrial patchwork family as the Urchin, with a bone cabin
        // face up front.
        static int PaintMatL(GenomeL2 g, int s, int p, float tm, bool[] panelCell, bool microHit)
        {
            bool deck = s == 0 || s == 7;
            bool upper = s == 1 || s == 6;
            bool lower = s == 2 || s == 5;
            bool belly = s == 3 || s == 4;
            int side = s <= 3 ? 0 : 1;

            // Bone cabin face and saddle.
            if (!belly && tm < 0.20f) return 3;
            if (deck && tm > 0.20f && tm < 0.42f) return 3;

            // Gunmetal machinery girdle and belly.
            bool chineSpan = (s == 1 && p == 2) || (s == 2 && p == 0)
                || (s == 6 && p == 0) || (s == 5 && p == 2);
            if (chineSpan && tm > 0.24f && tm < 0.88f) return 1;
            if (belly && tm > 0.20f && tm < 0.88f) return 1;

            // Segmented module blocks aft of the cab: orange segments split
            // by gunmetal seams, with bone or dark modules rolled per hash.
            if (tm > 0.30f)
            {
                float ft = tm - 0.30f;
                int seg = (int)(ft / 0.13f);
                float fu = ft - seg * 0.13f;
                if (fu < 0.018f) return 1;
                if (panelCell[(side * 15 + seg * 3 + (upper ? 0 : 1)) % 30])
                    return seg % 2 == 0 ? 3 : 1;
                if (microHit) return 1;
                return 0;
            }

            if (microHit && tm > 0.20f) return 1;
            return 0;
        }

        static GameObject BuildC2(string hash, Transform shipRoot)
        {
            var g = RollL2(hash);
            var b = new Builder();
            float L = g.L, W = g.W, H = g.H;
            const int rings = 96;

            var panelRng = Rng.Stream("claw2panels:" + hash);
            var panelCell = new bool[30];
            for (int i = 0; i < 30; i++) panelCell[i] = panelRng.NextDouble() < g.PanelOdds;
            var micro = new bool[rings - 1, Spans];
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.03;

            // Blunt-nosed barge plan: swells fast, holds fat, tapers late.
            float[] cts = { 0.00f, 0.07f, 0.18f, 0.34f, 0.52f, 0.72f, 0.90f, 1.00f };
            float[] csc = { 0.42f, 0.62f, 0.82f, 0.96f, 1.00f, 0.94f, 0.76f, 0.55f };
            float[] clf = { 0.00f, 0.01f, 0.02f, 0.03f, 0.03f, 0.02f, 0.01f, 0.00f };

            var stripVerts = HullLoft48(b, rings, HalfPtLF,
                t => CrSample(cts, csc, t)
                    * (1f + g.SurfAmp * 0.012f * Mathf.Sin((t * 6f + g.SurfPhase) * Mathf.PI * 2f)),
                t => CrSample(cts, clf, t) * H, t => (0.5f - t) * L, t => 1f, t => 1f, W, H,
                (so, po, tm, i) => PaintMatL(g, so, po, tm, panelCell, micro[i, so * 3 + po]));

            // Blunt bone cabin cap and gunmetal stern cap.
            var noseTip = new Vector3(0f, 0f, 0.5f * L + g.Nose);
            CapFan(b, noseTip, stripVerts, 0, true, 3);
            var sternC = new Vector3(0f, 0f, -0.5f * L - 0.05f);
            CapFan(b, sternC, stripVerts, rings - 1, false, 1);

            // Framed cockpit band across the cabin face.
            {
                float tCan = g.CanopyStart + g.CanopyLen * 0.5f;
                float zC = (0.5f - tCan) * L;
                float halfLen = g.CanopyLen * L * 0.72f;
                System.Func<float, float> deckAt = z =>
                {
                    float t = Mathf.Clamp01(0.5f - z / L);
                    float sc = CrSample(cts, csc, t);
                    return HalfPtL(0, t).y * H * sc + CrSample(cts, clf, t) * H;
                };
                Canopy(b, zC + halfLen, zC - halfLen, 0.30f, 0.16f, deckAt, 2, 1, 1);
            }

            // Two swept antennae off the cabin roof.
            for (int side = -1; side <= 1; side += 2)
            {
                float scA = CrSample(cts, csc, 0.14f);
                float deckY = HalfPtL(0, 0.14f).y * H * scA + CrSample(cts, clf, 0.14f) * H;
                var mount = new Vector3(side * W * 0.14f, deckY - 0.02f, (0.5f - 0.14f) * L);
                var dir = new Vector3(side * 0.07f, g.AntRake * 0.6f, -0.80f).normalized;
                const int segsA = 5;
                var path = new Vector3[segsA + 1];
                var radii = new float[segsA + 1];
                for (int i = 0; i <= segsA; i++)
                {
                    float u = i / (float)segsA;
                    path[i] = mount + dir * (g.AntLen * u) + new Vector3(0f, 0.25f * u * u, 0f);
                    radii[i] = Mathf.Lerp(0.05f, 0.010f, u);
                }
                Tube(b, path, radii, 6, 1, true);
                Ball(b, mount, 0.09f, 1, 4, 8);
            }

            // Twin giant claw arms with serrated open pincers — the two
            // claw hardpoints made visible.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * W * g.ArmSpread;
                float y = -0.15f * H;
                // shoulder mount block
                var shoulder = new Vector3(x * 0.85f, y, (0.5f - 0.28f) * L);
                BevelBox(b, shoulder, new Vector3(0.26f, 0.23f, 0.30f), 1);
                // massive segmented arm reaching forward and slightly out/down
                var armDir = new Vector3(side * 0.16f, -0.06f, 0.98f).normalized;
                const int armSegs = 6;
                var path = new Vector3[armSegs + 1];
                var radii = new float[armSegs + 1];
                for (int i = 0; i <= armSegs; i++)
                {
                    path[i] = shoulder + armDir * (i * (g.ArmLen * 0.86f / armSegs));
                    radii[i] = i % 2 == 0 ? 0.30f : 0.245f;
                }
                Tube(b, path, radii, 10, 0, false);
                for (int i = 1; i < armSegs; i += 2)
                    Tube(b, new[] { path[i] - armDir * 0.05f, path[i] + armDir * 0.05f },
                        new[] { 0.32f, 0.32f }, 10, 1, false);
                // amber wrist collar
                var wrist = path[armSegs];
                Tube(b, new[] { wrist - armDir * 0.06f, wrist + armDir * 0.06f },
                    new[] { 0.33f, 0.33f }, 10, 2, false);
                // serrated pincer: two heavy jaws curving toward each other
                for (int jaw = -1; jaw <= 1; jaw += 2)
                {
                    var j0 = wrist + new Vector3(0f, jaw * 0.15f, 0.08f);
                    var j1 = j0 + new Vector3(side * 0.05f, jaw * g.ClawGape * 0.55f, g.ArmLen * 0.22f);
                    var j2 = j1 + new Vector3(side * 0.02f, -jaw * g.ClawGape * 0.15f, g.ArmLen * 0.20f);
                    var j3 = j2 + new Vector3(0f, -jaw * g.ClawGape * 0.28f, g.ArmLen * 0.10f);
                    Tube(b, new[] { j0, j1, j2, j3 }, new[] { 0.26f, 0.22f, 0.15f, 0.03f }, 8, 1, true);
                    // teeth along the inner edge, biting toward the other jaw
                    for (int tooth = 0; tooth < 5; tooth++)
                    {
                        float u = 0.18f + tooth * 0.18f;
                        var basePt = u < 0.5f
                            ? Vector3.Lerp(j1, j2, u * 2f)
                            : Vector3.Lerp(j2, j3, (u - 0.5f) * 2f);
                        var tDir = new Vector3(0f, -jaw, 0.10f).normalized;
                        Tube(b, new[] { basePt, basePt + tDir * 0.24f },
                            new[] { 0.07f, 0.008f }, 5, 1, true);
                    }
                }
            }

            // Machinery modules with running lights down both flanks.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float t = 0.30f + i * 0.15f;
                    float sc = CrSample(cts, csc, t);
                    var c = new Vector3(side * W * sc * 0.90f, 0.08f * H, (0.5f - t) * L);
                    BevelBox(b, c, new Vector3(0.15f, 0.13f, 0.20f), i % 2 == 0 ? 1 : 0);
                    BevelBox(b, c + new Vector3(side * 0.11f, -0.04f, 0f), new Vector3(0.05f, 0.03f, 0.09f), 2);
                }
            }

            // Stacked engine drum clusters astern: two per side.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 2; row++)
                {
                    var ec = new Vector3(side * W * 0.45f, (row == 0 ? 0.28f : -0.22f) * H, -0.5f * L + 0.30f);
                    int n = g.EngineSegs;
                    var path = new Vector3[n + 1];
                    var radii = new float[n + 1];
                    float rBase = row == 0 ? 0.26f : 0.30f;
                    for (int i = 0; i <= n; i++)
                    {
                        path[i] = ec + Vector3.forward * (-i * 0.26f);
                        radii[i] = i % 2 == 0 ? rBase : rBase * 0.84f;
                    }
                    Tube(b, path, radii, 12, 1, false);
                    for (int i = 1; i < n; i += 2)
                        Tube(b, new[] { path[i] + Vector3.forward * 0.03f, path[i] - Vector3.forward * 0.03f },
                            new[] { rBase + 0.02f, rBase + 0.02f }, 12, 0, false);
                    var gc = path[n] + Vector3.forward * -0.03f;
                    Nozzle(b, gc, Vector3.back, rBase * 0.6f * 1.55f, rBase * 0.6f * 1.30f, 12, 1, 2);
                }
            }

            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "claw2_" + hash };
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

        // ================= CLASS 3 — "Horseshoe" =================
        // The armored surveyor, matched to its reference: a huge domed
        // carapace in plated orange/bone/gunmetal patchwork over a dense
        // machinery underbelly strung with running lights, six stubby
        // articulated legs under the skirt, twin giant serrated claw arms
        // reaching forward (the two hardpoints), a short center auger, a
        // segmented telson tail spike, and stacked engine drums astern.
        // Streams: claw3body / claw3panels.

        class GenomeH3
        {
            public float L, W, H, Nose, CanopyStart, CanopyLen;
            public float ArmLen, ArmSpread, ArmDroop;
            public float TailLen, LegScale;
            public int EngineSegs;
            public float Hue, Sat, Val, BoneVal, PanelOdds, SurfAmp, SurfPhase;
            public int BandMode, BandCount;
            public float BandPhase;
            public float[] LegL, LegA;
            public Color Orange, Bone;
        }

        static GenomeH3 RollH3(string hash)
        {
            var rng = Rng.Stream("claw3body:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new GenomeH3();
            g.L = R(5.4f, 6.0f);
            g.W = R(1.90f, 2.10f);
            g.H = R(1.05f, 1.20f);
            g.Nose = R(0.15f, 0.25f);
            g.CanopyStart = R(0.06f, 0.10f);
            g.CanopyLen = R(0.12f, 0.16f);
            g.ArmLen = R(2.4f, 3.0f);
            g.ArmSpread = R(0.45f, 0.60f);
            g.ArmDroop = R(0.10f, 0.20f);
            g.TailLen = R(1.7f, 2.3f);
            g.LegScale = R(0.90f, 1.10f);
            g.EngineSegs = 4 + rng.Next(2);
            g.Hue = R(0.055f, 0.085f);
            g.Sat = R(0.75f, 0.90f);
            g.Val = R(0.75f, 0.90f);
            g.BoneVal = R(0.82f, 0.90f);
            g.PanelOdds = R(0.45f, 0.70f);
            g.SurfAmp = R(0f, 0.5f);
            g.SurfPhase = R(0f, 1f);
            g.BandMode = rng.Next(3);
            g.BandCount = 2 + rng.Next(3);
            g.BandPhase = R(0f, 1f);
            g.LegL = new float[6];
            g.LegA = new float[6];
            for (int i = 0; i < 6; i++) g.LegL[i] = R(0.90f, 1.10f);
            for (int i = 0; i < 6; i++) g.LegA[i] = R(-0.06f, 0.06f);
            g.Orange = Color.HSVToRGB(g.Hue, g.Sat, g.Val);
            g.Bone = new Color(g.BoneVal, g.BoneVal - 0.015f, g.BoneVal - 0.05f);
            return g;
        }

        // Domed carapace sections: high crown, flaring skirt, shallow keel.
        static readonly float[,] NoseH =
        {
            {0.00f, 0.30f}, {0.30f, 0.29f}, {0.55f, 0.26f}, {0.75f, 0.20f},
            {0.88f, 0.11f}, {0.95f, 0.00f}, {0.92f, -0.08f},
            {0.80f, -0.13f}, {0.60f, -0.16f}, {0.38f, -0.18f},
            {0.24f, -0.19f}, {0.10f, -0.20f}, {0.00f, -0.20f},
        };
        static readonly float[,] MidH =
        {
            {0.00f, 1.00f}, {0.34f, 0.96f}, {0.62f, 0.84f}, {0.82f, 0.64f},
            {0.94f, 0.40f}, {1.00f, 0.12f}, {0.97f, -0.12f},
            {0.88f, -0.28f}, {0.70f, -0.38f}, {0.46f, -0.44f},
            {0.28f, -0.46f}, {0.11f, -0.48f}, {0.00f, -0.48f},
        };
        static readonly float[,] SternH =
        {
            {0.00f, 0.80f}, {0.32f, 0.77f}, {0.58f, 0.68f}, {0.78f, 0.52f},
            {0.90f, 0.32f}, {0.96f, 0.08f}, {0.93f, -0.14f},
            {0.82f, -0.28f}, {0.64f, -0.36f}, {0.42f, -0.42f},
            {0.26f, -0.44f}, {0.10f, -0.46f}, {0.00f, -0.46f},
        };

        static Vector2 HalfPtH(int k, float t)
        {
            float wMid = Smooth01(t / 0.30f);
            float wStern = Smooth01((t - 0.60f) / 0.40f);
            float x = Mathf.Lerp(NoseH[k, 0], MidH[k, 0], wMid);
            float y = Mathf.Lerp(NoseH[k, 1], MidH[k, 1], wMid);
            x = Mathf.Lerp(x, SternH[k, 0], wStern);
            y = Mathf.Lerp(y, SternH[k, 1], wStern);
            return new Vector2(x, y);
        }

        static Vector2 LoopPtH(int li, float t)
        {
            if (li <= 12) return HalfPtH(li, t);
            var p = HalfPtH(LoopPts - li, t);
            return new Vector2(-p.x, p.y);
        }

        static Vector2 HalfPtHF(float k, float t)
        {
            float wMid = Smooth01(t / 0.30f);
            float wStern = Smooth01((t - 0.60f) / 0.40f);
            var v = Vector2.Lerp(ProfCR(NoseH, k), ProfCR(MidH, k), wMid);
            return Vector2.Lerp(v, ProfCR(SternH, k), wStern);
        }

        // Large armored plate patchwork over the dome; the belly is all
        // gunmetal machinery.
        static int PaintMatH(GenomeH3 g, int s, int p, float tm, bool[] panelCell, bool microHit)
        {
            bool deck = s == 0 || s == 7;
            bool upper = s == 1 || s == 6;
            bool lower = s == 2 || s == 5;
            bool belly = s == 3 || s == 4;
            int side = s <= 3 ? 0 : 1;

            // Gunmetal machinery face and full machinery belly.
            if (tm < 0.10f) return 1;
            if (belly) return 1;
            if (lower && p == 2) return 1;

            // Bone spine stripe down the crown.
            if (deck && p == 0 && tm > 0.12f && tm < 0.72f) return 3;

            // Big carapace plates: bone or gunmetal patches on orange.
            if (deck || upper || lower)
            {
                float skew = (deck ? 0f : upper ? 0.03f : 0.06f) + p * 0.02f;
                float ft = tm - 0.12f - skew;
                if (ft >= 0f && ft < 0.66f)
                {
                    int cell = Mathf.Min(4, (int)(ft / 0.133f));
                    int band = deck ? 0 : upper ? 1 : 2;
                    if (panelCell[(side * 15 + band * 5 + cell) % 30])
                        return cell % 2 == 0 ? 3 : 1;
                }
            }

            if (g.BandMode == 1 && tm > 0.74f && tm < 0.94f)
            {
                float u = (tm - 0.74f) / 0.20f;
                if ((int)(u * g.BandCount * 2 + g.BandPhase * 2f) % 2 == 0) return 1;
            }
            else if (g.BandMode == 2 && tm > 0.14f && tm < 0.30f)
            {
                float u = (tm - 0.14f) / 0.16f;
                if ((int)(u * g.BandCount * 2.5f + g.BandPhase * 2f) % 2 == 0) return 1;
            }

            if (microHit && tm > 0.10f && tm < 0.90f) return 1;
            return 0;
        }

        static GameObject BuildC3(string hash, Transform shipRoot)
        {
            var g = RollH3(hash);
            var b = new Builder();
            // horseshoe carapace: short, broad, and a tall domed crown
            float L = g.L * 0.80f, W = g.W * 1.05f, H = g.H * 1.55f;
            const int rings = 96;

            var panelRng = Rng.Stream("claw3panels:" + hash);
            var panelCell = new bool[30];
            for (int i = 0; i < 30; i++) panelCell[i] = panelRng.NextDouble() < g.PanelOdds;
            var micro = new bool[rings - 1, Spans];
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.03;

            // Broad blunt dome plan: widest just before midship.
            float[] cts = { 0.00f, 0.06f, 0.16f, 0.30f, 0.45f, 0.64f, 0.84f, 1.00f };
            float[] csc = { 0.36f, 0.60f, 0.82f, 0.96f, 1.00f, 0.94f, 0.76f, 0.55f };
            float[] clf = { 0.00f, 0.01f, 0.02f, 0.03f, 0.03f, 0.02f, 0.01f, 0.00f };

            var stripVerts = HullLoft48(b, rings, HalfPtHF,
                t => CrSample(cts, csc, t)
                    * (1f + g.SurfAmp * 0.012f * Mathf.Sin((t * 6f + g.SurfPhase) * Mathf.PI * 2f)),
                t => CrSample(cts, clf, t) * H, t => (0.5f - t) * L, t => 1f, t => 1f, W, H,
                (so, po, tm, i) => PaintMatH(g, so, po, tm, panelCell, micro[i, so * 3 + po]));

            // Gunmetal prow wedge and stern cap.
            var noseTip = new Vector3(0f, -0.02f * H, 0.5f * L + g.Nose);
            CapFan(b, noseTip, stripVerts, 0, true, 1);
            var sternC = new Vector3(0f, 0f, -0.5f * L - 0.05f);
            CapFan(b, sternC, stripVerts, rings - 1, false, 1);

            // Low crew cabin canopy on the front slope of the dome.
            {
                float tCan = g.CanopyStart + g.CanopyLen * 0.5f;
                float zC = (0.5f - tCan) * L;
                float halfLen = g.CanopyLen * L * 0.68f;
                System.Func<float, float> deckAt = z =>
                {
                    float t = Mathf.Clamp01(0.5f - z / L);
                    float sc = CrSample(cts, csc, t);
                    return HalfPtH(0, t).y * H * sc + CrSample(cts, clf, t) * H;
                };
                Canopy(b, zC + halfLen, zC - halfLen, 0.24f, 0.13f, deckAt, 2, 1, 1);
            }

            // Twin giant serrated claw arms — the two hardpoints.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * W * g.ArmSpread;
                float y = -0.18f * H;
                var shoulder = new Vector3(x, y, (0.5f - 0.14f) * L);
                BevelBox(b, shoulder, new Vector3(0.20f, 0.17f, 0.24f), 1);
                var armDir = new Vector3(side * 0.30f, -g.ArmDroop, 0.92f).normalized;
                const int armSegs = 7;
                var path = new Vector3[armSegs + 1];
                var radii = new float[armSegs + 1];
                for (int i = 0; i <= armSegs; i++)
                {
                    path[i] = shoulder + armDir * (i * (g.ArmLen * 0.72f / armSegs));
                    radii[i] = (i % 2 == 0 ? 0.19f : 0.155f) * (1f - i * 0.075f);
                }
                Tube(b, path, radii, 9, 0, false);
                for (int i = 1; i < armSegs; i += 2)
                    Tube(b, new[] { path[i] - armDir * 0.035f, path[i] + armDir * 0.035f },
                        new[] { radii[i] + 0.035f, radii[i] + 0.035f }, 9, 1, false);
                // amber base collar
                Tube(b, new[] { shoulder + armDir * 0.10f, shoulder + armDir * 0.20f },
                    new[] { 0.21f, 0.21f }, 9, 2, false);
                // serrated ridge spikes along the top of the arm
                for (int sp = 0; sp < 4; sp++)
                {
                    float u = 0.30f + sp * 0.18f;
                    var basePt = shoulder + armDir * (g.ArmLen * 0.72f * u);
                    Tube(b, new[] { basePt, basePt + new Vector3(0f, 0.16f, 0.04f) },
                        new[] { 0.05f, 0.006f }, 5, 1, true);
                }
                // long tapered point
                var tip0 = path[armSegs];
                Tube(b, new[] { tip0, tip0 + armDir * (g.ArmLen * 0.28f) },
                    new[] { 0.075f, 0.006f }, 7, 1, true);
            }

            // Short ridged center auger under the prow.
            {
                var a0 = new Vector3(0f, -0.26f * H, (0.5f - 0.06f) * L);
                var auger = new Vector3[5];
                var augerR = new float[5];
                for (int i = 0; i < 5; i++)
                {
                    auger[i] = a0 + Vector3.forward * (i * 0.26f);
                    augerR[i] = (i % 2 == 0 ? 0.14f : 0.10f) * (1f - i * 0.18f);
                }
                augerR[4] = 0.008f;
                Tube(b, auger, augerR, 8, 1, true);
            }

            // ---- folded rim lip: the carapace edge gets real thickness ----
            {
                System.Func<int, float, Vector3> surf = (li, t) =>
                {
                    float sc = CrSample(cts, csc, t);
                    var pt = LoopPtH(li, t);
                    return new Vector3(pt.x * W * sc,
                        pt.y * H * sc + CrSample(cts, clf, t) * H, (0.5f - t) * L);
                };
                const int rimSteps = 26;
                for (int side = -1; side <= 1; side += 2)
                {
                    int li = side > 0 ? 5 : 19;
                    for (int i = 0; i < rimSteps; i++)
                    {
                        float t0 = 0.06f + i * (0.86f / rimSteps);
                        float t1 = t0 + 0.86f / rimSteps;
                        var a = surf(li, t0);
                        var b2 = surf(li, t1);
                        var outA = a + new Vector3(side * 0.07f, 0.01f, 0f);
                        var outB = b2 + new Vector3(side * 0.07f, 0.01f, 0f);
                        var loA = outA + new Vector3(-side * 0.02f, -0.14f, 0f);
                        var loB = outB + new Vector3(-side * 0.02f, -0.14f, 0f);
                        b.QuadUDS(a, b2, outB, outA, 1);
                        b.QuadUDS(outA, outB, loB, loA, 0);
                        if (i % 4 == 0)
                            Ball(b, outA + new Vector3(0f, -0.035f, 0f), 0.032f, 1, 2, 5);
                    }
                }
            }

            // Eight long articulated legs under the skirt.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int leg = 0; leg < 4; leg++)
                {
                    int li = ((side < 0 ? 0 : 3) + leg) % 6;
                    float tm = 0.26f + leg * 0.17f;
                    float sc = CrSample(cts, csc, tm);
                    var mount = new Vector3(side * W * sc * 0.62f, -0.30f * H, (0.5f - tm) * L);
                    // thigh: reaches well out and forward from under the shell
                    var d1 = new Vector3(side * (1.05f + g.LegA[li]), -0.34f, 0.30f - leg * 0.14f).normalized;
                    float len1 = 1.05f * g.LegScale;
                    var j1 = mount + d1 * len1;
                    Fairing(b, mount - d1 * 0.02f, d1, 0.135f, 0.23f, 0.11f, 8, 0);
                    var thigh = new Vector3[4];
                    var thighR = new float[4];
                    for (int k2 = 0; k2 < 4; k2++)
                    {
                        thigh[k2] = mount + d1 * (len1 * k2 / 3f);
                        thighR[k2] = k2 % 2 == 0 ? 0.135f : 0.11f;
                    }
                    Tube(b, thigh, thighR, 8, 0, false);
                    Ball(b, j1, 0.145f, 1, 4, 8);
                    // shin: angles down and slightly back
                    var d2 = new Vector3(side * (0.74f + g.LegA[li]), -0.74f, 0.20f).normalized;
                    float len2 = 1.15f * g.LegL[li] * g.LegScale;
                    var j2 = j1 + d2 * len2;
                    var shin = new Vector3[4];
                    var shinR = new float[4];
                    for (int k2 = 0; k2 < 4; k2++)
                    {
                        shin[k2] = j1 + d2 * (len2 * k2 / 3f);
                        shinR[k2] = (k2 % 2 == 0 ? 0.118f : 0.094f) * (1f - k2 * 0.13f);
                    }
                    Tube(b, shin, shinR, 7, 1, false);
                    Ball(b, j2, 0.085f, 0, 3, 7);
                    // segmented claw tip
                    var d3 = new Vector3(side * 0.20f, -0.90f, -0.10f).normalized;
                    var claw = new Vector3[4];
                    var clawR = new float[4];
                    for (int k2 = 0; k2 < 4; k2++)
                    {
                        claw[k2] = j2 + d3 * (0.46f * g.LegScale * k2 / 3f);
                        clawR[k2] = (k2 % 2 == 0 ? 0.062f : 0.046f) * (1f - k2 * 0.28f);
                    }
                    clawR[3] = 0.006f;
                    Tube(b, claw, clawR, 6, 1, true);
                }
            }

            // Underbelly machinery: rows of gunmetal blocks and amber lights.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float t = 0.24f + i * 0.16f;
                    float sc = CrSample(cts, csc, t);
                    var c = new Vector3(side * W * sc * 0.34f, -0.40f * H, (0.5f - t) * L);
                    BevelBox(b, c, new Vector3(0.16f, 0.10f, 0.18f), 1);
                    BevelBox(b, c + new Vector3(0f, -0.09f, side * 0.05f), new Vector3(0.06f, 0.025f, 0.09f), 2);
                }
            }

            // Segmented telson tail spike over the engines.
            {
                var root = new Vector3(0f, 0.14f * H, -0.5f * L + 0.10f);
                const int segs = 5;
                var path = new Vector3[segs + 1];
                var radii = new float[segs + 1];
                for (int i = 0; i <= segs; i++)
                {
                    path[i] = root + new Vector3(0f, i * 0.015f, -i * (g.TailLen * 0.6f / segs));
                    radii[i] = (i % 2 == 0 ? 0.11f : 0.088f) * (1f - i * 0.10f);
                }
                Tube(b, path, radii, 8, 1, false);
                Tube(b, new[] { path[1] + Vector3.forward * 0.03f, path[1] - Vector3.forward * 0.03f },
                    new[] { 0.12f, 0.12f }, 8, 0, false);
                var tip0 = path[segs];
                Tube(b, new[] { tip0, tip0 + new Vector3(0f, 0.03f, -g.TailLen * 0.4f) },
                    new[] { 0.05f, 0.005f }, 6, 1, true);
            }

            // Stacked engine drums astern: two per side.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 2; row++)
                {
                    var ec = new Vector3(side * W * 0.40f, (row == 0 ? 0.16f : -0.20f) * H, -0.5f * L + 0.28f);
                    int n = g.EngineSegs;
                    var path = new Vector3[n + 1];
                    var radii = new float[n + 1];
                    float rBase = row == 0 ? 0.28f : 0.32f;
                    for (int i = 0; i <= n; i++)
                    {
                        path[i] = ec + Vector3.forward * (-i * 0.27f);
                        radii[i] = i % 2 == 0 ? rBase : rBase * 0.84f;
                    }
                    Tube(b, path, radii, 12, 1, false);
                    for (int i = 1; i < n; i += 2)
                        Tube(b, new[] { path[i] + Vector3.forward * 0.03f, path[i] - Vector3.forward * 0.03f },
                            new[] { rBase + 0.02f, rBase + 0.02f }, 12, 0, false);
                    var gc = path[n] + Vector3.forward * -0.03f;
                    Nozzle(b, gc, Vector3.back, rBase * 0.6f * 1.55f, rBase * 0.6f * 1.30f, 12, 1, 2);
                }
            }

            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "claw3_" + hash };
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
