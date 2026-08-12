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
    /// C2 "Berkut" is the eagle strike cruiser: heavier hull under rows of
    /// overlapping plumage shingles, a hooked bill, folded stacked wings,
    /// three drone bays per flank, and a third ventral engine.
    /// C3 "Strix" is the owl recon/EW cruiser: broad mottled hull, huge
    /// glowing eyes ringed by facial-disc petals, ear tufts, dense rounded
    /// wing stacks, sensor pod banks under the flanks, and web/disruptor
    /// emitter masts instead of any gun.
    /// Submeshes: 0 silver-white, 1 dark evergreen, 2 teal glow, 3 glass.
    /// Streams: talonbody / talonpanels (C1), talon2body / talon2panels
    /// (C2), talon3body / talon3panels (C3).
    /// </summary>
    public static class TalonShipMesh
    {
        const int LoopPts = 24;
        const int Spans = 24;

        public static GameObject Build(string hash, int cls, Transform shipRoot)
            => cls == 3 ? BuildC3(hash, shipRoot)
             : cls == 2 ? BuildC2(hash, shipRoot) : BuildC1(hash, shipRoot);

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

        static Vector2 HalfPtTF(float k, float t)
        {
            float wMid = Smooth01(t / 0.36f);
            float wStern = Smooth01((t - 0.58f) / 0.42f);
            var n = ProfCR(NoseT, k);
            var m = ProfCR(MidT, k);
            var st = ProfCR(SternT, k);
            var v = Vector2.Lerp(n, m, wMid);
            return Vector2.Lerp(v, st, wStern);
        }

        /// <summary>48-pt continuous beak-into-body loft shared by all
        /// Talon classes: bill rings narrow and droop off the face, then
        /// the same 16 strips run down the body. Beak faces are mat 1.</summary>
        static int[][][] TalonLoft(Builder b, int beakRings, int rings,
            float W, float H, float zBF, float beakLen,
            float beakBase, float scLo, float scHi, float xnLo,
            float droopPow, float droopAmt, float yBase,
            System.Func<float, float> scAt, System.Func<float, float> liftAt,
            System.Func<float, float> zAt,
            System.Func<int, int, float, int, int> paint)
        {
            int total = beakRings + rings;
            var sv = new int[16][][];
            for (int s = 0; s < 16; s++) sv[s] = new int[total][];
            for (int i = 0; i < beakRings; i++)
            {
                float ub = i / (float)beakRings;
                float sc = beakBase * Mathf.Lerp(scLo, scHi, Smooth01(ub));
                float xNarrow = Mathf.Lerp(xnLo, 0.98f, ub);
                float yOff = yBase - Mathf.Pow(1f - ub, droopPow) * droopAmt;
                float z = zBF + beakLen * (1f - ub);
                for (int s = 0; s < 16; s++)
                {
                    sv[s][i] = new int[4];
                    for (int p = 0; p < 4; p++)
                    {
                        int li = (s * 3 + p) % 48;
                        float k = li <= 24 ? li * 0.5f : (48 - li) * 0.5f;
                        var pt = HalfPtTF(k, 0f);
                        float x = li <= 24 ? pt.x : -pt.x;
                        sv[s][i][p] = b.Add(new Vector3(x * W * sc * xNarrow, pt.y * H * sc + yOff, z));
                    }
                }
            }
            for (int j = 0; j < rings; j++)
            {
                float t = j / (float)(rings - 1);
                float sc = scAt(t);
                float lift = liftAt(t);
                float z = zAt(t);
                int i = beakRings + j;
                for (int s = 0; s < 16; s++)
                {
                    sv[s][i] = new int[4];
                    for (int p = 0; p < 4; p++)
                    {
                        int li = (s * 3 + p) % 48;
                        float k = li <= 24 ? li * 0.5f : (48 - li) * 0.5f;
                        var pt = HalfPtTF(k, t);
                        float x = li <= 24 ? pt.x : -pt.x;
                        sv[s][i][p] = b.Add(new Vector3(x * W * sc, pt.y * H * sc + lift, z));
                    }
                }
            }
            for (int i = 0; i < total - 1; i++)
            {
                bool beakZone = i < beakRings;
                float tm = beakZone ? 0f : (i - beakRings + 0.5f) / (rings - 1);
                for (int s = 0; s < 16; s++)
                    for (int p = 0; p < 3; p++)
                    {
                        int go = ((s * 3 + p) % 48) / 2;
                        int mat = beakZone ? 1
                            : paint(go / 3, go % 3, tm, Mathf.Min(rings - 2, i - beakRings));
                        b.FaceQ(sv[s][i][p], sv[s][i + 1][p],
                            sv[s][i + 1][p + 1], sv[s][i][p + 1], mat);
                    }
            }
            return sv;
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

            // Dark face and beak-root wash at the front of the body.
            if (tm < 0.08f) return 1;

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
            const int rings = 96;

            var panelRng = Rng.Stream("talonpanels:" + hash);
            var markCell = new bool[30];
            for (int i = 0; i < 30; i++) markCell[i] = panelRng.NextDouble() < g.MarkOdds;
            var micro = new bool[rings - 1, Spans];
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.03;

            // ---- Mk.III layout: one body, face at the front ----
            float zBF = 0.40f * L;            // face front; the beak hooks on here
            float Lb = zBF + 0.5f * L;        // body runs back to -0.5L

            // Raptor back-line: narrow face swelling to a deep chest at 25%,
            // shoulder hump falling to a lean, dropped tail.
            float[] cts = { 0.00f, 0.08f, 0.25f, 0.40f, 0.55f, 0.72f, 0.88f, 1.00f };
            float[] csc = { 0.40f, 0.72f, 1.00f, 0.93f, 0.78f, 0.58f, 0.42f, 0.30f };
            float[] clf = { 0.10f, 0.15f, 0.16f, 0.10f, 0.03f, -0.03f, -0.08f, -0.12f };

            System.Func<float, float> zAt = t => zBF - t * Lb;
            System.Func<int, float, float> hullY = (k, t) =>
            {
                float sc2 = CrSample(cts, csc, t);
                return HalfPtT(k, t).y * H * sc2 + CrSample(cts, clf, t) * H;
            };

            // ---- one continuous loft: beak rings flow into the body ----
            // The beak is part of the hull surface: forward of the face the
            // same strip loop keeps going, narrowing and drooping into a
            // blunt dark bill. No seams, no bolted-on tube.
            const int beakRings = 14;
            var stripVerts = TalonLoft(b, beakRings, rings, W, H, zBF, g.Beak,
                0.40f, 0.28f, 0.96f, 0.55f, 1.6f, 0.22f * H, CrSample(cts, clf, 0f) * H,
                t =>
                {
                    float sc2 = CrSample(cts, csc, t);
                    return sc2 * (1f + g.SurfAmp * 0.012f * Mathf.Sin((t * 6f + g.SurfPhase) * Mathf.PI * 2f));
                },
                t => CrSample(cts, clf, t) * H, zAt,
                (so, po, tm, i) => PaintMatT(g, so, po, tm, markCell, micro[i, so * 3 + po]));

            // Blunt bill tip and tail cap.
            float yTip = CrSample(cts, clf, 0f) * H - 0.22f * H;
            var billTip = new Vector3(0f, yTip - 0.01f, zBF + g.Beak + 0.06f);
            CapFan(b, billTip, stripVerts, 0, true, 1);
            var sternC = new Vector3(0f, CrSample(cts, clf, 1f) * H, -0.5f * L - 0.05f);
            CapFan(b, sternC, stripVerts, stripVerts[0].Length - 1, false, 1);

            // ---- eyes + brow wedges on the face flanks ----
            for (int side = -1; side <= 1; side += 2)
            {
                float tE = 0.05f;
                float scE = CrSample(cts, csc, tE);
                float liftE = CrSample(cts, clf, tE) * H;
                var eye = new Vector3(side * W * scE * 0.72f, liftE + H * scE * 0.30f, zAt(tE));
                Ball(b, eye, 0.11f, 3, 3, 6);
                var a = new Vector3(side * 0.08f, hullY(0, 0.02f) + 0.02f, zAt(0.02f));
                var b2 = new Vector3(eye.x + side * 0.10f, eye.y + 0.14f, eye.z + 0.14f);
                var c = new Vector3(eye.x + side * 0.06f, eye.y + 0.16f, eye.z - 0.16f);
                var d = new Vector3(side * 0.08f, hullY(0, 0.09f) + 0.03f, zAt(0.09f));
                b.QuadUDS(a, b2, c, d, 1);
            }

            // ---- cockpit canopy on the crown, right behind the beak ----
            {
                float tCan = g.CanopyStart;
                float zC = zAt(tCan);
                float halfLen = g.CanopyLen * L * 0.55f;
                System.Func<float, float> deckAt = z =>
                {
                    float t = Mathf.Clamp01((zBF - z) / Lb);
                    float sc = CrSample(cts, csc, t);
                    return HalfPtT(0, t).y * H * sc + CrSample(cts, clf, t) * H;
                };
                Canopy(b, zC + halfLen, zC - halfLen, 0.24f, 0.15f, deckAt, 3, 1, 1);
            }

            // ---- nape crest sweeping back off the crown ----
            for (int i = 0; i < 3; i++)
            {
                float xoff = (i - 1) * 0.09f;
                float len = i == 1 ? 0.85f : 0.60f;
                float tN = g.CanopyStart + g.CanopyLen + 0.06f;
                var basePt = new Vector3(xoff, hullY(0, tN) - 0.01f, zAt(tN));
                var dir = new Vector3((i - 1) * 0.10f, 0.40f, -0.90f).normalized;
                Tube(b, new[] { basePt, basePt + dir * (len * 0.5f), basePt + dir * len },
                    new[] { 0.05f, 0.032f, 0.005f }, 5, 1, true);
            }

            // ---- angular avian details on the body ----
            // Faceted collar ring marking the head/body boundary.
            {
                float t0 = 0.13f;
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
                    float t0 = 0.14f + m2 * 0.08f;
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

            // Breast keel under the deep chest.
            {
                var kA = new Vector3(0f, hullY(12, 0.12f) + 0.02f, zAt(0.12f));
                var kB = new Vector3(0f, hullY(12, 0.26f) - 0.30f, zAt(0.26f));
                var kC = new Vector3(0f, hullY(12, 0.44f) + 0.02f, zAt(0.44f));
                var xoff = new Vector3(0.035f, 0f, 0f);
                b.TriUDS(kA + xoff, kB + xoff, kC + xoff, 0);
                b.TriUDS(kA - xoff, kB - xoff, kC - xoff, 0);
                b.QuadUDS(kA + xoff, kA - xoff, kB - xoff, kB + xoff, 1);
                b.QuadUDS(kB + xoff, kB - xoff, kC - xoff, kC + xoff, 1);
            }

            // Dorsal ridge plates down the falling spine.
            for (int r2 = 0; r2 < 4; r2++)
            {
                float t0 = 0.38f + r2 * 0.10f;
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
                    float t0 = 0.18f + f * 0.09f;
                    float span = g.FeatherSpan * (1f - f * 0.16f);
                    float sweep = g.FeatherSweep * (1f - f * 0.10f);
                    float liftW = CrSample(cts, clf, t0) * H;
                    var rootF2 = new Vector3(s * W * 0.42f, H * (0.30f - f * 0.06f) + liftW, zAt(t0));
                    var rootB2 = rootF2 + new Vector3(-s * 0.03f, -0.02f, -0.55f);
                    var tipF2 = rootF2 + new Vector3(s * span, span * g.FeatherRake, -sweep);
                    var tipB2 = rootB2 + new Vector3(s * span * 0.94f, span * g.FeatherRake * 0.9f, -sweep * 1.06f);
                    Feather(b, rootF2, rootB2, tipF2, tipB2, side < 0);
                }
                for (int f = 0; f < 2; f++)
                {
                    float t0 = 0.32f + f * 0.09f;
                    float span = g.FeatherSpan * (0.62f - f * 0.14f);
                    float sweep = g.FeatherSweep * (0.80f - f * 0.10f);
                    float liftW = CrSample(cts, clf, t0) * H;
                    var rootF2 = new Vector3(s * W * 0.50f, -H * 0.05f + liftW, zAt(t0));
                    var rootB2 = rootF2 + new Vector3(-s * 0.03f, -0.02f, -0.45f);
                    var tipF2 = rootF2 + new Vector3(s * span, -span * 0.08f, -sweep);
                    var tipB2 = rootB2 + new Vector3(s * span * 0.94f, -span * 0.08f, -sweep * 1.06f);
                    Feather(b, rootF2, rootB2, tipF2, tipB2, side < 0);
                }
            }

            // Fanned tail feathers on the lean tail, plus a center vane.
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                for (int f = 0; f < 2; f++)
                {
                    float spread = 0.35f + f * 0.45f;
                    float liftT = CrSample(cts, clf, 0.86f) * H;
                    var rootF2 = new Vector3(s * W * 0.16f, liftT + 0.05f * H, zAt(0.86f));
                    var rootB2 = rootF2 + new Vector3(0f, -0.02f, -0.40f);
                    var tipF2 = rootF2 + new Vector3(s * g.TailSpan * spread, 0.08f, -g.TailSweep);
                    var tipB2 = rootB2 + new Vector3(s * g.TailSpan * spread * 0.94f, 0.06f, -g.TailSweep * 1.08f);
                    Feather(b, rootF2, rootB2, tipF2, tipB2, side < 0);
                }
            }
            {
                float y0 = hullY(0, 0.84f);
                var rootF2 = new Vector3(0f, y0 - 0.02f, zAt(0.84f));
                var rootB2 = new Vector3(0f, y0 - 0.02f, zAt(0.94f));
                var tipF2 = new Vector3(0f, y0 + 0.55f, zAt(0.84f) - g.TailSweep * 0.55f);
                var tipB2 = new Vector3(0f, y0 + 0.48f, zAt(0.94f) - g.TailSweep * 0.62f);
                var lead = new[] { rootF2, tipF2 };
                var trail = new[] { rootB2, tipB2 };
                var ts = new[] { 0f, 1f };
                LoftWing(b, lead, ts, trail, ts, 0.05f, 0.012f, 5, true, 1);
            }

            // Chin gun: a stubby underslung cannon with a blunt muzzle
            // brake, tucked well behind the bill so the beak owns the front.
            {
                float y = hullY(12, 0.12f) + 0.05f;
                var housing0 = new Vector3(0f, y, zAt(0.26f));
                var housing1 = new Vector3(0f, y, zAt(0.12f));
                Tube(b, new[] { housing0, housing1 }, new[] { 0.10f, 0.088f }, 8, 1, false);
                Tube(b, new[] { housing1 - Vector3.forward * 0.045f, housing1 + Vector3.forward * 0.045f },
                    new[] { 0.105f, 0.105f }, 8, 2, false);
                var muzzle = housing1 + Vector3.forward * (g.GunLen * 0.30f);
                Tube(b, new[] { housing1, muzzle }, new[] { 0.060f, 0.052f }, 8, 1, false);
                Tube(b, new[] { muzzle, muzzle + Vector3.forward * 0.12f },
                    new[] { 0.066f, 0.060f }, 8, 1, true);
            }

            // Glowing drone bay hatches — the two drone hardpoints.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int bay = 0; bay < 2; bay++)
                {
                    float t = 0.46f + bay * 0.12f;
                    float sc = CrSample(cts, csc, t);
                    float liftD = CrSample(cts, clf, t) * H;
                    var c = new Vector3(side * W * sc * 0.86f, 0.02f * H + liftD, zAt(t));
                    BevelBox(b, c, new Vector3(0.06f, 0.14f, 0.22f), 1);
                    BevelBox(b, c + new Vector3(side * 0.045f, 0f, 0f), new Vector3(0.025f, 0.10f, 0.17f), 2);
                }
            }

            // Twin boxy engine nacelles tucked against the lean tail.
            for (int side = -1; side <= 1; side += 2)
            {
                float liftE = CrSample(cts, clf, 0.86f) * H;
                var ec = new Vector3(side * W * 0.38f, 0.06f * H + liftE, zAt(0.86f));
                BevelBox(b, ec, new Vector3(0.26f, 0.20f, 0.55f), 1);
                BevelBox(b, ec + new Vector3(0f, 0.05f, 0.30f), new Vector3(0.18f, 0.12f, 0.20f), 0);
                Nozzle(b, ec + new Vector3(0f, 0f, -0.61f), Vector3.back, 0.20f, 0.28f, 10, 1, 2);
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

        // ==================== Class 2 "Berkut" ====================

        class GenomeT2
        {
            public float L, W, H, Beak, Hook, CanopyStart, CanopyLen;
            public float GunLen;
            public float FeatherSpan, FeatherSweep, FeatherRake;
            public float TailSpan, TailSweep;
            public int EngineSegs;
            public float Hue, Sat, Val, WhiteVal, MarkOdds, SurfAmp, SurfPhase;
            public int BandMode, BandCount;
            public float BandPhase;
            public Color Green, White;
        }

        static GenomeT2 RollT2(string hash)
        {
            var rng = Rng.Stream("talon2body:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new GenomeT2();
            g.L = R(10.4f, 11.2f);
            g.W = R(1.45f, 1.65f);
            g.H = R(1.05f, 1.20f);
            g.Beak = R(1.3f, 1.7f);
            g.Hook = R(0.26f, 0.40f);
            g.CanopyStart = R(0.13f, 0.17f);
            g.CanopyLen = R(0.18f, 0.24f);
            g.GunLen = R(2.0f, 2.4f);
            g.FeatherSpan = R(3.2f, 3.9f);
            g.FeatherSweep = R(2.6f, 3.3f);
            g.FeatherRake = R(0.08f, 0.20f);
            g.TailSpan = R(2.0f, 2.6f);
            g.TailSweep = R(1.8f, 2.4f);
            g.EngineSegs = 3 + rng.Next(2);
            g.Hue = R(0.36f, 0.46f);
            g.Sat = R(0.50f, 0.70f);
            g.Val = R(0.20f, 0.32f);
            g.WhiteVal = R(0.85f, 0.93f);
            g.MarkOdds = R(0.45f, 0.70f);
            g.SurfAmp = R(0f, 0.5f);
            g.SurfPhase = R(0f, 1f);
            g.BandMode = rng.Next(3);
            g.BandCount = 2 + rng.Next(3);
            g.BandPhase = R(0f, 1f);
            g.Green = Color.HSVToRGB(g.Hue, g.Sat, g.Val);
            g.White = new Color(g.WhiteVal, g.WhiteVal + 0.01f, g.WhiteVal + 0.02f);
            return g;
        }

        // Eagle plumage: white head and belly, a dark slate saddle across
        // the back and shoulders broken by pale feather patches, scattered
        // flank chevrons, and tail bars or a dark neck collar.
        static int PaintMatT2(GenomeT2 g, int s, int p, float tm, bool[] markCell, bool microHit)
        {
            bool deck = s == 0 || s == 7;
            bool upper = s == 1 || s == 6;
            bool lower = s == 2 || s == 5;
            bool belly = s == 3 || s == 4;
            int side = s <= 3 ? 0 : 1;

            // White eagle head — the dark bill is painted by the beak zone.
            if (tm < 0.16f) return 0;

            // Dark saddle over back and shoulders, pale patches punched out.
            if ((deck || upper) && tm > 0.20f && tm < 0.78f)
            {
                float ft = tm - 0.20f - p * 0.015f;
                int cell = Mathf.Min(4, (int)(ft / 0.116f));
                int band = deck ? 0 : 1;
                return markCell[(side * 15 + band * 5 + cell) % 30] ? 1 : 0;
            }

            // Scattered chevrons along the lower flanks.
            if (lower && tm > 0.30f && tm < 0.75f)
            {
                int cell = Mathf.Min(4, (int)((tm - 0.30f) / 0.09f));
                if (markCell[(side * 15 + 10 + cell) % 30]) return 1;
            }

            // Band families: dark tail bars or a collar on the white neck.
            if (g.BandMode == 1 && tm > 0.80f && tm < 0.96f)
            {
                float u = (tm - 0.80f) / 0.16f;
                if ((int)(u * g.BandCount * 2 + g.BandPhase * 2f) % 2 == 0) return 1;
            }
            else if (g.BandMode == 2 && !belly && tm > 0.17f && tm < 0.28f)
            {
                float u = (tm - 0.17f) / 0.11f;
                if ((int)(u * g.BandCount * 2.5f + g.BandPhase * 2f) % 2 == 0) return 1;
            }

            if (microHit && !belly && tm > 0.18f && tm < 0.94f) return 1;
            return 0;
        }

        static GameObject BuildC2(string hash, Transform shipRoot)
        {
            var g = RollT2(hash);
            var b = new Builder();
            float L = g.L, W = g.W, H = g.H;
            const int rings = 104;

            var panelRng = Rng.Stream("talon2panels:" + hash);
            var markCell = new bool[30];
            for (int i = 0; i < 30; i++) markCell[i] = panelRng.NextDouble() < g.MarkOdds;
            var micro = new bool[rings - 1, Spans];
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.03;

            float zBF = 0.38f * L;
            float Lb = zBF + 0.5f * L;

            // Eagle back-line: deeper chest and a higher shoulder hump than
            // the Kestrel, falling away to a heavy dropped tail.
            float[] cts = { 0.00f, 0.08f, 0.25f, 0.40f, 0.55f, 0.72f, 0.88f, 1.00f };
            float[] csc = { 0.42f, 0.75f, 1.00f, 0.96f, 0.84f, 0.66f, 0.48f, 0.34f };
            float[] clf = { 0.12f, 0.18f, 0.20f, 0.14f, 0.06f, -0.02f, -0.08f, -0.14f };

            System.Func<float, float> zAt = t => zBF - t * Lb;
            System.Func<int, float, float> hullY = (k, t) =>
            {
                float sc2 = CrSample(cts, csc, t);
                return HalfPtT(k, t).y * H * sc2 + CrSample(cts, clf, t) * H;
            };
            System.Func<int, float, Vector3> surf = (li, t) =>
            {
                float sc2 = CrSample(cts, csc, t);
                float lift2 = CrSample(cts, clf, t) * H;
                var pt = LoopPtT(((li % LoopPts) + LoopPts) % LoopPts, t);
                return new Vector3(pt.x * W * sc2, pt.y * H * sc2 + lift2, zAt(t));
            };

            // ---- continuous loft with a hooked bill ----
            const int beakRings = 16;
            var stripVerts = TalonLoft(b, beakRings, rings, W, H, zBF, g.Beak,
                0.42f, 0.30f, 0.97f, 0.50f, 1.35f, g.Hook * H, CrSample(cts, clf, 0f) * H,
                t =>
                {
                    float sc2 = CrSample(cts, csc, t);
                    return sc2 * (1f + g.SurfAmp * 0.012f * Mathf.Sin((t * 6f + g.SurfPhase) * Mathf.PI * 2f));
                },
                t => CrSample(cts, clf, t) * H, zAt,
                (so, po, tm, i) => PaintMatT2(g, so, po, tm, markCell, micro[i, so * 3 + po]));

            // Hooked bill tip dropping below the last ring, and a tail cap.
            float yTip = CrSample(cts, clf, 0f) * H - g.Hook * H;
            var billTip = new Vector3(0f, yTip - 0.16f, zBF + g.Beak + 0.04f);
            CapFan(b, billTip, stripVerts, 0, true, 1);
            var sternC = new Vector3(0f, CrSample(cts, clf, 1f) * H, -0.5f * L - 0.06f);
            CapFan(b, sternC, stripVerts, stripVerts[0].Length - 1, false, 1);

            // ---- eyes + heavy brow wedges ----
            for (int side = -1; side <= 1; side += 2)
            {
                float tE = 0.045f;
                float scE = CrSample(cts, csc, tE);
                float liftE = CrSample(cts, clf, tE) * H;
                var eye = new Vector3(side * W * scE * 0.72f, liftE + H * scE * 0.30f, zAt(tE));
                Ball(b, eye, 0.13f, 3, 3, 6);
                var a = new Vector3(side * 0.09f, hullY(0, 0.015f) + 0.02f, zAt(0.015f));
                var b2 = new Vector3(eye.x + side * 0.12f, eye.y + 0.17f, eye.z + 0.16f);
                var c = new Vector3(eye.x + side * 0.07f, eye.y + 0.19f, eye.z - 0.18f);
                var d = new Vector3(side * 0.09f, hullY(0, 0.085f) + 0.03f, zAt(0.085f));
                b.QuadUDS(a, b2, c, d, 1);
            }

            // ---- canopy on the crown ----
            {
                float tCan = g.CanopyStart;
                float zC = zAt(tCan);
                float halfLen = g.CanopyLen * L * 0.50f;
                System.Func<float, float> deckAt = z =>
                {
                    float t = Mathf.Clamp01((zBF - z) / Lb);
                    float sc = CrSample(cts, csc, t);
                    return HalfPtT(0, t).y * H * sc + CrSample(cts, clf, t) * H;
                };
                Canopy(b, zC + halfLen, zC - halfLen, 0.28f, 0.17f, deckAt, 3, 1, 1);
            }

            // ---- nape crest: four swept spikes ----
            for (int i = 0; i < 4; i++)
            {
                float xoff = (i - 1.5f) * 0.08f;
                float len = (i == 1 || i == 2) ? 0.95f : 0.65f;
                float tN = g.CanopyStart + g.CanopyLen + 0.06f;
                var basePt = new Vector3(xoff, hullY(0, tN) - 0.01f, zAt(tN));
                var dir = new Vector3((i - 1.5f) * 0.08f, 0.42f, -0.90f).normalized;
                Tube(b, new[] { basePt, basePt + dir * (len * 0.5f), basePt + dir * len },
                    new[] { 0.055f, 0.035f, 0.005f }, 5, 1, true);
            }

            // ---- faceted collar where the white head meets the saddle ----
            {
                float t0 = 0.17f;
                float scC = CrSample(cts, csc, t0) * 1.05f;
                float liftC = CrSample(cts, clf, t0) * H;
                float z = zAt(t0);
                for (int k = 0; k < 8; k++)
                {
                    int li0 = (k * 3) % LoopPts;
                    int li1 = (k * 3 + 3) % LoopPts;
                    var p0 = LoopPtT(li0, t0);
                    var p1 = LoopPtT(li1, t0);
                    var a = new Vector3(p0.x * W * scC, p0.y * H * scC + liftC, z + 0.11f);
                    var b2 = new Vector3(p1.x * W * scC, p1.y * H * scC + liftC, z + 0.11f);
                    var c = new Vector3(p1.x * W * scC, p1.y * H * scC + liftC, z - 0.11f);
                    var d = new Vector3(p0.x * W * scC, p0.y * H * scC + liftC, z - 0.11f);
                    b.QuadUDS(a, b2, c, d, k % 2 == 0 ? 1 : 0);
                }
            }

            // ---- plumage shingles: four overlapping rows over the back ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int r2 = 0; r2 < 4; r2++)
                {
                    float t0 = 0.22f + r2 * 0.12f;
                    float t1 = t0 + 0.07f;
                    for (int li = 1; li <= 3; li++)
                    {
                        int liA = side > 0 ? li : LoopPts - li;
                        int liB = side > 0 ? li + 1 : LoopPts - li - 1;
                        var a = surf(liA, t0); var b2 = surf(liB, t0);
                        var c = surf(liB, t1); var d = surf(liA, t1);
                        var raise = new Vector3(0f, 0.035f, 0f);
                        a += raise + new Vector3(a.x * 0.03f, 0f, 0f);
                        b2 += raise + new Vector3(b2.x * 0.03f, 0f, 0f);
                        c += raise + new Vector3(c.x * 0.03f, -0.05f, 0f);
                        d += raise + new Vector3(d.x * 0.03f, -0.05f, 0f);
                        int mat = markCell[(r2 * 6 + li + (side > 0 ? 0 : 3)) % 30] ? 1 : 0;
                        b.QuadUDS(a, b2, c, d, mat);
                        var apex = (c + d) * 0.5f + new Vector3(0f, -0.03f, -0.14f);
                        b.TriUDS(c, d, apex, 1);
                    }
                }
            }

            // ---- breast keel under the deep chest ----
            {
                var kA = new Vector3(0f, hullY(12, 0.12f) + 0.02f, zAt(0.12f));
                var kB = new Vector3(0f, hullY(12, 0.28f) - 0.38f, zAt(0.28f));
                var kC = new Vector3(0f, hullY(12, 0.46f) + 0.02f, zAt(0.46f));
                var xoff = new Vector3(0.04f, 0f, 0f);
                b.TriUDS(kA + xoff, kB + xoff, kC + xoff, 0);
                b.TriUDS(kA - xoff, kB - xoff, kC - xoff, 0);
                b.QuadUDS(kA + xoff, kA - xoff, kB - xoff, kB + xoff, 1);
                b.QuadUDS(kB + xoff, kB - xoff, kC - xoff, kC + xoff, 1);
            }

            // ---- dorsal ridge plates down the falling spine ----
            for (int r2 = 0; r2 < 5; r2++)
            {
                float t0 = 0.36f + r2 * 0.09f;
                float z = zAt(t0);
                float y0 = hullY(0, t0);
                var a = new Vector3(0f, y0 + 0.01f, z + 0.11f);
                var apex = new Vector3(0f, y0 + 0.16f, z - 0.02f);
                var c = new Vector3(0f, y0 + 0.01f, z - 0.15f);
                b.TriUDS(a, apex, c, r2 % 2 == 0 ? 1 : 0);
            }

            // ---- folded wing stacks: four primaries over three coverts ----
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                for (int f = 0; f < 4; f++)
                {
                    float t0 = 0.20f + f * 0.075f;
                    float span = g.FeatherSpan * (1f - f * 0.13f);
                    float sweep = g.FeatherSweep * (1f + f * 0.05f);
                    float liftW = CrSample(cts, clf, t0) * H;
                    var rootF2 = new Vector3(s * W * 0.44f, H * (0.32f - f * 0.05f) + liftW, zAt(t0));
                    var rootB2 = rootF2 + new Vector3(-s * 0.03f, -0.02f, -0.60f);
                    var tipF2 = rootF2 + new Vector3(s * span, span * g.FeatherRake, -sweep);
                    var tipB2 = rootB2 + new Vector3(s * span * 0.94f, span * g.FeatherRake * 0.9f, -sweep * 1.06f);
                    Feather(b, rootF2, rootB2, tipF2, tipB2, side < 0);
                }
                for (int f = 0; f < 3; f++)
                {
                    float t0 = 0.36f + f * 0.08f;
                    float span = g.FeatherSpan * (0.58f - f * 0.11f);
                    float sweep = g.FeatherSweep * (0.72f - f * 0.08f);
                    float liftW = CrSample(cts, clf, t0) * H;
                    var rootF2 = new Vector3(s * W * 0.52f, -H * 0.02f + liftW, zAt(t0));
                    var rootB2 = rootF2 + new Vector3(-s * 0.03f, -0.02f, -0.45f);
                    var tipF2 = rootF2 + new Vector3(s * span, -span * 0.06f, -sweep);
                    var tipB2 = rootB2 + new Vector3(s * span * 0.94f, -span * 0.06f, -sweep * 1.06f);
                    Feather(b, rootF2, rootB2, tipF2, tipB2, side < 0);
                }
            }

            // ---- fanned tail: three feathers per side + center vane ----
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                for (int f = 0; f < 3; f++)
                {
                    float spread = 0.30f + f * 0.35f;
                    float liftT = CrSample(cts, clf, 0.86f) * H;
                    var rootF2 = new Vector3(s * W * 0.16f, liftT + 0.05f * H, zAt(0.86f));
                    var rootB2 = rootF2 + new Vector3(0f, -0.02f, -0.44f);
                    var tipF2 = rootF2 + new Vector3(s * g.TailSpan * spread, 0.10f, -g.TailSweep);
                    var tipB2 = rootB2 + new Vector3(s * g.TailSpan * spread * 0.94f, 0.08f, -g.TailSweep * 1.08f);
                    Feather(b, rootF2, rootB2, tipF2, tipB2, side < 0);
                }
            }
            {
                float y0 = hullY(0, 0.84f);
                var rootF2 = new Vector3(0f, y0 - 0.02f, zAt(0.84f));
                var rootB2 = new Vector3(0f, y0 - 0.02f, zAt(0.94f));
                var tipF2 = new Vector3(0f, y0 + 0.65f, zAt(0.84f) - g.TailSweep * 0.60f);
                var tipB2 = new Vector3(0f, y0 + 0.56f, zAt(0.94f) - g.TailSweep * 0.68f);
                var lead = new[] { rootF2, tipF2 };
                var trail = new[] { rootB2, tipB2 };
                var ts = new[] { 0f, 1f };
                LoftWing(b, lead, ts, trail, ts, 0.055f, 0.012f, 5, true, 1);
            }

            // ---- chin gun: stubby underslung cannon, tucked behind the bill ----
            {
                float y = hullY(12, 0.12f) + 0.06f;
                var housing0 = new Vector3(0f, y, zAt(0.28f));
                var housing1 = new Vector3(0f, y, zAt(0.13f));
                Tube(b, new[] { housing0, housing1 }, new[] { 0.12f, 0.105f }, 8, 1, false);
                Tube(b, new[] { housing1 - Vector3.forward * 0.05f, housing1 + Vector3.forward * 0.05f },
                    new[] { 0.125f, 0.125f }, 8, 2, false);
                var muzzle = housing1 + Vector3.forward * (g.GunLen * 0.30f);
                Tube(b, new[] { housing1, muzzle }, new[] { 0.070f, 0.060f }, 8, 1, false);
                Tube(b, new[] { muzzle, muzzle + Vector3.forward * 0.14f },
                    new[] { 0.078f, 0.070f }, 8, 1, true);
            }

            // ---- three glowing drone bays per flank ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int bay = 0; bay < 3; bay++)
                {
                    float t = 0.40f + bay * 0.11f;
                    float sc = CrSample(cts, csc, t);
                    float liftD = CrSample(cts, clf, t) * H;
                    var c = new Vector3(side * W * sc * 0.86f, 0.02f * H + liftD, zAt(t));
                    BevelBox(b, c, new Vector3(0.07f, 0.16f, 0.24f), 1);
                    BevelBox(b, c + new Vector3(side * 0.05f, 0f, 0f), new Vector3(0.03f, 0.12f, 0.19f), 2);
                }
            }

            // ---- twin nacelles plus a third ventral engine ----
            for (int side = -1; side <= 1; side += 2)
            {
                float liftE = CrSample(cts, clf, 0.87f) * H;
                var ec = new Vector3(side * W * 0.42f, 0.06f * H + liftE, zAt(0.87f));
                BevelBox(b, ec, new Vector3(0.30f, 0.24f, 0.62f), 1);
                BevelBox(b, ec + new Vector3(0f, 0.06f, 0.34f), new Vector3(0.20f, 0.14f, 0.22f), 0);
                Nozzle(b, ec + new Vector3(0f, 0f, -0.69f), Vector3.back, 0.22f, 0.31f, 10, 1, 2);
                int n = g.EngineSegs;
                for (int i = 0; i < n; i++)
                    Tube(b, new[] { ec + new Vector3(0f, -0.17f, 0.34f - i * 0.30f), ec + new Vector3(0f, -0.17f, 0.20f - i * 0.30f) },
                        new[] { 0.14f, 0.14f }, 8, i % 2 == 0 ? 0 : 1, false);
            }
            {
                float liftE = CrSample(cts, clf, 0.90f) * H;
                var ec = new Vector3(0f, -H * 0.34f + liftE, zAt(0.90f));
                BevelBox(b, ec, new Vector3(0.22f, 0.18f, 0.50f), 1);
                Nozzle(b, ec + new Vector3(0f, 0f, -0.57f), Vector3.back, 0.165f, 0.23f, 10, 1, 2);
                for (int i = 0; i < 2; i++)
                    Tube(b, new[] { ec + new Vector3(0f, -0.13f, 0.24f - i * 0.30f), ec + new Vector3(0f, -0.13f, 0.10f - i * 0.30f) },
                        new[] { 0.12f, 0.12f }, 8, i % 2 == 0 ? 0 : 1, false);
            }

            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "talon2_" + hash };
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

        // ==================== Class 3 "Strix" ====================

        class GenomeT3
        {
            public float L, W, H, Beak, Hook, CanopyStart, CanopyLen;
            public float EyeR, TuftLen;
            public float FeatherSpan, FeatherSweep, FeatherRake;
            public float TailSpan, TailSweep;
            public int EngineSegs;
            public float Hue, Sat, Val, WhiteVal, MarkOdds, SurfAmp, SurfPhase;
            public int BandMode, BandCount;
            public float BandPhase;
            public Color Green, White;
        }

        static GenomeT3 RollT3(string hash)
        {
            var rng = Rng.Stream("talon3body:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);
            var g = new GenomeT3();
            g.L = R(9.6f, 10.4f);
            g.W = R(1.70f, 1.90f);
            g.H = R(1.15f, 1.30f);
            g.Beak = R(0.7f, 1.0f);
            g.Hook = R(0.30f, 0.42f);
            g.CanopyStart = R(0.12f, 0.16f);
            g.CanopyLen = R(0.16f, 0.22f);
            g.EyeR = R(0.17f, 0.22f);
            g.TuftLen = R(0.5f, 0.8f);
            g.FeatherSpan = R(3.4f, 4.1f);
            g.FeatherSweep = R(2.2f, 2.8f);
            g.FeatherRake = R(0.02f, 0.10f);
            g.TailSpan = R(1.8f, 2.3f);
            g.TailSweep = R(1.5f, 2.0f);
            g.EngineSegs = 3 + rng.Next(2);
            g.Hue = R(0.36f, 0.46f);
            g.Sat = R(0.45f, 0.65f);
            g.Val = R(0.16f, 0.26f);
            g.WhiteVal = R(0.86f, 0.94f);
            g.MarkOdds = R(0.40f, 0.60f);
            g.SurfAmp = R(0f, 0.5f);
            g.SurfPhase = R(0f, 1f);
            g.BandMode = rng.Next(3);
            g.BandCount = 2 + rng.Next(3);
            g.BandPhase = R(0f, 1f);
            g.Green = Color.HSVToRGB(g.Hue, g.Sat, g.Val);
            g.White = new Color(g.WhiteVal, g.WhiteVal + 0.01f, g.WhiteVal + 0.02f);
            return g;
        }

        // Owl plumage: pale mottled face, near-black back flecked with
        // white speckles, a barred breast, and dense micro-mottling all
        // over — the night camouflage of a recon hull.
        static int PaintMatT3(GenomeT3 g, int s, int p, float tm, bool[] markCell, bool microHit)
        {
            bool deck = s == 0 || s == 7;
            bool upper = s == 1 || s == 6;
            bool lower = s == 2 || s == 5;
            bool belly = s == 3 || s == 4;
            int side = s <= 3 ? 0 : 1;

            // Pale owl face; mottling starts behind the facial disc.
            if (tm < 0.14f) return 0;

            // Near-black back with white speckle cells punched through.
            if ((deck || upper) && tm > 0.16f && tm < 0.88f)
            {
                float ft = tm - 0.16f - p * 0.02f;
                int cell = Mathf.Min(4, (int)(ft / 0.144f));
                int band = deck ? 0 : 1;
                return markCell[(side * 15 + band * 5 + cell) % 30] ? 0 : 1;
            }

            // Barred owl breast: dark bars across the pale belly.
            if (belly && tm > 0.22f && tm < 0.72f)
            {
                float u = (tm - 0.22f) / 0.50f;
                if ((int)(u * g.BandCount * 3 + g.BandPhase * 2f) % 3 == 0) return 1;
            }

            // Scattered chevrons along the lower flanks.
            if (lower && tm > 0.24f && tm < 0.80f)
            {
                int cell = Mathf.Min(4, (int)((tm - 0.24f) / 0.112f));
                if (markCell[(side * 15 + 10 + cell) % 30]) return 1;
            }

            // Band families: dark tail bars or a speckled collar.
            if (g.BandMode == 1 && tm > 0.82f && tm < 0.97f)
            {
                float u = (tm - 0.82f) / 0.15f;
                if ((int)(u * g.BandCount * 2 + g.BandPhase * 2f) % 2 == 0) return 1;
            }
            else if (g.BandMode == 2 && !belly && tm > 0.15f && tm < 0.24f)
            {
                float u = (tm - 0.15f) / 0.09f;
                if ((int)(u * g.BandCount * 2.5f + g.BandPhase * 2f) % 2 == 0) return 1;
            }

            if (microHit && (lower || belly) && tm > 0.14f && tm < 0.96f) return 1;
            return 0;
        }

        static GameObject BuildC3(string hash, Transform shipRoot)
        {
            var g = RollT3(hash);
            var b = new Builder();
            float L = g.L, W = g.W, H = g.H;
            const int rings = 104;

            var panelRng = Rng.Stream("talon3panels:" + hash);
            var markCell = new bool[30];
            for (int i = 0; i < 30; i++) markCell[i] = panelRng.NextDouble() < g.MarkOdds;
            var micro = new bool[rings - 1, Spans];
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < Spans; j++)
                    micro[i, j] = panelRng.NextDouble() < 0.08;

            float zBF = 0.36f * L;
            float Lb = zBF + 0.5f * L;

            // Owl back-line: round and stocky — a full chest right behind
            // the face, staying deep amidships before a short falling tail.
            float[] cts = { 0.00f, 0.08f, 0.25f, 0.40f, 0.55f, 0.72f, 0.88f, 1.00f };
            float[] csc = { 0.50f, 0.80f, 1.00f, 0.97f, 0.88f, 0.72f, 0.52f, 0.36f };
            float[] clf = { 0.10f, 0.16f, 0.18f, 0.13f, 0.06f, -0.01f, -0.07f, -0.13f };

            System.Func<float, float> zAt = t => zBF - t * Lb;
            System.Func<int, float, float> hullY = (k, t) =>
            {
                float sc2 = CrSample(cts, csc, t);
                return HalfPtT(k, t).y * H * sc2 + CrSample(cts, clf, t) * H;
            };

            // ---- continuous loft with a short hooked owl bill ----
            const int beakRings = 14;
            var stripVerts = TalonLoft(b, beakRings, rings, W, H, zBF, g.Beak,
                0.50f, 0.30f, 0.97f, 0.55f, 1.35f, g.Hook * H, CrSample(cts, clf, 0f) * H,
                t =>
                {
                    float sc2 = CrSample(cts, csc, t);
                    return sc2 * (1f + g.SurfAmp * 0.012f * Mathf.Sin((t * 6f + g.SurfPhase) * Mathf.PI * 2f));
                },
                t => CrSample(cts, clf, t) * H, zAt,
                (so, po, tm, i) => PaintMatT3(g, so, po, tm, markCell, micro[i, so * 3 + po]));

            // Sharp hooked bill point and tail cap.
            float yTip = CrSample(cts, clf, 0f) * H - g.Hook * H;
            var billTip = new Vector3(0f, yTip - 0.14f, zBF + g.Beak + 0.16f);
            CapFan(b, billTip, stripVerts, 0, true, 1);
            var sternC = new Vector3(0f, CrSample(cts, clf, 1f) * H, -0.5f * L - 0.06f);
            CapFan(b, sternC, stripVerts, stripVerts[0].Length - 1, false, 1);

            // ---- huge glowing owl eyes ringed by facial-disc petals ----
            for (int side = -1; side <= 1; side += 2)
            {
                float tE = 0.05f;
                float scE = CrSample(cts, csc, tE);
                float liftE = CrSample(cts, clf, tE) * H;
                var eye = new Vector3(side * W * scE * 0.58f, liftE + H * scE * 0.34f, zAt(tE));
                Ball(b, eye + new Vector3(0f, 0f, -0.05f), g.EyeR + 0.05f, 3, 3, 8);
                Ball(b, eye, g.EyeR, 2, 3, 8);
                for (int k = 0; k < 8; k++)
                {
                    float a0 = k / 8f * Mathf.PI * 2f;
                    float a1 = (k + 1) / 8f * Mathf.PI * 2f;
                    var dA = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f);
                    var dB = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f);
                    float r0 = g.EyeR + 0.04f, r1 = g.EyeR + 0.16f;
                    var inA = eye + dA * r0 + new Vector3(0f, 0f, -0.02f);
                    var inB = eye + dB * r0 + new Vector3(0f, 0f, -0.02f);
                    var outB = eye + dB * r1 + new Vector3(0f, 0f, -0.09f);
                    var outA = eye + dA * r1 + new Vector3(0f, 0f, -0.09f);
                    b.QuadUDS(inA, inB, outB, outA, k % 2 == 0 ? 0 : 1);
                }
            }

            // ---- ear tufts on the crown corners ----
            for (int side = -1; side <= 1; side += 2)
            {
                var basePt = new Vector3(side * W * 0.34f, hullY(0, 0.03f) + 0.02f, zAt(0.03f));
                var dir = new Vector3(side * 0.45f, 0.85f, -0.30f).normalized;
                Tube(b, new[] { basePt, basePt + dir * (g.TuftLen * 0.5f), basePt + dir * g.TuftLen },
                    new[] { 0.06f, 0.038f, 0.005f }, 5, 1, true);
                var basePt2 = basePt + new Vector3(-side * 0.10f, 0f, -0.10f);
                Tube(b, new[] { basePt2, basePt2 + dir * (g.TuftLen * 0.60f) },
                    new[] { 0.045f, 0.005f }, 5, 1, true);
            }

            // ---- canopy on the crown behind the face ----
            {
                float tCan = g.CanopyStart;
                float zC = zAt(tCan);
                float halfLen = g.CanopyLen * L * 0.50f;
                System.Func<float, float> deckAt = z =>
                {
                    float t = Mathf.Clamp01((zBF - z) / Lb);
                    float sc = CrSample(cts, csc, t);
                    return HalfPtT(0, t).y * H * sc + CrSample(cts, clf, t) * H;
                };
                Canopy(b, zC + halfLen, zC - halfLen, 0.26f, 0.16f, deckAt, 3, 1, 1);
            }

            // ---- faceted collar behind the facial disc ----
            {
                float t0 = 0.15f;
                float scC = CrSample(cts, csc, t0) * 1.05f;
                float liftC = CrSample(cts, clf, t0) * H;
                float z = zAt(t0);
                for (int k = 0; k < 8; k++)
                {
                    int li0 = (k * 3) % LoopPts;
                    int li1 = (k * 3 + 3) % LoopPts;
                    var p0 = LoopPtT(li0, t0);
                    var p1 = LoopPtT(li1, t0);
                    var a = new Vector3(p0.x * W * scC, p0.y * H * scC + liftC, z + 0.11f);
                    var b2 = new Vector3(p1.x * W * scC, p1.y * H * scC + liftC, z + 0.11f);
                    var c = new Vector3(p1.x * W * scC, p1.y * H * scC + liftC, z - 0.11f);
                    var d = new Vector3(p0.x * W * scC, p0.y * H * scC + liftC, z - 0.11f);
                    b.QuadUDS(a, b2, c, d, k % 2 == 0 ? 1 : 0);
                }
            }

            // ---- breast keel under the round chest ----
            {
                var kA = new Vector3(0f, hullY(12, 0.12f) + 0.02f, zAt(0.12f));
                var kB = new Vector3(0f, hullY(12, 0.28f) - 0.34f, zAt(0.28f));
                var kC = new Vector3(0f, hullY(12, 0.46f) + 0.02f, zAt(0.46f));
                var xoff = new Vector3(0.04f, 0f, 0f);
                b.TriUDS(kA + xoff, kB + xoff, kC + xoff, 0);
                b.TriUDS(kA - xoff, kB - xoff, kC - xoff, 0);
                b.QuadUDS(kA + xoff, kA - xoff, kB - xoff, kB + xoff, 1);
                b.QuadUDS(kB + xoff, kB - xoff, kC - xoff, kC + xoff, 1);
            }

            // ---- dorsal ridge plates ----
            for (int r2 = 0; r2 < 4; r2++)
            {
                float t0 = 0.40f + r2 * 0.10f;
                float z = zAt(t0);
                float y0 = hullY(0, t0);
                var a = new Vector3(0f, y0 + 0.01f, z + 0.10f);
                var apex = new Vector3(0f, y0 + 0.13f, z - 0.02f);
                var c = new Vector3(0f, y0 + 0.01f, z - 0.14f);
                b.TriUDS(a, apex, c, r2 % 2 == 0 ? 1 : 0);
            }

            // ---- dense rounded wing stacks: five primaries, three coverts ----
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                for (int f = 0; f < 5; f++)
                {
                    float t0 = 0.18f + f * 0.07f;
                    float span = g.FeatherSpan * (1f - f * 0.11f);
                    float sweep = g.FeatherSweep * (1f + f * 0.06f);
                    float liftW = CrSample(cts, clf, t0) * H;
                    var rootF2 = new Vector3(s * W * 0.42f, H * (0.30f - f * 0.045f) + liftW, zAt(t0));
                    var rootB2 = rootF2 + new Vector3(-s * 0.03f, -0.02f, -0.55f);
                    var tipF2 = rootF2 + new Vector3(s * span, span * g.FeatherRake - 0.05f, -sweep);
                    var tipB2 = rootB2 + new Vector3(s * span * 0.94f, span * g.FeatherRake * 0.9f - 0.05f, -sweep * 1.06f);
                    Feather(b, rootF2, rootB2, tipF2, tipB2, side < 0);
                }
                for (int f = 0; f < 3; f++)
                {
                    float t0 = 0.36f + f * 0.08f;
                    float span = g.FeatherSpan * (0.55f - f * 0.10f);
                    float sweep = g.FeatherSweep * (0.70f - f * 0.08f);
                    float liftW = CrSample(cts, clf, t0) * H;
                    var rootF2 = new Vector3(s * W * 0.52f, -H * 0.02f + liftW, zAt(t0));
                    var rootB2 = rootF2 + new Vector3(-s * 0.03f, -0.02f, -0.42f);
                    var tipF2 = rootF2 + new Vector3(s * span, -span * 0.08f, -sweep);
                    var tipB2 = rootB2 + new Vector3(s * span * 0.94f, -span * 0.08f, -sweep * 1.06f);
                    Feather(b, rootF2, rootB2, tipF2, tipB2, side < 0);
                }
            }

            // ---- short owl tail fan + center vane ----
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                for (int f = 0; f < 3; f++)
                {
                    float spread = 0.30f + f * 0.35f;
                    float liftT = CrSample(cts, clf, 0.86f) * H;
                    var rootF2 = new Vector3(s * W * 0.15f, liftT + 0.05f * H, zAt(0.86f));
                    var rootB2 = rootF2 + new Vector3(0f, -0.02f, -0.40f);
                    var tipF2 = rootF2 + new Vector3(s * g.TailSpan * spread, 0.08f, -g.TailSweep);
                    var tipB2 = rootB2 + new Vector3(s * g.TailSpan * spread * 0.94f, 0.06f, -g.TailSweep * 1.08f);
                    Feather(b, rootF2, rootB2, tipF2, tipB2, side < 0);
                }
            }
            {
                float y0 = hullY(0, 0.84f);
                var rootF2 = new Vector3(0f, y0 - 0.02f, zAt(0.84f));
                var rootB2 = new Vector3(0f, y0 - 0.02f, zAt(0.94f));
                var tipF2 = new Vector3(0f, y0 + 0.55f, zAt(0.84f) - g.TailSweep * 0.55f);
                var tipB2 = new Vector3(0f, y0 + 0.47f, zAt(0.94f) - g.TailSweep * 0.62f);
                var lead = new[] { rootF2, tipF2 };
                var trail = new[] { rootB2, tipB2 };
                var ts = new[] { 0f, 1f };
                LoftWing(b, lead, ts, trail, ts, 0.055f, 0.012f, 5, true, 1);
            }

            // ---- recon sensor pod banks under each flank ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    float t = 0.32f + i * 0.11f;
                    float lift = CrSample(cts, clf, t) * H;
                    var c = new Vector3(side * W * 0.40f, -H * 0.42f + lift, zAt(t));
                    Tube(b, new[] { c + Vector3.forward * 0.25f, c - Vector3.forward * 0.25f },
                        new[] { 0.085f, 0.085f }, 8, 1, true);
                    Tube(b, new[] { c + Vector3.forward * 0.25f, c + Vector3.forward * 0.33f },
                        new[] { 0.075f, 0.065f }, 8, 2, true);
                }
            }

            // ---- EW masts: dorsal web emitter, ventral disruptor ----
            {
                float t0 = 0.30f;
                var basePt = new Vector3(0f, hullY(0, t0), zAt(t0));
                Tube(b, new[] { basePt, basePt + Vector3.up * 0.30f }, new[] { 0.045f, 0.035f }, 6, 1, false);
                Ball(b, basePt + Vector3.up * 0.36f, 0.09f, 2, 3, 6);
            }
            {
                float t0 = 0.44f;
                var basePt = new Vector3(0f, hullY(12, t0), zAt(t0));
                Tube(b, new[] { basePt, basePt - Vector3.up * 0.26f }, new[] { 0.045f, 0.035f }, 6, 1, false);
                Ball(b, basePt - Vector3.up * 0.32f, 0.09f, 2, 3, 6);
            }

            // ---- four drone bays: two glowing hatches per flank ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int bay = 0; bay < 2; bay++)
                {
                    float t = 0.44f + bay * 0.14f;
                    float sc = CrSample(cts, csc, t);
                    float liftD = CrSample(cts, clf, t) * H;
                    var c = new Vector3(side * W * sc * 0.86f, 0.02f * H + liftD, zAt(t));
                    BevelBox(b, c, new Vector3(0.08f, 0.18f, 0.28f), 1);
                    BevelBox(b, c + new Vector3(side * 0.055f, 0f, 0f), new Vector3(0.035f, 0.13f, 0.22f), 2);
                }
            }

            // ---- twin nacelles plus a ventral engine ----
            for (int side = -1; side <= 1; side += 2)
            {
                float liftE = CrSample(cts, clf, 0.87f) * H;
                var ec = new Vector3(side * W * 0.40f, 0.06f * H + liftE, zAt(0.87f));
                BevelBox(b, ec, new Vector3(0.28f, 0.22f, 0.58f), 1);
                BevelBox(b, ec + new Vector3(0f, 0.06f, 0.32f), new Vector3(0.19f, 0.13f, 0.21f), 0);
                Nozzle(b, ec + new Vector3(0f, 0f, -0.65f), Vector3.back, 0.21f, 0.29f, 10, 1, 2);
                int n = g.EngineSegs;
                for (int i = 0; i < n; i++)
                    Tube(b, new[] { ec + new Vector3(0f, -0.16f, 0.32f - i * 0.28f), ec + new Vector3(0f, -0.16f, 0.19f - i * 0.28f) },
                        new[] { 0.13f, 0.13f }, 8, i % 2 == 0 ? 0 : 1, false);
            }
            {
                float liftE = CrSample(cts, clf, 0.90f) * H;
                var ec = new Vector3(0f, -H * 0.32f + liftE, zAt(0.90f));
                BevelBox(b, ec, new Vector3(0.20f, 0.17f, 0.46f), 1);
                Nozzle(b, ec + new Vector3(0f, 0f, -0.53f), Vector3.back, 0.155f, 0.22f, 10, 1, 2);
                for (int i = 0; i < 2; i++)
                    Tube(b, new[] { ec + new Vector3(0f, -0.12f, 0.22f - i * 0.28f), ec + new Vector3(0f, -0.12f, 0.09f - i * 0.28f) },
                        new[] { 0.11f, 0.11f }, 8, i % 2 == 0 ? 0 : 1, false);
            }

            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "talon3_" + hash };
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
