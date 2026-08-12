using UnityEngine;
using static SpaceGame.MeshKit;

namespace SpaceGame
{
    /// <summary>
    /// Trail-class hull meshes — fennec exploration scouts. C1 "Fennec",
    /// matched to its reference: a sleek cream delta body with a pointed
    /// fox muzzle and twin whisker antennas, two enormous ear fins canted
    /// outward with teal sensor membranes, teal light studs down the
    /// flanks, a small raked canopy, low delta wings with canards, a
    /// ventral collector dish, a boxy stern sensor eye with a teal lens,
    /// and a single fat engine.
    /// C2 "Otocyon" is the bat-eared dark-space explorer: a blunt glowing
    /// intake muzzle, two enormous webbed sail-ears per side with rib
    /// frames and studded edges, teal pod clusters down the flanks, twin
    /// collector dishes, a dorsal mast array, and twin engines flanking a
    /// big teal core orb.
    /// C3 "Nanook" is the arctic surveyor: a massive rounded ivory hull
    /// with a boxy dark bear nose, little rounded ears, rows of capsule
    /// pods with teal lenses stacked down the flanks, a dorsal survey
    /// radome, twin collector dishes, and a rear bank of capsule engines.
    /// Submeshes: 0 cream, 1 dark umber, 2 teal glow, 3 dark glass.
    /// Streams: trailbody / trailpanels (C1), trail2body / trail2panels
    /// (C2), trail3body / trail3panels (C3).
    /// </summary>
    public static class TrailShipMesh
    {
        const int LoopPts = 24;
        const int Spans = 24;

        public static GameObject Build(string hash, int cls, Transform shipRoot)
            => cls == 3 ? BuildC3(hash, shipRoot)
             : cls == 2 ? BuildC2(hash, shipRoot) : BuildC1(hash, shipRoot);

        class GenomeR1
        {
            public float L, W, H, Nose;
            public float EarSpan, EarSweep, EarCant;
            public float WhiskerLen;
            public float CanopyStart, CanopyLen;
            public float WingSpan, WingSweep, CanardSpan;
            public int EngineSegs;
            public float Hue, Sat, Val, MarkOdds, SurfAmp, SurfPhase;
            public int BandMode, BandCount;
            public float BandPhase;
            public Color Cream, Umber;
        }

        static GenomeR1 RollR1(string hash)
        {
            var rng = Rng.Stream("trailbody:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new GenomeR1();
            g.L = R(6.2f, 6.8f);
            g.W = R(0.95f, 1.10f);
            g.H = R(0.62f, 0.72f);
            g.Nose = R(0.7f, 1.0f);
            g.EarSpan = R(2.0f, 2.6f);
            g.EarSweep = R(0.9f, 1.3f);
            g.EarCant = R(0.30f, 0.45f);
            g.WhiskerLen = R(0.8f, 1.2f);
            g.CanopyStart = R(0.16f, 0.20f);
            g.CanopyLen = R(0.18f, 0.24f);
            g.WingSpan = R(1.6f, 2.0f);
            g.WingSweep = R(1.1f, 1.5f);
            g.CanardSpan = R(0.6f, 0.8f);
            g.EngineSegs = 3 + rng.Next(2);
            g.Hue = R(0.07f, 0.11f);
            g.Sat = R(0.25f, 0.40f);
            g.Val = R(0.78f, 0.88f);
            g.MarkOdds = R(0.35f, 0.60f);
            g.SurfAmp = R(0f, 0.5f);
            g.SurfPhase = R(0f, 1f);
            g.BandMode = rng.Next(3);
            g.BandCount = 2 + rng.Next(3);
            g.BandPhase = R(0f, 1f);
            g.Cream = Color.HSVToRGB(g.Hue, g.Sat, g.Val);
            g.Umber = Color.HSVToRGB(g.Hue, Mathf.Min(1f, g.Sat + 0.30f), 0.32f);
            return g;
        }

        // Scout sections: sharp fox muzzle, lean teardrop body, clipped tail.
        static readonly float[,] NoseR =
        {
            {0.00f, 0.20f}, {0.30f, 0.19f}, {0.55f, 0.16f}, {0.75f, 0.11f},
            {0.90f, 0.05f}, {0.98f, -0.01f}, {1.00f, -0.06f},
            {0.85f, -0.10f}, {0.62f, -0.13f}, {0.38f, -0.15f},
            {0.24f, -0.16f}, {0.10f, -0.17f}, {0.00f, -0.17f},
        };
        static readonly float[,] MidR =
        {
            {0.00f, 0.62f}, {0.32f, 0.59f}, {0.58f, 0.51f}, {0.78f, 0.37f},
            {0.92f, 0.18f}, {1.00f, -0.02f}, {0.95f, -0.20f},
            {0.83f, -0.34f}, {0.63f, -0.43f}, {0.41f, -0.49f},
            {0.26f, -0.52f}, {0.10f, -0.54f}, {0.00f, -0.55f},
        };
        static readonly float[,] SternR =
        {
            {0.00f, 0.50f}, {0.34f, 0.48f}, {0.60f, 0.41f}, {0.78f, 0.30f},
            {0.90f, 0.15f}, {0.96f, -0.01f}, {0.92f, -0.15f},
            {0.80f, -0.26f}, {0.61f, -0.33f}, {0.40f, -0.38f},
            {0.25f, -0.40f}, {0.10f, -0.42f}, {0.00f, -0.42f},
        };

        static Vector2 HalfPtR(int k, float t)
        {
            float wMid = Smooth01(t / 0.38f);
            float wStern = Smooth01((t - 0.60f) / 0.40f);
            float x = Mathf.Lerp(NoseR[k, 0], MidR[k, 0], wMid);
            float y = Mathf.Lerp(NoseR[k, 1], MidR[k, 1], wMid);
            x = Mathf.Lerp(x, SternR[k, 0], wStern);
            y = Mathf.Lerp(y, SternR[k, 1], wStern);
            return new Vector2(x, y);
        }

        static Vector2 LoopPtR(int li, float t)
        {
            if (li <= 12) return HalfPtR(li, t);
            var p = HalfPtR(LoopPts - li, t);
            return new Vector2(-p.x, p.y);
        }

        static Vector2 HalfPtRF(float k, float t)
        {
            float wMid = Smooth01(t / 0.38f);
            float wStern = Smooth01((t - 0.60f) / 0.40f);
            var n = ProfCR(NoseR, k);
            var m = ProfCR(MidR, k);
            var st = ProfCR(SternR, k);
            var v = Vector2.Lerp(n, m, wMid);
            return Vector2.Lerp(v, st, wStern);
        }

        // Fennec coat: cream base, umber muzzle tip and saddle patches,
        // umber micro flecks; the teal accents are all geometry.
        static int PaintMatR(GenomeR1 g, int s, int p, float tm, bool[] markCell, bool microHit)
        {
            bool deck = s == 0 || s == 7;
            bool upper = s == 1 || s == 6;
            bool lower = s == 2 || s == 5;
            bool belly = s == 3 || s == 4;
            int side = s <= 3 ? 0 : 1;

            // Dark fox-nose tip.
            if (tm < 0.05f) return 1;

            // Saddle patches over the back.
            if ((deck || upper) && tm > 0.20f && tm < 0.78f)
            {
                float ft = tm - 0.20f - p * 0.02f;
                int cell = Mathf.Min(4, (int)(ft / 0.116f));
                int band = deck ? 0 : 1;
                if (markCell[(side * 15 + band * 5 + cell) % 30]) return 1;
            }

            // Sparse flank ticking.
            if (lower && tm > 0.24f && tm < 0.80f)
            {
                int cell = Mathf.Min(4, (int)((tm - 0.24f) / 0.112f));
                if (markCell[(side * 15 + 10 + cell) % 30] && (cell + p) % 2 == 0) return 1;
            }

            // Band families: umber tail rings or muzzle chevrons.
            if (g.BandMode == 1 && tm > 0.80f && tm < 0.95f)
            {
                float u = (tm - 0.80f) / 0.15f;
                if ((int)(u * g.BandCount * 2 + g.BandPhase * 2f) % 2 == 0) return 1;
            }
            else if (g.BandMode == 2 && tm > 0.05f && tm < 0.16f)
            {
                float u = (tm - 0.05f) / 0.11f;
                if ((int)(u * g.BandCount * 2.5f + g.BandPhase * 2f) % 2 == 0) return 1;
            }

            if (microHit && !belly && tm > 0.10f && tm < 0.92f) return 1;
            return 0;
        }

        static GameObject BuildC1(string hash, Transform shipRoot)
        {
            var g = RollR1(hash);
            var b = new Builder();
            float L = g.L, W = g.W, H = g.H;
            const int rings = 88;

            var panelRng = Rng.Stream("trailpanels:" + hash);
            var markCell = new bool[30];
            for (int i = 0; i < 30; i++) markCell[i] = panelRng.NextDouble() < g.MarkOdds;
            var micro = new bool[rings - 1, Spans];
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.03;

            // Lean scout body: quick swell behind the muzzle, deepest just
            // ahead of midships, tapering hard to a clipped tail.
            float[] cts = { 0.00f, 0.09f, 0.24f, 0.40f, 0.56f, 0.72f, 0.88f, 1.00f };
            float[] csc = { 0.22f, 0.55f, 0.88f, 1.00f, 0.94f, 0.80f, 0.60f, 0.42f };
            float[] clf = { -0.02f, 0.02f, 0.06f, 0.07f, 0.05f, 0.01f, -0.04f, -0.08f };

            System.Func<float, float> zAt = t => (0.50f - 1.00f * t) * L;
            System.Func<int, float, float> hullY = (k, t) =>
            {
                float sc2 = CrSample(cts, csc, t);
                return HalfPtR(k, t).y * H * sc2 + CrSample(cts, clf, t) * H;
            };
            System.Func<int, float, Vector3> surf = (li, t) =>
            {
                float sc2 = CrSample(cts, csc, t);
                float lift2 = CrSample(cts, clf, t) * H;
                var pt = LoopPtR(((li % LoopPts) + LoopPts) % LoopPts, t);
                return new Vector3(pt.x * W * sc2, pt.y * H * sc2 + lift2, zAt(t));
            };

            // ---- main hull loft (48-pt smoothed) ----
            var stripVerts = HullLoft48(b, rings, HalfPtRF,
                t => CrSample(cts, csc, t)
                    * (1f + g.SurfAmp * 0.012f * Mathf.Sin((t * 6f + g.SurfPhase) * Mathf.PI * 2f)),
                t => CrSample(cts, clf, t) * H, zAt, t => 1f, t => 1f, W, H,
                (so, po, tm, i) => PaintMatR(g, so, po, tm, markCell, micro[i, so * 3 + po]));

            // Fox muzzle point and tail cap.
            var muzzle = new Vector3(0f, CrSample(cts, clf, 0f) * H - 0.02f, 0.50f * L + g.Nose);
            CapFan(b, muzzle, stripVerts, 0, true, 1);
            var sternC = new Vector3(0f, CrSample(cts, clf, 1f) * H, -0.50f * L - 0.05f);
            CapFan(b, sternC, stripVerts, rings - 1, false, 1);

            // ---- twin whisker antennas raking forward off the muzzle ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int w2 = 0; w2 < 2; w2++)
                {
                    var basePt = surf(side > 0 ? 4 : 20, 0.05f + w2 * 0.03f);
                    var dir = new Vector3(side * (0.16f + w2 * 0.10f), -0.06f, 0.98f).normalized;
                    Tube(b, new[] { basePt, basePt + dir * (g.WhiskerLen * 0.55f), basePt + dir * g.WhiskerLen },
                        new[] { 0.018f, 0.010f, 0.003f }, 4, 1, true);
                }
            }

            // ---- the ears: two huge canted fins with teal membranes ----
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                float t0 = 0.42f;
                float y0 = hullY(1, t0);
                var rootF = new Vector3(s * W * 0.28f, y0 - 0.02f, zAt(t0) + 0.55f);
                var rootB = new Vector3(s * W * 0.34f, y0 - 0.02f, zAt(t0) - 0.75f);
                var tipF = rootF + new Vector3(s * g.EarSpan * g.EarCant, g.EarSpan, -g.EarSweep * 0.55f);
                var tipB = rootB + new Vector3(s * g.EarSpan * g.EarCant * 1.1f, g.EarSpan * 0.82f, -g.EarSweep);
                var lead = new[] { rootF, tipF };
                var trail = new[] { rootB, tipB };
                var ts = new[] { 0f, 1f };
                LoftWing(b, lead, ts, trail, ts, 0.07f, 0.015f, 7, side < 0, 0);
                WingPlate(b, lead, ts, trail, ts, 0.07f, 0.015f, 0.22f, 0.88f, 2);
                WingPlate(b, lead, ts, trail, ts, 0.07f, 0.015f, 0.90f, 0.98f, 1);
            }

            // ---- raked glass canopy on the fore-deck ----
            {
                float tCan = g.CanopyStart;
                float zC = zAt(tCan);
                float halfLen = g.CanopyLen * L * 0.55f;
                System.Func<float, float> deckAt = z =>
                {
                    float t = Mathf.Clamp01((0.50f * L - z) / L);
                    float sc = CrSample(cts, csc, t);
                    return HalfPtR(0, t).y * H * sc + CrSample(cts, clf, t) * H;
                };
                Canopy(b, zC + halfLen, zC - halfLen, 0.20f, 0.13f, deckAt, 3, 1, 1);
            }

            // ---- teal light studs down both flanks ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 5; i++)
                {
                    float t = 0.18f + i * 0.14f;
                    float sc = CrSample(cts, csc, t);
                    float lift = CrSample(cts, clf, t) * H;
                    var c = new Vector3(side * W * sc * 0.97f, 0.02f * H + lift, zAt(t));
                    BevelBox(b, c, new Vector3(0.022f, 0.035f, 0.08f), 2);
                }
            }

            // ---- low delta wings and forward canards ----
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                {
                    float t0 = 0.56f;
                    float lift = CrSample(cts, clf, t0) * H;
                    var rootF = new Vector3(s * W * 0.62f, -H * 0.16f + lift, zAt(t0) + 0.35f);
                    var rootB = rootF + new Vector3(-s * 0.02f, -0.01f, -0.85f);
                    var tipF = rootF + new Vector3(s * g.WingSpan, -0.06f, -g.WingSweep);
                    var tipB = rootB + new Vector3(s * g.WingSpan * 0.92f, -0.06f, -g.WingSweep * 1.05f);
                    LoftWing(b, new[] { rootF, tipF }, new[] { 0f, 1f },
                        new[] { rootB, tipB }, new[] { 0f, 1f }, 0.05f, 0.012f, 6, side < 0, 0);
                    WingPlate(b, new[] { rootF, tipF }, new[] { 0f, 1f },
                        new[] { rootB, tipB }, new[] { 0f, 1f }, 0.05f, 0.012f, 0.60f, 0.92f, 1);
                }
                {
                    float t0 = 0.16f;
                    float lift = CrSample(cts, clf, t0) * H;
                    var rootF = new Vector3(s * W * 0.55f, -H * 0.04f + lift, zAt(t0) + 0.18f);
                    var rootB = rootF + new Vector3(-s * 0.01f, -0.01f, -0.36f);
                    var tipF = rootF + new Vector3(s * g.CanardSpan, -0.02f, -g.WingSweep * 0.35f);
                    var tipB = rootB + new Vector3(s * g.CanardSpan * 0.92f, -0.02f, -g.WingSweep * 0.40f);
                    LoftWing(b, new[] { rootF, tipF }, new[] { 0f, 1f },
                        new[] { rootB, tipB }, new[] { 0f, 1f }, 0.04f, 0.010f, 5, side < 0, 0);
                }
            }

            // ---- ventral collector dish (the collector hardpoint) ----
            {
                float t0 = 0.46f;
                float y0 = hullY(12, t0);
                var basePt = new Vector3(0f, y0 + 0.02f, zAt(t0));
                Tube(b, new[] { basePt, basePt - Vector3.up * 0.18f },
                    new[] { 0.05f, 0.035f }, 6, 1, false);
                var dish = basePt - Vector3.up * 0.22f;
                Tube(b, new[] { dish, dish - Vector3.up * 0.05f },
                    new[] { 0.16f, 0.19f }, 8, 3, false);
                Ball(b, dish - Vector3.up * 0.09f, 0.055f, 2, 2, 6);
            }

            // ---- boxy stern sensor eye with a teal lens ----
            {
                float y0 = hullY(0, 0.90f);
                var c = new Vector3(0f, y0 + 0.06f, zAt(0.90f));
                BevelBox(b, c, new Vector3(0.20f, 0.16f, 0.22f), 1);
                BevelBox(b, c + new Vector3(0f, 0f, -0.24f), new Vector3(0.13f, 0.11f, 0.02f), 3);
                Ball(b, c + new Vector3(0f, 0f, -0.26f), 0.075f, 2, 2, 6);
                var mast = c + new Vector3(0f, 0.17f, 0.06f);
                Tube(b, new[] { mast, mast + new Vector3(0f, 0.30f, -0.06f) },
                    new[] { 0.018f, 0.004f }, 4, 1, true);
            }

            // ---- single fat engine plus two trim thrusters ----
            {
                float lift = CrSample(cts, clf, 0.96f) * H;
                var ec = new Vector3(0f, -0.08f * H + lift, zAt(0.94f));
                Tube(b, new[] { ec, ec - Vector3.forward * 0.50f },
                    new[] { 0.17f, 0.17f }, 8, 1, false);
                Nozzle(b, ec - Vector3.forward * 0.58f, Vector3.back, 0.18f, 0.26f, 10, 1, 2);
                int n = g.EngineSegs;
                for (int i = 0; i < n; i++)
                    Tube(b, new[] { ec + new Vector3(0f, 0f, -0.06f - i * 0.14f), ec + new Vector3(0f, 0f, -0.12f - i * 0.14f) },
                        new[] { 0.185f, 0.185f }, 8, i % 2 == 0 ? 0 : 1, false);
                for (int side = -1; side <= 1; side += 2)
                {
                    var tc = new Vector3(side * W * 0.42f, 0.02f * H + lift, zAt(0.92f));
                    Tube(b, new[] { tc, tc - Vector3.forward * 0.28f },
                        new[] { 0.07f, 0.07f }, 6, 1, false);
                    Nozzle(b, tc - Vector3.forward * 0.33f, Vector3.back, 0.072f, 0.10f, 8, 1, 2);
                }
            }

            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "trail1_" + hash };
            mesh.SetVertices(b.V);
            mesh.subMeshCount = 4;
            for (int m = 0; m < 4; m++) mesh.SetTriangles(b.Sub[m], m);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.materials = new[]
            {
                Metal(g.Cream, 0.88f, 0.80f),
                Metal(g.Umber, 0.90f, 0.72f),
                SystemView.Mat(new Color(0.30f, 0.95f, 0.95f), true),
                Metal(new Color(0.05f, 0.08f, 0.09f), 0.9f, 0.95f),
            };
            return go;
        }

        // ==================== Class 2 "Otocyon" ====================

        class GenomeR2
        {
            public float L, W, H, Nose;
            public float SailSpan, SailSweep, SailCant;
            public float WhiskerLen;
            public float CanopyStart, CanopyLen;
            public int PodCount, EngineSegs;
            public float Hue, Sat, Val, MarkOdds, SurfAmp, SurfPhase;
            public int BandMode, BandCount;
            public float BandPhase;
            public Color Cream, Umber;
        }

        static GenomeR2 RollR2(string hash)
        {
            var rng = Rng.Stream("trail2body:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new GenomeR2();
            g.L = R(8.2f, 9.0f);
            g.W = R(1.15f, 1.30f);
            g.H = R(0.75f, 0.85f);
            g.Nose = R(0.4f, 0.6f);
            g.SailSpan = R(3.0f, 3.7f);
            g.SailSweep = R(1.6f, 2.2f);
            g.SailCant = R(0.55f, 0.75f);
            g.WhiskerLen = R(1.0f, 1.4f);
            g.CanopyStart = R(0.14f, 0.18f);
            g.CanopyLen = R(0.20f, 0.26f);
            g.PodCount = 4 + rng.Next(3);
            g.EngineSegs = 3 + rng.Next(2);
            g.Hue = R(0.07f, 0.11f);
            g.Sat = R(0.25f, 0.40f);
            g.Val = R(0.78f, 0.88f);
            g.MarkOdds = R(0.40f, 0.65f);
            g.SurfAmp = R(0f, 0.5f);
            g.SurfPhase = R(0f, 1f);
            g.BandMode = rng.Next(3);
            g.BandCount = 2 + rng.Next(3);
            g.BandPhase = R(0f, 1f);
            g.Cream = Color.HSVToRGB(g.Hue, g.Sat, g.Val);
            g.Umber = Color.HSVToRGB(g.Hue, Mathf.Min(1f, g.Sat + 0.30f), 0.32f);
            return g;
        }

        // One webbed sail-ear: a rounded membrane loft between bulged CR
        // chains, three umber rib strips, and stud nodes along the tip edge.
        static void Sail(Builder b, Vector3 rootF, Vector3 rootB, Vector3 tipF, Vector3 tipB,
            float side, bool flip)
        {
            var mF = (rootF + tipF) * 0.5f + new Vector3(side * 0.25f, 0.15f, 0.30f);
            var mB = (rootB + tipB) * 0.5f + new Vector3(side * 0.30f, 0.10f, -0.30f);
            var lead = new[] { rootF, mF, tipF };
            var trail = new[] { rootB, mB, tipB };
            var ts = new[] { 0f, 0.5f, 1f };
            LoftWing(b, lead, ts, trail, ts, 0.06f, 0.014f, 8, flip, 0);
            for (int rib = 0; rib < 3; rib++)
            {
                float f0 = 0.28f + rib * 0.19f;
                WingPlate(b, lead, ts, trail, ts, 0.06f, 0.014f, f0, f0 + 0.05f, 1);
            }
            for (int n = 0; n < 4; n++)
            {
                float u = 0.25f + n * 0.24f;
                var stud = WingSurfPt(lead, ts, trail, ts, 0.06f, 0.014f, u, 0.96f, 0.02f);
                Ball(b, stud, 0.038f, n % 3 == 0 ? 2 : 1, 2, 5);
            }
        }

        static GameObject BuildC2(string hash, Transform shipRoot)
        {
            var g = RollR2(hash);
            var b = new Builder();
            float L = g.L, W = g.W, H = g.H;
            const int rings = 92;

            var panelRng = Rng.Stream("trail2panels:" + hash);
            var markCell = new bool[30];
            for (int i = 0; i < 30; i++) markCell[i] = panelRng.NextDouble() < g.MarkOdds;
            var micro = new bool[rings - 1, Spans];
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.03;

            // Heavier explorer body: blunter bow, full amidships, and a
            // tail that keeps volume for the drive block.
            float[] cts = { 0.00f, 0.09f, 0.24f, 0.40f, 0.56f, 0.72f, 0.88f, 1.00f };
            float[] csc = { 0.34f, 0.64f, 0.90f, 1.00f, 0.96f, 0.85f, 0.68f, 0.48f };
            float[] clf = { -0.02f, 0.02f, 0.06f, 0.07f, 0.05f, 0.01f, -0.04f, -0.08f };

            System.Func<float, float> zAt = t => (0.50f - 1.00f * t) * L;
            System.Func<int, float, float> hullY = (k, t) =>
            {
                float sc2 = CrSample(cts, csc, t);
                return HalfPtR(k, t).y * H * sc2 + CrSample(cts, clf, t) * H;
            };
            System.Func<int, float, Vector3> surf = (li, t) =>
            {
                float sc2 = CrSample(cts, csc, t);
                float lift2 = CrSample(cts, clf, t) * H;
                var pt = LoopPtR(((li % LoopPts) + LoopPts) % LoopPts, t);
                return new Vector3(pt.x * W * sc2, pt.y * H * sc2 + lift2, zAt(t));
            };

            // ---- main hull loft (48-pt smoothed) ----
            var stripVerts = HullLoft48(b, rings, HalfPtRF,
                t => CrSample(cts, csc, t)
                    * (1f + g.SurfAmp * 0.012f * Mathf.Sin((t * 6f + g.SurfPhase) * Mathf.PI * 2f)),
                t => CrSample(cts, clf, t) * H, zAt, t => 1f, t => 1f, W, H,
                (so, po, tm, i) => PaintMatR2(g, so, po, tm, markCell, micro[i, so * 3 + po]));

            // Blunt intake muzzle: dark shroud over a glowing teal core.
            float yBow = CrSample(cts, clf, 0f) * H;
            var bowC = new Vector3(0f, yBow - 0.02f, 0.50f * L + 0.02f);
            CapFan(b, bowC, stripVerts, 0, true, 1);
            Tube(b, new[] { bowC, bowC + Vector3.forward * g.Nose },
                new[] { W * 0.26f, W * 0.20f }, 8, 1, false);
            Tube(b, new[] { bowC + Vector3.forward * g.Nose, bowC + Vector3.forward * (g.Nose + 0.07f) },
                new[] { W * 0.15f, W * 0.13f }, 8, 2, true);
            var sternC = new Vector3(0f, CrSample(cts, clf, 1f) * H, -0.50f * L - 0.05f);
            CapFan(b, sternC, stripVerts, rings - 1, false, 1);

            // ---- webbed sail-ears: a big fore pair and a smaller aft pair ----
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                {
                    float y0 = hullY(1, 0.34f);
                    var rootF = new Vector3(s * W * 0.30f, y0 - 0.02f, zAt(0.28f));
                    var rootB = new Vector3(s * W * 0.36f, y0 - 0.02f, zAt(0.54f));
                    var tipF = rootF + new Vector3(s * g.SailSpan * g.SailCant, g.SailSpan, -g.SailSweep * 0.5f);
                    var tipB = rootB + new Vector3(s * g.SailSpan * g.SailCant * 1.15f, g.SailSpan * 0.72f, -g.SailSweep);
                    Sail(b, rootF, rootB, tipF, tipB, s, side < 0);
                }
                {
                    float y0 = hullY(1, 0.68f);
                    float span = g.SailSpan * 0.62f;
                    var rootF = new Vector3(s * W * 0.34f, y0 - 0.02f, zAt(0.62f));
                    var rootB = new Vector3(s * W * 0.38f, y0 - 0.02f, zAt(0.80f));
                    var tipF = rootF + new Vector3(s * span * g.SailCant * 1.3f, span * 0.85f, -g.SailSweep * 0.6f);
                    var tipB = rootB + new Vector3(s * span * g.SailCant * 1.45f, span * 0.60f, -g.SailSweep * 1.05f);
                    Sail(b, rootF, rootB, tipF, tipB, s, side < 0);
                }
            }

            // ---- whisker antennas: nose pair and sail-joint pair ----
            for (int side = -1; side <= 1; side += 2)
            {
                var basePt = surf(side > 0 ? 4 : 20, 0.06f);
                var dir = new Vector3(side * 0.18f, -0.05f, 0.98f).normalized;
                Tube(b, new[] { basePt, basePt + dir * (g.WhiskerLen * 0.55f), basePt + dir * g.WhiskerLen },
                    new[] { 0.020f, 0.011f, 0.003f }, 4, 1, true);
                var jointPt = surf(side > 0 ? 2 : 22, 0.30f) + new Vector3(0f, 0.05f, 0f);
                var dir2 = new Vector3(side * 0.30f, 0.55f, 0.78f).normalized;
                Tube(b, new[] { jointPt, jointPt + dir2 * (g.WhiskerLen * 0.7f), jointPt + dir2 * (g.WhiskerLen * 1.3f) },
                    new[] { 0.018f, 0.010f, 0.003f }, 4, 1, true);
            }

            // ---- long raked canopy on the fore-deck ----
            {
                float tCan = g.CanopyStart;
                float zC = zAt(tCan);
                float halfLen = g.CanopyLen * L * 0.55f;
                System.Func<float, float> deckAt = z =>
                {
                    float t = Mathf.Clamp01((0.50f * L - z) / L);
                    float sc = CrSample(cts, csc, t);
                    return HalfPtR(0, t).y * H * sc + CrSample(cts, clf, t) * H;
                };
                Canopy(b, zC + halfLen, zC - halfLen, 0.24f, 0.15f, deckAt, 3, 1, 1);
            }

            // ---- teal pod clusters low on both flanks ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < g.PodCount; i++)
                {
                    float t = 0.30f + i * 0.07f;
                    var c = surf(side > 0 ? 5 : 19, t) + new Vector3(side * 0.02f, -0.02f, 0f);
                    Ball(b, c, 0.062f, 2, 2, 6);
                }
                var orb = surf(side > 0 ? 4 : 20, 0.66f) + new Vector3(side * 0.04f, 0f, 0f);
                Ball(b, orb, 0.13f, 2, 3, 7);
                Tube(b, new[] { orb + Vector3.forward * 0.16f, orb - Vector3.forward * 0.16f },
                    new[] { 0.145f, 0.145f }, 7, 1, false);
            }

            // ---- twin ventral collector dishes ----
            for (int d2 = 0; d2 < 2; d2++)
            {
                float t0 = 0.40f + d2 * 0.16f;
                float y0 = hullY(12, t0);
                var basePt = new Vector3(0f, y0 + 0.02f, zAt(t0));
                Tube(b, new[] { basePt, basePt - Vector3.up * 0.18f },
                    new[] { 0.05f, 0.035f }, 6, 1, false);
                var dish = basePt - Vector3.up * 0.22f;
                Tube(b, new[] { dish, dish - Vector3.up * 0.05f },
                    new[] { 0.15f, 0.18f }, 8, 3, false);
                Ball(b, dish - Vector3.up * 0.09f, 0.05f, 2, 2, 6);
            }

            // ---- dorsal sensor mast array + stern eye ----
            for (int m2 = 0; m2 < 2; m2++)
            {
                float t0 = 0.22f + m2 * 0.10f;
                var mast = new Vector3((m2 == 0 ? -1 : 1) * 0.10f, hullY(0, t0) - 0.01f, zAt(t0));
                Tube(b, new[] { mast, mast + new Vector3(0f, 0.40f + m2 * 0.14f, -0.10f) },
                    new[] { 0.020f, 0.004f }, 4, 1, true);
            }
            {
                float y0 = hullY(0, 0.90f);
                var c = new Vector3(0f, y0 + 0.07f, zAt(0.90f));
                BevelBox(b, c, new Vector3(0.22f, 0.17f, 0.24f), 1);
                BevelBox(b, c + new Vector3(0f, 0f, -0.26f), new Vector3(0.14f, 0.12f, 0.02f), 3);
                Ball(b, c + new Vector3(0f, 0f, -0.28f), 0.08f, 2, 2, 6);
            }

            // ---- twin engines flanking a teal core orb ----
            {
                float lift = CrSample(cts, clf, 0.96f) * H;
                for (int side = -1; side <= 1; side += 2)
                {
                    var ec = new Vector3(side * W * 0.34f, -0.06f * H + lift, zAt(0.93f));
                    Tube(b, new[] { ec, ec - Vector3.forward * 0.45f },
                        new[] { 0.13f, 0.13f }, 8, 1, false);
                    Nozzle(b, ec - Vector3.forward * 0.53f, Vector3.back, 0.14f, 0.20f, 10, 1, 2);
                    int n = g.EngineSegs;
                    for (int i = 0; i < n; i++)
                        Tube(b, new[] { ec + new Vector3(0f, 0f, -0.05f - i * 0.12f), ec + new Vector3(0f, 0f, -0.10f - i * 0.12f) },
                            new[] { 0.145f, 0.145f }, 8, i % 2 == 0 ? 0 : 1, false);
                }
                var core = new Vector3(0f, 0.02f * H + lift, zAt(0.90f) - 0.30f);
                Ball(b, core, 0.15f, 2, 3, 8);
                Tube(b, new[] { core + Vector3.forward * 0.18f, core - Vector3.forward * 0.10f },
                    new[] { 0.165f, 0.165f }, 8, 1, false);
            }

            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "trail2_" + hash };
            mesh.SetVertices(b.V);
            mesh.subMeshCount = 4;
            for (int m = 0; m < 4; m++) mesh.SetTriangles(b.Sub[m], m);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.materials = new[]
            {
                Metal(g.Cream, 0.88f, 0.80f),
                Metal(g.Umber, 0.90f, 0.72f),
                SystemView.Mat(new Color(0.30f, 0.95f, 0.95f), true),
                Metal(new Color(0.05f, 0.08f, 0.09f), 0.9f, 0.95f),
            };
            return go;
        }

        // Otocyon coat: like the Fennec's but with bolder saddle fields
        // and a dark intake muzzle.
        static int PaintMatR2(GenomeR2 g, int s, int p, float tm, bool[] markCell, bool microHit)
        {
            bool deck = s == 0 || s == 7;
            bool upper = s == 1 || s == 6;
            bool lower = s == 2 || s == 5;
            bool belly = s == 3 || s == 4;
            int side = s <= 3 ? 0 : 1;

            if (tm < 0.08f) return 1;

            if ((deck || upper) && tm > 0.16f && tm < 0.80f)
            {
                float ft = tm - 0.16f - p * 0.02f;
                int cell = Mathf.Min(4, (int)(ft / 0.128f));
                int band = deck ? 0 : 1;
                if (markCell[(side * 15 + band * 5 + cell) % 30]) return 1;
            }

            if (lower && tm > 0.22f && tm < 0.82f)
            {
                int cell = Mathf.Min(4, (int)((tm - 0.22f) / 0.12f));
                if (markCell[(side * 15 + 10 + cell) % 30] && (cell + p) % 2 == 0) return 1;
            }

            if (g.BandMode == 1 && tm > 0.82f && tm < 0.96f)
            {
                float u = (tm - 0.82f) / 0.14f;
                if ((int)(u * g.BandCount * 2 + g.BandPhase * 2f) % 2 == 0) return 1;
            }
            else if (g.BandMode == 2 && tm > 0.08f && tm < 0.18f)
            {
                float u = (tm - 0.08f) / 0.10f;
                if ((int)(u * g.BandCount * 2.5f + g.BandPhase * 2f) % 2 == 0) return 1;
            }

            if (microHit && !belly && tm > 0.10f && tm < 0.92f) return 1;
            return 0;
        }

        // ==================== Class 3 "Nanook" ====================

        class GenomeR3
        {
            public float L, W, H, Nose;
            public float PodLen, PodR;
            public int PodCount;
            public float EarSize;
            public float CanopyStart, CanopyLen, DomeR;
            public int EngineSegs;
            public float Hue, Sat, Val, MarkOdds, SurfAmp, SurfPhase;
            public int BandMode, BandCount;
            public float BandPhase;
            public Color Cream, Umber;
        }

        static GenomeR3 RollR3(string hash)
        {
            var rng = Rng.Stream("trail3body:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new GenomeR3();
            g.L = R(10.5f, 11.5f);
            g.W = R(1.6f, 1.8f);
            g.H = R(1.1f, 1.25f);
            g.Nose = R(0.5f, 0.7f);
            g.PodLen = R(0.8f, 1.1f);
            g.PodR = R(0.15f, 0.20f);
            g.PodCount = 3 + rng.Next(2);
            g.EarSize = R(0.08f, 0.12f);
            g.CanopyStart = R(0.12f, 0.16f);
            g.CanopyLen = R(0.20f, 0.26f);
            g.DomeR = R(0.14f, 0.19f);
            g.EngineSegs = 3 + rng.Next(2);
            g.Hue = R(0.07f, 0.11f);
            g.Sat = R(0.15f, 0.28f);
            g.Val = R(0.82f, 0.90f);
            g.MarkOdds = R(0.30f, 0.55f);
            g.SurfAmp = R(0f, 0.5f);
            g.SurfPhase = R(0f, 1f);
            g.BandMode = rng.Next(3);
            g.BandCount = 2 + rng.Next(3);
            g.BandPhase = R(0f, 1f);
            g.Cream = Color.HSVToRGB(g.Hue, g.Sat, g.Val);
            g.Umber = Color.HSVToRGB(g.Hue, Mathf.Min(1f, g.Sat + 0.35f), 0.28f);
            return g;
        }

        // Polar coat: near-white ivory with sparse dark grime patches, a
        // dark muzzle band, and heavy micro mottling.
        static int PaintMatR3(GenomeR3 g, int s, int p, float tm, bool[] markCell, bool microHit)
        {
            bool deck = s == 0 || s == 7;
            bool upper = s == 1 || s == 6;
            bool lower = s == 2 || s == 5;
            bool belly = s == 3 || s == 4;
            int side = s <= 3 ? 0 : 1;

            if (tm < 0.06f) return 1;

            if ((deck || upper) && tm > 0.18f && tm < 0.82f)
            {
                float ft = tm - 0.18f - p * 0.02f;
                int cell = Mathf.Min(4, (int)(ft / 0.128f));
                int band = deck ? 0 : 1;
                if (markCell[(side * 15 + band * 5 + cell) % 30] && (cell + p) % 2 == 0) return 1;
            }

            if (lower && tm > 0.24f && tm < 0.84f)
            {
                int cell = Mathf.Min(4, (int)((tm - 0.24f) / 0.12f));
                if (markCell[(side * 15 + 10 + cell) % 30] && (cell + p) % 2 == 1) return 1;
            }

            if (g.BandMode == 1 && tm > 0.84f && tm < 0.96f)
            {
                float u = (tm - 0.84f) / 0.12f;
                if ((int)(u * g.BandCount * 2 + g.BandPhase * 2f) % 2 == 0) return 1;
            }
            else if (g.BandMode == 2 && tm > 0.06f && tm < 0.15f)
            {
                float u = (tm - 0.06f) / 0.09f;
                if ((int)(u * g.BandCount * 2.5f + g.BandPhase * 2f) % 2 == 0) return 1;
            }

            if (microHit && !belly && tm > 0.08f && tm < 0.94f) return 1;
            return 0;
        }

        static GameObject BuildC3(string hash, Transform shipRoot)
        {
            var g = RollR3(hash);
            var b = new Builder();
            float L = g.L, W = g.W, H = g.H;
            const int rings = 96;

            var panelRng = Rng.Stream("trail3panels:" + hash);
            var markCell = new bool[30];
            for (int i = 0; i < 30; i++) markCell[i] = panelRng.NextDouble() < g.MarkOdds;
            var micro = new bool[rings - 1, Spans];
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.05;

            // Bear body: a full rounded mass almost end to end, with a
            // gentle taper to the snout and a heavy rump at the stern.
            float[] cts = { 0.00f, 0.08f, 0.22f, 0.40f, 0.58f, 0.74f, 0.90f, 1.00f };
            float[] csc = { 0.30f, 0.62f, 0.90f, 1.00f, 0.98f, 0.92f, 0.80f, 0.60f };
            float[] clf = { -0.04f, 0.02f, 0.06f, 0.08f, 0.07f, 0.04f, -0.01f, -0.06f };

            System.Func<float, float> zAt = t => (0.50f - 1.00f * t) * L;
            System.Func<int, float, float> hullY = (k, t) =>
            {
                float sc2 = CrSample(cts, csc, t);
                return HalfPtR(k, t).y * H * sc2 + CrSample(cts, clf, t) * H;
            };
            System.Func<int, float, Vector3> surf = (li, t) =>
            {
                float sc2 = CrSample(cts, csc, t);
                float lift2 = CrSample(cts, clf, t) * H;
                var pt = LoopPtR(((li % LoopPts) + LoopPts) % LoopPts, t);
                return new Vector3(pt.x * W * sc2, pt.y * H * sc2 + lift2, zAt(t));
            };

            // ---- main hull loft (48-pt smoothed) ----
            var stripVerts = HullLoft48(b, rings, HalfPtRF,
                t => CrSample(cts, csc, t)
                    * (1f + g.SurfAmp * 0.012f * Mathf.Sin((t * 6f + g.SurfPhase) * Mathf.PI * 2f)),
                t => CrSample(cts, clf, t) * H, zAt, t => 1f, t => 1f, W, H,
                (so, po, tm, i) => PaintMatR3(g, so, po, tm, markCell, micro[i, so * 3 + po]));

            // Boxy dark bear nose with a teal lens, and a rump cap.
            float yBow = CrSample(cts, clf, 0f) * H;
            var bowC = new Vector3(0f, yBow - 0.03f, 0.50f * L + 0.02f);
            CapFan(b, bowC, stripVerts, 0, true, 1);
            BevelBox(b, bowC + Vector3.forward * (g.Nose * 0.5f),
                new Vector3(W * 0.22f, H * 0.16f, g.Nose * 0.5f), 3);
            BevelBox(b, bowC + Vector3.forward * (g.Nose + 0.02f),
                new Vector3(W * 0.13f, H * 0.09f, 0.02f), 2);
            var sternC = new Vector3(0f, CrSample(cts, clf, 1f) * H, -0.50f * L - 0.05f);
            CapFan(b, sternC, stripVerts, rings - 1, false, 1);

            // ---- little rounded bear ears on the crown ----
            for (int side = -1; side <= 1; side += 2)
            {
                var ear = surf(side > 0 ? 2 : 22, 0.07f) + new Vector3(0f, 0.06f, 0f);
                Ball(b, ear, g.EarSize, 1, 3, 6);
            }

            // ---- dark glass brow canopy ----
            {
                float tCan = g.CanopyStart;
                float zC = zAt(tCan);
                float halfLen = g.CanopyLen * L * 0.5f;
                System.Func<float, float> deckAt = z =>
                {
                    float t = Mathf.Clamp01((0.50f * L - z) / L);
                    float sc = CrSample(cts, csc, t);
                    return HalfPtR(0, t).y * H * sc + CrSample(cts, clf, t) * H;
                };
                Canopy(b, zC + halfLen, zC - halfLen, 0.26f, 0.15f, deckAt, 3, 1, 1);
            }

            // ---- capsule pod stacks down the flanks and shoulders ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 2; row++)
                {
                    int li = row == 0 ? 3 : 5;
                    float yAdj = row == 0 ? 0.04f : -0.02f;
                    for (int i = 0; i < g.PodCount; i++)
                    {
                        float t = 0.30f + i * 0.16f + row * 0.07f;
                        var c = surf(side > 0 ? li : LoopPts - li, t)
                            + new Vector3(side * 0.06f, yAdj, 0f);
                        Tube(b, new[] { c + Vector3.forward * (g.PodLen * 0.5f), c - Vector3.forward * (g.PodLen * 0.5f) },
                            new[] { g.PodR, g.PodR }, 8, (row + i) % 2 == 0 ? 0 : 1, true);
                        Tube(b, new[] { c + Vector3.forward * (g.PodLen * 0.5f), c + Vector3.forward * (g.PodLen * 0.5f + 0.05f) },
                            new[] { g.PodR * 0.75f, g.PodR * 0.62f }, 8, 2, true);
                        // strap bands and a hull bracket so the pod reads mounted
                        for (int st = -1; st <= 1; st += 2)
                            Tube(b, new[] { c + Vector3.forward * (st * g.PodLen * 0.28f + 0.017f),
                                    c + Vector3.forward * (st * g.PodLen * 0.28f - 0.017f) },
                                new[] { g.PodR * 1.07f, g.PodR * 1.07f }, 8, 1, false);
                        BevelBox(b, c + new Vector3(-side * g.PodR * 0.75f, 0f, 0f),
                            new Vector3(g.PodR * 0.45f, g.PodR * 0.55f, g.PodLen * 0.30f), 0.02f, 1);
                    }
                }
                for (int i = 0; i < 2; i++)
                {
                    float t = 0.40f + i * 0.22f;
                    var c = surf(side > 0 ? 1 : 23, t) + new Vector3(side * 0.02f, 0.10f, 0f);
                    Tube(b, new[] { c + Vector3.forward * (g.PodLen * 0.42f), c - Vector3.forward * (g.PodLen * 0.42f) },
                        new[] { g.PodR * 0.8f, g.PodR * 0.8f }, 7, i % 2 == 0 ? 1 : 0, true);
                    Tube(b, new[] { c + Vector3.forward * (g.PodLen * 0.42f), c + Vector3.forward * (g.PodLen * 0.42f + 0.04f) },
                        new[] { g.PodR * 0.58f, g.PodR * 0.48f }, 7, 2, true);
                }
            }

            // ---- dorsal survey radome ----
            {
                float t0 = 0.30f;
                var basePt = new Vector3(0f, hullY(0, t0) - 0.01f, zAt(t0));
                Tube(b, new[] { basePt, basePt + Vector3.up * 0.14f },
                    new[] { 0.09f, 0.07f }, 7, 1, false);
                Ball(b, basePt + Vector3.up * (0.14f + g.DomeR * 0.8f), g.DomeR, 3, 3, 8);
                Tube(b, new[] { basePt + Vector3.up * (0.14f + g.DomeR * 0.8f) + Vector3.forward * g.DomeR,
                        basePt + Vector3.up * (0.14f + g.DomeR * 0.8f) - Vector3.forward * g.DomeR },
                    new[] { g.DomeR * 0.55f, g.DomeR * 0.55f }, 7, 2, false);
            }

            // ---- twin ventral collector dishes ----
            for (int d2 = 0; d2 < 2; d2++)
            {
                float t0 = 0.42f + d2 * 0.18f;
                float y0 = hullY(12, t0);
                var basePt = new Vector3(0f, y0 + 0.02f, zAt(t0));
                Tube(b, new[] { basePt, basePt - Vector3.up * 0.20f },
                    new[] { 0.055f, 0.04f }, 6, 1, false);
                var dish = basePt - Vector3.up * 0.24f;
                Tube(b, new[] { dish, dish - Vector3.up * 0.05f },
                    new[] { 0.17f, 0.20f }, 8, 3, false);
                Ball(b, dish - Vector3.up * 0.10f, 0.055f, 2, 2, 6);
            }

            // ---- stern sensor eye ----
            {
                float y0 = hullY(0, 0.90f);
                var c = new Vector3(0f, y0 + 0.08f, zAt(0.90f));
                BevelBox(b, c, new Vector3(0.24f, 0.18f, 0.26f), 1);
                BevelBox(b, c + new Vector3(0f, 0f, -0.28f), new Vector3(0.15f, 0.13f, 0.02f), 3);
                Ball(b, c + new Vector3(0f, 0f, -0.30f), 0.085f, 2, 2, 6);
            }

            // ---- rear bank of capsule engines: 2x2 plus a center tube ----
            {
                float lift = CrSample(cts, clf, 0.95f) * H;
                for (int side = -1; side <= 1; side += 2)
                    for (int row = 0; row < 2; row++)
                    {
                        var ec = new Vector3(side * W * (0.30f + row * 0.22f),
                            (row == 0 ? 0.16f : -0.14f) * H + lift, zAt(0.90f + row * 0.03f));
                        Tube(b, new[] { ec + Vector3.forward * 0.35f, ec - Vector3.forward * 0.40f },
                            new[] { 0.14f, 0.14f }, 8, row == 0 ? 0 : 1, false);
                        Nozzle(b, ec - Vector3.forward * 0.48f, Vector3.back, 0.15f, 0.21f, 10, 1, 2);
                    }
                var core = new Vector3(0f, -0.02f * H + lift, zAt(0.94f));
                Tube(b, new[] { core + Vector3.forward * 0.30f, core - Vector3.forward * 0.50f },
                    new[] { 0.17f, 0.17f }, 8, 1, false);
                Nozzle(b, core - Vector3.forward * 0.58f, Vector3.back, 0.18f, 0.26f, 10, 1, 2);
                int n = g.EngineSegs;
                for (int i = 0; i < n; i++)
                    Tube(b, new[] { core + new Vector3(0f, 0f, 0.24f - i * 0.16f), core + new Vector3(0f, 0f, 0.17f - i * 0.16f) },
                        new[] { 0.185f, 0.185f }, 8, i % 2 == 0 ? 0 : 1, false);
            }

            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "trail3_" + hash };
            mesh.SetVertices(b.V);
            mesh.subMeshCount = 4;
            for (int m = 0; m < 4; m++) mesh.SetTriangles(b.Sub[m], m);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.materials = new[]
            {
                Metal(g.Cream, 0.88f, 0.80f),
                Metal(g.Umber, 0.90f, 0.72f),
                SystemView.Mat(new Color(0.30f, 0.95f, 0.95f), true),
                Metal(new Color(0.05f, 0.08f, 0.09f), 0.9f, 0.95f),
            };
            return go;
        }
    }
}
