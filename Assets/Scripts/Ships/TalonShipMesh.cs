using UnityEngine;
using static SpaceGame.MeshKit;

namespace SpaceGame
{
    /// <summary>
    /// Talon-class hull meshes — avian drone cruisers. C1 "Kestrel",
    /// matched to its reference: sleek silver-white raptor body with dark
    /// evergreen markings, a long dark beak, a raked smoked-glass canopy,
    /// layered feather-blade wings (three primaries over two coverts per
    /// side), a fanned tail, twin boxy engine nacelles with teal exhaust,
    /// glowing drone bay hatches on the flanks, and one chin gun.
    /// Submeshes: 0 silver-white, 1 dark evergreen, 2 teal glow, 3 glass.
    /// Streams: talonbody / talonpanels.
    /// </summary>
    public static class TalonShipMesh
    {
        const int LoopPts = 24;
        const int Spans = 24;
        static readonly int[] StripStart = { 0, 3, 6, 9, 12, 15, 18, 21 };

        public static GameObject Build(string hash, int cls, Transform shipRoot)
            => BuildC1(hash, shipRoot); // single class so far

        class GenomeT1
        {
            public float L, W, H, Beak, CanopyStart, CanopyLen;
            public float GunLen;
            public float FeatherSpan, FeatherSweep, FeatherRake;
            public float TailSpan, TailSweep;
            public int EngineSegs;
            public float Hue, Sat, Val, WhiteVal, MarkOdds, SurfAmp, SurfPhase;
            public int BandMode, BandCount;
            public float BandPhase;
            public Color Green, White;
        }

        static GenomeT1 RollT1(string hash)
        {
            var rng = Rng.Stream("talonbody:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new GenomeT1();
            g.L = R(7.8f, 8.4f);
            g.W = R(1.05f, 1.20f);
            g.H = R(0.80f, 0.92f);
            g.Beak = R(0.90f, 1.30f);
            g.CanopyStart = R(0.14f, 0.18f);
            g.CanopyLen = R(0.20f, 0.26f);
            g.GunLen = R(1.8f, 2.2f);
            g.FeatherSpan = R(2.4f, 3.0f);
            g.FeatherSweep = R(1.8f, 2.4f);
            g.FeatherRake = R(0.15f, 0.30f);
            g.TailSpan = R(1.6f, 2.1f);
            g.TailSweep = R(1.4f, 1.9f);
            g.EngineSegs = 3 + rng.Next(2);
            g.Hue = R(0.36f, 0.46f);
            g.Sat = R(0.55f, 0.75f);
            g.Val = R(0.30f, 0.45f);
            g.WhiteVal = R(0.85f, 0.93f);
            g.MarkOdds = R(0.40f, 0.65f);
            g.SurfAmp = R(0f, 0.5f);
            g.SurfPhase = R(0f, 1f);
            g.BandMode = rng.Next(3);
            g.BandCount = 2 + rng.Next(3);
            g.BandPhase = R(0f, 1f);
            g.Green = Color.HSVToRGB(g.Hue, g.Sat, g.Val);
            g.White = new Color(g.WhiteVal, g.WhiteVal + 0.01f, g.WhiteVal + 0.02f);
            return g;
        }

        // Raptor sections: slim beak sliver, deep breast, tapering tail.
        static readonly float[,] NoseT =
        {
            {0.00f, 0.18f}, {0.30f, 0.17f}, {0.55f, 0.14f}, {0.75f, 0.10f},
            {0.90f, 0.05f}, {0.98f, 0.00f}, {1.00f, -0.04f},
            {0.85f, -0.08f}, {0.62f, -0.11f}, {0.38f, -0.13f},
            {0.24f, -0.14f}, {0.10f, -0.15f}, {0.00f, -0.15f},
        };
        static readonly float[,] MidT =
        {
            {0.00f, 0.75f}, {0.32f, 0.71f}, {0.58f, 0.60f}, {0.78f, 0.42f},
            {0.92f, 0.20f}, {1.00f, -0.02f}, {0.95f, -0.22f},
            {0.83f, -0.38f}, {0.63f, -0.48f}, {0.41f, -0.55f},
            {0.26f, -0.58f}, {0.10f, -0.60f}, {0.00f, -0.61f},
        };
        static readonly float[,] SternT =
        {
            {0.00f, 0.60f}, {0.34f, 0.57f}, {0.60f, 0.49f}, {0.78f, 0.36f},
            {0.90f, 0.18f}, {0.96f, -0.01f}, {0.92f, -0.18f},
            {0.80f, -0.31f}, {0.61f, -0.40f}, {0.40f, -0.46f},
            {0.25f, -0.48f}, {0.10f, -0.50f}, {0.00f, -0.50f},
        };

        static Vector2 HalfPtT(int k, float t)
        {
            float wMid = Smooth01(t / 0.36f);
            float wStern = Smooth01((t - 0.58f) / 0.42f);
            float x = Mathf.Lerp(NoseT[k, 0], MidT[k, 0], wMid);
            float y = Mathf.Lerp(NoseT[k, 1], MidT[k, 1], wMid);
            x = Mathf.Lerp(x, SternT[k, 0], wStern);
            y = Mathf.Lerp(y, SternT[k, 1], wStern);
            return new Vector2(x, y);
        }

        static Vector2 LoopPtT(int li, float t)
        {
            if (li <= 12) return HalfPtT(li, t);
            var p = HalfPtT(LoopPts - li, t);
            return new Vector2(-p.x, p.y);
        }

        // Raptor plumage: silver-white base, dark evergreen beak, mantle
        // stripe, and feather-mark chevrons; pale breast and belly.
        static int PaintMatT(GenomeT1 g, int s, int p, float tm, bool[] markCell, bool microHit)
        {
            bool deck = s == 0 || s == 7;
            bool upper = s == 1 || s == 6;
            bool lower = s == 2 || s == 5;
            bool belly = s == 3 || s == 4;
            int side = s <= 3 ? 0 : 1;

            // Dark beak and crown.
            if (tm < 0.10f) return 1;
            if (deck && tm < 0.16f) return 1;

            // Mantle stripe down the spine.
            if (deck && p == 0 && tm > 0.24f && tm < 0.74f) return 1;

            // Feather-mark chevrons on back and flanks; breast stays pale.
            if ((deck || upper || lower) && !belly)
            {
                float skew = (deck ? 0f : upper ? 0.03f : 0.06f) + p * 0.02f;
                float ft = tm - 0.20f - skew;
                if (ft >= 0f && ft < 0.56f)
                {
                    int cell = Mathf.Min(4, (int)(ft / 0.113f));
                    int band = deck ? 0 : upper ? 1 : 2;
                    if (markCell[(side * 15 + band * 5 + cell) % 30]) return 1;
                }
            }

            // Band families: dark tail bars or collar rings.
            if (g.BandMode == 1 && tm > 0.76f && tm < 0.94f)
            {
                float u = (tm - 0.76f) / 0.18f;
                if ((int)(u * g.BandCount * 2 + g.BandPhase * 2f) % 2 == 0) return 1;
            }
            else if (g.BandMode == 2 && !belly && tm > 0.16f && tm < 0.30f)
            {
                float u = (tm - 0.16f) / 0.14f;
                if ((int)(u * g.BandCount * 2.5f + g.BandPhase * 2f) % 2 == 0) return 1;
            }

            if (microHit && !belly && tm > 0.12f && tm < 0.92f) return 1;
            return 0;
        }

        // One thin feather blade lofted between two straight chains, with a
        // raised rachis shaft down the centerline, a dark tip, and barb
        // vanes off the trailing edge.
        static void Feather(Builder b, Vector3 rootF, Vector3 rootB, Vector3 tipF, Vector3 tipB, bool flip)
        {
            var lead = new[] { rootF, tipF };
            var trail = new[] { rootB, tipB };
            var ts = new[] { 0f, 1f };
            LoftWing(b, lead, ts, trail, ts, 0.055f, 0.012f, 6, flip, 0);
            WingPlate(b, lead, ts, trail, ts, 0.055f, 0.012f, 0.55f, 0.80f, 1);
            WingPlate(b, lead, ts, trail, ts, 0.055f, 0.012f, 0.84f, 0.97f, 1);
            for (int seg = 0; seg < 4; seg++)
            {
                float u0 = 0.06f + seg * 0.22f;
                float u1 = u0 + 0.22f;
                var a = WingSurfPt(lead, ts, trail, ts, 0.055f, 0.012f, u0, 0.42f, 0.018f);
                var b2 = WingSurfPt(lead, ts, trail, ts, 0.055f, 0.012f, u0, 0.52f, 0.018f);
                var c = WingSurfPt(lead, ts, trail, ts, 0.055f, 0.012f, u1, 0.52f, 0.018f);
                var d = WingSurfPt(lead, ts, trail, ts, 0.055f, 0.012f, u1, 0.42f, 0.018f);
                b.QuadUDS(a, b2, c, d, 1);
            }
            for (int i = 0; i < 3; i++)
            {
                float u0 = 0.28f + i * 0.24f;
                var e0 = Vector3.Lerp(rootB, tipB, u0);
                var e1 = Vector3.Lerp(rootB, tipB, u0 + 0.10f);
                var backDir = (Vector3.Lerp(rootB, tipB, u0 + 0.05f)
                    - Vector3.Lerp(rootF, tipF, u0 + 0.05f)).normalized;
                var apex = (e0 + e1) * 0.5f + backDir * 0.16f;
                b.TriUDS(e0, e1, apex, 0);
            }
        }

        static GameObject BuildC1(string hash, Transform shipRoot)
        {
            var g = RollT1(hash);
            var b = new Builder();
            float L = g.L, W = g.W, H = g.H;
            const int rings = 56;

            var panelRng = Rng.Stream("talonpanels:" + hash);
            var markCell = new bool[30];
            for (int i = 0; i < 30; i++) markCell[i] = panelRng.NextDouble() < g.MarkOdds;
            var micro = new bool[rings - 1, Spans];
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.03;

            // Falcon plan: deep breast at 42%, long tail taper.
            float[] cts = { 0.00f, 0.08f, 0.20f, 0.34f, 0.48f, 0.68f, 0.86f, 1.00f };
            float[] csc = { 0.03f, 0.18f, 0.42f, 0.74f, 1.00f, 0.88f, 0.64f, 0.42f };
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
                        var pt = LoopPtT(li, t);
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
                        int mat = PaintMatT(g, s, p, tm, markCell, micro[i, s * 3 + p]);
                        b.FaceQ(stripVerts[s][i][p], stripVerts[s][i + 1][p],
                            stripVerts[s][i + 1][p + 1], stripVerts[s][i][p + 1], mat);
                    }
            }

            // Dark hooked beak and stern cap.
            var noseTip = new Vector3(0f, -0.06f * H, 0.5f * L + g.Beak);
            for (int s = 0; s < 8; s++)
                for (int p = 0; p < 3; p++)
                    b.TriU(noseTip, b.V[stripVerts[s][0][p + 1]], b.V[stripVerts[s][0][p]], 1);
            var sternC = new Vector3(0f, 0f, -0.5f * L - 0.05f);
            for (int s = 0; s < 8; s++)
                for (int p = 0; p < 3; p++)
                    b.TriU(sternC, b.V[stripVerts[s][rings - 1][p]], b.V[stripVerts[s][rings - 1][p + 1]], 1);

            // Raked smoked-glass canopy behind the beak.
            {
                float tCan = g.CanopyStart + g.CanopyLen * 0.5f;
                float zC = (0.5f - tCan) * L;
                float halfLen = g.CanopyLen * L * 0.76f;
                System.Func<float, float> deckAt = z =>
                {
                    float t = Mathf.Clamp01(0.5f - z / L);
                    float sc = CrSample(cts, csc, t);
                    return HalfPtT(0, t).y * H * sc + CrSample(cts, clf, t) * H;
                };
                Canopy(b, zC + halfLen, zC - halfLen, 0.28f, 0.18f, deckAt, 3, 1, 1);
            }

            // ---- angular avian details ----
            System.Func<int, float, float> hullY = (k, t) =>
            {
                float sc2 = CrSample(cts, csc, t);
                return HalfPtT(k, t).y * H * sc2 + CrSample(cts, clf, t) * H;
            };
            System.Func<float, float> zAt = t => (0.5f - t) * L;

            // Brow crest: faceted wedges hooding the canopy like a raptor's
            // scowl.
            for (int side = -1; side <= 1; side += 2)
            {
                float t0 = g.CanopyStart - 0.02f;
                float t1 = g.CanopyStart + g.CanopyLen + 0.04f;
                var a = new Vector3(side * 0.08f, hullY(0, t0) + 0.03f, zAt(t0));
                var b2 = new Vector3(side * 0.30f, hullY(0, t0) - 0.06f, zAt(t0) - 0.10f);
                var c = new Vector3(side * 0.34f, hullY(0, t1) - 0.02f, zAt(t1));
                var d = new Vector3(side * 0.08f, hullY(0, t1) + 0.09f, zAt(t1) - 0.06f);
                b.QuadUDS(a, b2, c, d, 1);
            }

            // Neck collar: a faceted ring of plates where head meets body.
            {
                float t0 = 0.20f;
                float scC = CrSample(cts, csc, t0) * 1.05f;
                float liftC = CrSample(cts, clf, t0) * H;
                float z = zAt(t0);
                for (int k = 0; k < 8; k++)
                {
                    int li0 = (k * 3) % LoopPts;
                    int li1 = (k * 3 + 3) % LoopPts;
                    var p0 = LoopPtT(li0, t0);
                    var p1 = LoopPtT(li1, t0);
                    var a = new Vector3(p0.x * W * scC, p0.y * H * scC + liftC, z + 0.10f);
                    var b2 = new Vector3(p1.x * W * scC, p1.y * H * scC + liftC, z + 0.10f);
                    var c = new Vector3(p1.x * W * scC, p1.y * H * scC + liftC, z - 0.10f);
                    var d = new Vector3(p0.x * W * scC, p0.y * H * scC + liftC, z - 0.10f);
                    b.QuadUDS(a, b2, c, d, k % 2 == 0 ? 1 : 0);
                }
            }

            // Mantle chevrons over the wing shoulders.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int m2 = 0; m2 < 3; m2++)
                {
                    float t0 = 0.28f + m2 * 0.08f;
                    float scM = CrSample(cts, csc, t0);
                    float liftM = CrSample(cts, clf, t0) * H;
                    var basePt = new Vector3(side * W * scM * 0.52f, H * scM * 0.40f + liftM, zAt(t0));
                    var a = basePt + new Vector3(-side * 0.04f, 0.08f, 0.10f);
                    var b2 = basePt + new Vector3(side * 0.24f, -0.04f, 0.02f);
                    var c = basePt + new Vector3(side * 0.20f, -0.08f, -0.26f);
                    var d = basePt + new Vector3(-side * 0.06f, 0.05f, -0.30f);
                    b.QuadUDS(a, b2, c, d, m2 % 2 == 0 ? 1 : 0);
                }
            }

            // Breast keel: an angular blade under the forward belly.
            {
                var kA = new Vector3(0f, hullY(12, 0.26f) + 0.02f, zAt(0.26f));
                var kB = new Vector3(0f, hullY(12, 0.38f) - 0.32f, zAt(0.38f));
                var kC = new Vector3(0f, hullY(12, 0.52f) + 0.02f, zAt(0.52f));
                var xoff = new Vector3(0.035f, 0f, 0f);
                b.TriUDS(kA + xoff, kB + xoff, kC + xoff, 0);
                b.TriUDS(kA - xoff, kB - xoff, kC - xoff, 0);
                b.QuadUDS(kA + xoff, kA - xoff, kB - xoff, kB + xoff, 1);
                b.QuadUDS(kB + xoff, kB - xoff, kC - xoff, kC + xoff, 1);
            }

            // Dorsal ridge: a row of small angular plates down the spine.
            for (int r2 = 0; r2 < 4; r2++)
            {
                float t0 = 0.46f + r2 * 0.09f;
                float z = zAt(t0);
                float y0 = hullY(0, t0);
                var a = new Vector3(0f, y0 + 0.01f, z + 0.10f);
                var apex = new Vector3(0f, y0 + 0.14f, z - 0.02f);
                var c = new Vector3(0f, y0 + 0.01f, z - 0.14f);
                b.TriUDS(a, apex, c, r2 % 2 == 0 ? 1 : 0);
            }

            // Layered feather wings: three primaries over two coverts.
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                for (int f = 0; f < 3; f++)
                {
                    float t0 = 0.30f + f * 0.10f;
                    float span = g.FeatherSpan * (1f - f * 0.16f);
                    float sweep = g.FeatherSweep * (1f - f * 0.10f);
                    var rootF2 = new Vector3(s * W * 0.42f, H * (0.30f - f * 0.06f), (0.5f - t0) * L);
                    var rootB2 = rootF2 + new Vector3(-s * 0.03f, -0.02f, -0.55f);
                    var tipF2 = rootF2 + new Vector3(s * span, span * g.FeatherRake, -sweep);
                    var tipB2 = rootB2 + new Vector3(s * span * 0.94f, span * g.FeatherRake * 0.9f, -sweep * 1.06f);
                    Feather(b, rootF2, rootB2, tipF2, tipB2, side < 0);
                }
                for (int f = 0; f < 2; f++)
                {
                    float t0 = 0.42f + f * 0.10f;
                    float span = g.FeatherSpan * (0.62f - f * 0.14f);
                    float sweep = g.FeatherSweep * (0.80f - f * 0.10f);
                    var rootF2 = new Vector3(s * W * 0.50f, -H * 0.05f, (0.5f - t0) * L);
                    var rootB2 = rootF2 + new Vector3(-s * 0.03f, -0.02f, -0.45f);
                    var tipF2 = rootF2 + new Vector3(s * span, -span * 0.08f, -sweep);
                    var tipB2 = rootB2 + new Vector3(s * span * 0.94f, -span * 0.08f, -sweep * 1.06f);
                    Feather(b, rootF2, rootB2, tipF2, tipB2, side < 0);
                }
            }

            // Fanned tail feathers: two blades per side plus a center vane.
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                for (int f = 0; f < 2; f++)
                {
                    float spread = 0.35f + f * 0.45f;
                    var rootF2 = new Vector3(s * W * 0.20f, 0.05f * H, (0.5f - 0.86f) * L);
                    var rootB2 = rootF2 + new Vector3(0f, -0.02f, -0.40f);
                    var tipF2 = rootF2 + new Vector3(s * g.TailSpan * spread, 0.08f, -g.TailSweep);
                    var tipB2 = rootB2 + new Vector3(s * g.TailSpan * spread * 0.94f, 0.06f, -g.TailSweep * 1.08f);
                    Feather(b, rootF2, rootB2, tipF2, tipB2, side < 0);
                }
            }
            {
                // center tail vane, vertical
                float scD = CrSample(cts, csc, 0.84f);
                float y0 = HalfPtT(0, 0.84f).y * H * scD + CrSample(cts, clf, 0.84f) * H;
                var rootF2 = new Vector3(0f, y0 - 0.02f, (0.5f - 0.84f) * L);
                var rootB2 = new Vector3(0f, y0 - 0.02f, (0.5f - 0.94f) * L);
                var tipF2 = new Vector3(0f, y0 + 0.55f, (0.5f - 0.84f) * L - g.TailSweep * 0.55f);
                var tipB2 = new Vector3(0f, y0 + 0.48f, (0.5f - 0.94f) * L - g.TailSweep * 0.62f);
                var lead = new[] { rootF2, tipF2 };
                var trail = new[] { rootB2, tipB2 };
                var ts = new[] { 0f, 1f };
                LoftWing(b, lead, ts, trail, ts, 0.05f, 0.012f, 5, true, 1);
            }

            // Chin gun — the single turret hardpoint.
            {
                float y = -0.30f * H;
                var housing0 = new Vector3(0f, y, (0.5f - 0.26f) * L);
                var housing1 = new Vector3(0f, y, (0.5f - 0.06f) * L);
                Tube(b, new[] { housing0, housing1 }, new[] { 0.10f, 0.088f }, 8, 1, false);
                Tube(b, new[] { housing1 - Vector3.forward * 0.045f, housing1 + Vector3.forward * 0.045f },
                    new[] { 0.105f, 0.105f }, 8, 2, false);
                var muzzle = new Vector3(0f, y, (0.5f - 0.06f) * L + g.GunLen * 0.6f);
                Tube(b, new[] { housing1, muzzle }, new[] { 0.06f, 0.045f }, 8, 1, false);
                Tube(b, new[] { muzzle, muzzle + Vector3.forward * (g.GunLen * 0.4f) },
                    new[] { 0.032f, 0.005f }, 6, 1, true);
            }

            // Glowing drone bay hatches — the two drone hardpoints.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int bay = 0; bay < 2; bay++)
                {
                    float t = 0.52f + bay * 0.12f;
                    float sc = CrSample(cts, csc, t);
                    var c = new Vector3(side * W * sc * 0.86f, 0.02f * H, (0.5f - t) * L);
                    Box(b, c, new Vector3(0.06f, 0.14f, 0.22f), 1);
                    Box(b, c + new Vector3(side * 0.045f, 0f, 0f), new Vector3(0.025f, 0.10f, 0.17f), 2);
                }
            }

            // Twin boxy engine nacelles with teal exhaust blocks.
            for (int side = -1; side <= 1; side += 2)
            {
                var ec = new Vector3(side * W * 0.58f, 0.06f * H, (0.5f - 0.86f) * L);
                Box(b, ec, new Vector3(0.26f, 0.20f, 0.55f), 1);
                Box(b, ec + new Vector3(0f, 0.05f, 0.30f), new Vector3(0.18f, 0.12f, 0.20f), 0);
                Box(b, ec + new Vector3(0f, 0f, -0.58f), new Vector3(0.18f, 0.14f, 0.06f), 2);
                int n = g.EngineSegs;
                for (int i = 0; i < n; i++)
                    Tube(b, new[] { ec + new Vector3(0f, -0.14f, 0.30f - i * 0.28f), ec + new Vector3(0f, -0.14f, 0.18f - i * 0.28f) },
                        new[] { 0.12f, 0.12f }, 8, i % 2 == 0 ? 0 : 1, false);
            }

            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "talon1_" + hash };
            mesh.SetVertices(b.V);
            mesh.subMeshCount = 4;
            for (int m = 0; m < 4; m++) mesh.SetTriangles(b.Sub[m], m);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.materials = new[]
            {
                Metal(g.White, 0.88f, 0.82f),
                Metal(g.Green, 0.90f, 0.75f),
                SystemView.Mat(new Color(0.35f, 0.95f, 0.85f), true),
                Metal(new Color(0.04f, 0.07f, 0.08f), 0.9f, 0.95f),
            };
            return go;
        }
    }
}
