using UnityEngine;
using static SpaceGame.MeshKit;

namespace SpaceGame
{
    /// <summary>
    /// Pack-class hull meshes — camel long-range freighters. C1
    /// "Bactrian", matched to its reference: a boxy sand-and-olive hull
    /// with a raked train-nose cockpit, two tiered cargo humps on the
    /// back crowned with amber beacons, container modules hung on the
    /// flanks, camel-leg landing struts underneath, amber running lights,
    /// one modest dorsal gun, a stubby tail fin, and twin boxy engines.
    /// Submeshes: 0 sand, 1 olive, 2 amber glow, 3 dark glass.
    /// Streams: packbody / packpanels.
    /// </summary>
    public static class PackShipMesh
    {
        const int LoopPts = 24;
        const int Spans = 24;
        static readonly int[] StripStart = { 0, 3, 6, 9, 12, 15, 18, 21 };

        public static GameObject Build(string hash, int cls, Transform shipRoot)
            => BuildC1(hash, shipRoot); // single class so far

        class GenomeP1
        {
            public float L, W, H, Nose;
            public float HumpH, HumpW, Hump1T, Hump2T;
            public float GunLen;
            public float CanopyStart, CanopyLen;
            public float LegScale;
            public int EngineSegs;
            public float Hue, Sat, Val, OliveHue, MarkOdds, SurfAmp, SurfPhase;
            public int BandMode, BandCount;
            public float BandPhase;
            public Color Sand, Olive;
        }

        static GenomeP1 RollP1(string hash)
        {
            var rng = Rng.Stream("packbody:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new GenomeP1();
            g.L = R(9.5f, 10.5f);
            g.W = R(1.4f, 1.6f);
            g.H = R(0.95f, 1.10f);
            g.Nose = R(0.5f, 0.8f);
            g.HumpH = R(0.55f, 0.75f);
            g.HumpW = R(0.75f, 0.95f);
            g.Hump1T = R(0.30f, 0.34f);
            g.Hump2T = R(0.56f, 0.62f);
            g.GunLen = R(1.2f, 1.5f);
            g.CanopyStart = R(0.08f, 0.12f);
            g.CanopyLen = R(0.16f, 0.20f);
            g.LegScale = R(0.9f, 1.1f);
            g.EngineSegs = 3 + rng.Next(2);
            g.Hue = R(0.10f, 0.13f);
            g.Sat = R(0.30f, 0.45f);
            g.Val = R(0.75f, 0.85f);
            g.OliveHue = R(0.22f, 0.28f);
            g.MarkOdds = R(0.40f, 0.65f);
            g.SurfAmp = R(0f, 0.5f);
            g.SurfPhase = R(0f, 1f);
            g.BandMode = rng.Next(3);
            g.BandCount = 2 + rng.Next(3);
            g.BandPhase = R(0f, 1f);
            g.Sand = Color.HSVToRGB(g.Hue, g.Sat, g.Val);
            g.Olive = Color.HSVToRGB(g.OliveHue, R(0.45f, 0.60f), R(0.28f, 0.38f));
            return g;
        }

        // Freighter sections: blunt wedge bow, slab-sided box amidships,
        // barely-tapered stern.
        static readonly float[,] NoseP =
        {
            {0.00f, 0.24f}, {0.32f, 0.23f}, {0.58f, 0.20f}, {0.78f, 0.14f},
            {0.92f, 0.06f}, {0.99f, -0.02f}, {0.97f, -0.10f},
            {0.84f, -0.16f}, {0.62f, -0.20f}, {0.40f, -0.22f},
            {0.25f, -0.23f}, {0.10f, -0.24f}, {0.00f, -0.24f},
        };
        static readonly float[,] MidP =
        {
            {0.00f, 0.60f}, {0.36f, 0.58f}, {0.64f, 0.52f}, {0.84f, 0.38f},
            {0.95f, 0.18f}, {1.00f, -0.04f}, {0.97f, -0.24f},
            {0.86f, -0.40f}, {0.64f, -0.50f}, {0.42f, -0.56f},
            {0.26f, -0.58f}, {0.10f, -0.60f}, {0.00f, -0.60f},
        };
        static readonly float[,] SternP =
        {
            {0.00f, 0.55f}, {0.36f, 0.53f}, {0.62f, 0.47f}, {0.82f, 0.35f},
            {0.93f, 0.17f}, {0.99f, -0.03f}, {0.96f, -0.20f},
            {0.84f, -0.34f}, {0.62f, -0.43f}, {0.40f, -0.48f},
            {0.25f, -0.50f}, {0.10f, -0.52f}, {0.00f, -0.52f},
        };

        static Vector2 HalfPtP(int k, float t)
        {
            float wMid = Smooth01(t / 0.32f);
            float wStern = Smooth01((t - 0.62f) / 0.38f);
            float x = Mathf.Lerp(NoseP[k, 0], MidP[k, 0], wMid);
            float y = Mathf.Lerp(NoseP[k, 1], MidP[k, 1], wMid);
            x = Mathf.Lerp(x, SternP[k, 0], wStern);
            y = Mathf.Lerp(y, SternP[k, 1], wStern);
            return new Vector2(x, y);
        }

        static Vector2 LoopPtP(int li, float t)
        {
            if (li <= 12) return HalfPtP(li, t);
            var p = HalfPtP(LoopPts - li, t);
            return new Vector2(-p.x, p.y);
        }

        // Caravan livery: sand base with olive camo fields, an olive nose
        // wedge, and olive banding fore or aft.
        static int PaintMatP(GenomeP1 g, int s, int p, float tm, bool[] markCell, bool microHit)
        {
            bool deck = s == 0 || s == 7;
            bool upper = s == 1 || s == 6;
            bool lower = s == 2 || s == 5;
            int side = s <= 3 ? 0 : 1;

            if (tm < 0.07f) return 1;

            if ((deck || upper) && tm > 0.14f && tm < 0.84f)
            {
                float ft = tm - 0.14f - p * 0.02f;
                int cell = Mathf.Min(4, (int)(ft / 0.14f));
                int band = deck ? 0 : 1;
                if (markCell[(side * 15 + band * 5 + cell) % 30]) return 1;
            }

            if (lower && tm > 0.20f && tm < 0.86f)
            {
                int cell = Mathf.Min(4, (int)((tm - 0.20f) / 0.132f));
                if (markCell[(side * 15 + 10 + cell) % 30] && (cell + p) % 2 == 0) return 1;
            }

            if (g.BandMode == 1 && tm > 0.84f && tm < 0.96f)
            {
                float u = (tm - 0.84f) / 0.12f;
                if ((int)(u * g.BandCount * 2 + g.BandPhase * 2f) % 2 == 0) return 1;
            }
            else if (g.BandMode == 2 && tm > 0.07f && tm < 0.17f)
            {
                float u = (tm - 0.07f) / 0.10f;
                if ((int)(u * g.BandCount * 2.5f + g.BandPhase * 2f) % 2 == 0) return 1;
            }

            if (microHit && tm > 0.09f && tm < 0.93f) return 1;
            return 0;
        }

        static GameObject BuildC1(string hash, Transform shipRoot)
        {
            var g = RollP1(hash);
            var b = new Builder();
            float L = g.L, W = g.W, H = g.H;
            const int rings = 48;

            var panelRng = Rng.Stream("packpanels:" + hash);
            var markCell = new bool[30];
            for (int i = 0; i < 30; i++) markCell[i] = panelRng.NextDouble() < g.MarkOdds;
            var micro = new bool[rings - 1, Spans];
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.04;

            // Freighter body: quick swell behind the cab, then a long slab
            // of hold that barely tapers before the drive block.
            float[] cts = { 0.00f, 0.08f, 0.22f, 0.40f, 0.58f, 0.75f, 0.90f, 1.00f };
            float[] csc = { 0.30f, 0.62f, 0.90f, 1.00f, 0.98f, 0.94f, 0.86f, 0.66f };
            float[] clf = { -0.02f, 0.02f, 0.05f, 0.06f, 0.06f, 0.05f, 0.02f, -0.03f };

            System.Func<float, float> zAt = t => (0.50f - 1.00f * t) * L;
            System.Func<int, float, float> hullY = (k, t) =>
            {
                float sc2 = CrSample(cts, csc, t);
                return HalfPtP(k, t).y * H * sc2 + CrSample(cts, clf, t) * H;
            };
            System.Func<int, float, Vector3> surf = (li, t) =>
            {
                float sc2 = CrSample(cts, csc, t);
                float lift2 = CrSample(cts, clf, t) * H;
                var pt = LoopPtP(((li % LoopPts) + LoopPts) % LoopPts, t);
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
                sc *= 1f + g.SurfAmp * 0.012f * Mathf.Sin((t * 6f + g.SurfPhase) * Mathf.PI * 2f);
                float lift = CrSample(cts, clf, t) * H;
                float z = zAt(t);
                for (int s = 0; s < 8; s++)
                {
                    stripVerts[s][j] = new int[4];
                    for (int p = 0; p < 4; p++)
                    {
                        int li = (StripStart[s] + p) % LoopPts;
                        var pt = LoopPtP(li, t);
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
                        int mat = PaintMatP(g, s, p, tm, markCell, micro[i, s * 3 + p]);
                        b.FaceQ(stripVerts[s][i][p], stripVerts[s][i + 1][p],
                            stripVerts[s][i + 1][p + 1], stripVerts[s][i][p + 1], mat);
                    }
            }

            // Train-nose wedge and stern cap.
            var prow = new Vector3(0f, CrSample(cts, clf, 0f) * H - 0.06f, 0.50f * L + g.Nose);
            for (int s = 0; s < 8; s++)
                for (int p = 0; p < 3; p++)
                    b.TriU(prow, b.V[stripVerts[s][0][p + 1]], b.V[stripVerts[s][0][p]], 1);
            var sternC = new Vector3(0f, CrSample(cts, clf, 1f) * H, -0.50f * L - 0.05f);
            for (int s = 0; s < 8; s++)
                for (int p = 0; p < 3; p++)
                    b.TriU(sternC, b.V[stripVerts[s][rings - 1][p]], b.V[stripVerts[s][rings - 1][p + 1]], 1);

            // ---- raked train-cab canopy right at the bow ----
            {
                float tCan = g.CanopyStart;
                float zC = zAt(tCan);
                float halfLen = g.CanopyLen * L * 0.55f;
                System.Func<float, float> deckAt = z =>
                {
                    float t = Mathf.Clamp01((0.50f * L - z) / L);
                    float sc = CrSample(cts, csc, t);
                    return HalfPtP(0, t).y * H * sc + CrSample(cts, clf, t) * H;
                };
                Canopy(b, zC + halfLen, zC - halfLen, 0.26f, 0.16f, deckAt, 3, 1, 1);
            }

            // ---- the two cargo humps: tiered pods with amber beacons ----
            for (int hump = 0; hump < 2; hump++)
            {
                float t0 = hump == 0 ? g.Hump1T : g.Hump2T;
                float z = zAt(t0);
                float y0 = hullY(0, t0);
                float hw = g.HumpW * W;
                float hh = g.HumpH * H;
                Box(b, new Vector3(0f, y0 + hh * 0.28f, z), new Vector3(hw * 0.55f, hh * 0.30f, 0.72f), 0);
                Box(b, new Vector3(0f, y0 + hh * 0.62f, z - 0.04f), new Vector3(hw * 0.42f, hh * 0.24f, 0.56f), 0);
                Box(b, new Vector3(0f, y0 + hh * 0.92f, z - 0.08f), new Vector3(hw * 0.28f, hh * 0.18f, 0.40f), 1);
                Box(b, new Vector3(0f, y0 + hh * 1.12f, z - 0.08f), new Vector3(0.10f, 0.03f, 0.12f), 2);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(b, new Vector3(side * hw * 0.56f, y0 + hh * 0.30f, z),
                        new Vector3(0.015f, hh * 0.16f, 0.40f), 2);
                    var strut = new Vector3(side * hw * 0.40f, y0 + hh * 0.50f, z + 0.74f);
                    Tube(b, new[] { strut, strut + new Vector3(0f, -hh * 0.45f, 0.16f) },
                        new[] { 0.035f, 0.03f }, 5, 1, false);
                }
            }

            // ---- container modules hung on the flanks ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    float t = 0.34f + i * 0.19f;
                    var c = surf(side > 0 ? 4 : 20, t) + new Vector3(side * 0.10f, 0f, 0f);
                    Box(b, c, new Vector3(0.14f, 0.19f, 0.42f), i % 2 == 0 ? 1 : 0);
                    Box(b, c + new Vector3(side * 0.145f, -0.05f, 0f), new Vector3(0.012f, 0.05f, 0.32f), 2);
                }
            }

            // ---- camel-leg landing struts with foot skids ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int leg = 0; leg < 3; leg++)
                {
                    float t = 0.28f + leg * 0.22f;
                    float y0 = hullY(10, t);
                    var hip = new Vector3(side * W * 0.42f, y0 + 0.02f, zAt(t));
                    var knee = hip + new Vector3(side * 0.14f, -0.24f * g.LegScale, -0.04f);
                    var foot = knee + new Vector3(side * 0.04f, -0.20f * g.LegScale, 0.02f);
                    Tube(b, new[] { hip, knee, foot }, new[] { 0.05f, 0.04f, 0.032f }, 5, 1, false);
                    Box(b, foot + new Vector3(0f, -0.03f, 0f), new Vector3(0.06f, 0.028f, 0.16f), 1);
                }
            }

            // ---- one modest dorsal gun ahead of the first hump ----
            {
                float t0 = 0.18f;
                float y0 = hullY(0, t0);
                var mount = new Vector3(0f, y0 + 0.05f, zAt(t0));
                Box(b, mount, new Vector3(0.10f, 0.06f, 0.14f), 1);
                var gb = mount + new Vector3(0f, 0.08f, 0.08f);
                var mz = gb + new Vector3(0f, 0.015f, g.GunLen);
                Tube(b, new[] { gb, mz }, new[] { 0.05f, 0.042f }, 7, 1, false);
                Tube(b, new[] { mz, mz + Vector3.forward * 0.08f }, new[] { 0.048f, 0.042f }, 7, 0, true);
            }

            // ---- amber running lights down both flanks ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 6; i++)
                {
                    float t = 0.16f + i * 0.13f;
                    float sc = CrSample(cts, csc, t);
                    float lift = CrSample(cts, clf, t) * H;
                    var c = new Vector3(side * W * sc * 0.98f, 0.08f * H + lift, zAt(t));
                    Box(b, c, new Vector3(0.02f, 0.03f, 0.08f), 2);
                }
            }

            // ---- stubby tail fin ----
            {
                float y0 = hullY(0, 0.90f);
                var rootF = new Vector3(0f, y0 - 0.02f, zAt(0.88f));
                var rootB = new Vector3(0f, y0 - 0.02f, zAt(0.97f));
                var tipF = new Vector3(0f, y0 + 0.42f, zAt(0.88f) - 0.32f);
                var tipB = new Vector3(0f, y0 + 0.36f, zAt(0.97f) - 0.38f);
                LoftWing(b, new[] { rootF, tipF }, new[] { 0f, 1f },
                    new[] { rootB, tipB }, new[] { 0f, 1f }, 0.045f, 0.012f, 5, true, 1);
            }

            // ---- twin boxy engines with amber exhaust ----
            {
                float lift = CrSample(cts, clf, 0.95f) * H;
                for (int side = -1; side <= 1; side += 2)
                {
                    var ec = new Vector3(side * W * 0.40f, -0.02f * H + lift, zAt(0.92f));
                    Box(b, ec, new Vector3(0.24f, 0.22f, 0.48f), 1);
                    Box(b, ec + new Vector3(0f, 0.05f, 0.28f), new Vector3(0.16f, 0.13f, 0.16f), 0);
                    Box(b, ec + new Vector3(0f, 0f, -0.52f), new Vector3(0.16f, 0.15f, 0.06f), 2);
                    int n = g.EngineSegs;
                    for (int i = 0; i < n; i++)
                        Tube(b, new[] { ec + new Vector3(0f, -0.15f, 0.26f - i * 0.24f), ec + new Vector3(0f, -0.15f, 0.15f - i * 0.24f) },
                            new[] { 0.11f, 0.11f }, 7, i % 2 == 0 ? 0 : 1, false);
                }
            }

            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "pack1_" + hash };
            mesh.SetVertices(b.V);
            mesh.subMeshCount = 4;
            for (int m = 0; m < 4; m++) mesh.SetTriangles(b.Sub[m], m);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.materials = new[]
            {
                Metal(g.Sand, 0.88f, 0.80f),
                Metal(g.Olive, 0.90f, 0.72f),
                SystemView.Mat(new Color(1.0f, 0.72f, 0.25f), true),
                Metal(new Color(0.05f, 0.08f, 0.09f), 0.9f, 0.95f),
            };
            return go;
        }
    }
}
