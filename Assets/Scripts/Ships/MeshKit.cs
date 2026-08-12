using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Shared procedural-mesh toolkit for generated ship lines (Hive, Fin).
    /// Submesh material indices are per-builder conventions; the Builder
    /// just routes triangles into four index lists.
    /// </summary>
    public static class MeshKit
    {
        public class Builder
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

            public void QuadU(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int mat)
            {
                int ia = Add(a), ib = Add(b), ic = Add(c), id = Add(d);
                Face(ia, id, ic, mat); Face(ia, ic, ib, mat);
            }
        }

        public static float CrSample(float[] ts, float[] vs, float t)
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

        public static float Smooth01(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        /// <summary>Uniform Catmull-Rom between p1 and p2 at u.</summary>
        public static float CrPoint(float p0, float p1, float p2, float p3, float u)
        {
            float u2 = u * u, u3 = u2 * u;
            return 0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * u3);
        }

        /// <summary>Sample a 13-row half-profile at a fractional row index —
        /// Catmull-Rom smoothed, passing exactly through the rows.</summary>
        public static Vector2 ProfCR(float[,] prof, float k)
        {
            int i1 = Mathf.Clamp((int)Mathf.Floor(k), 0, 12);
            int i0 = Mathf.Max(0, i1 - 1);
            int i2 = Mathf.Min(12, i1 + 1);
            int i3 = Mathf.Min(12, i1 + 2);
            float u = k - i1;
            return new Vector2(
                CrPoint(prof[i0, 0], prof[i1, 0], prof[i2, 0], prof[i3, 0], u),
                CrPoint(prof[i0, 1], prof[i1, 1], prof[i2, 1], prof[i3, 1], u));
        }

        /// <summary>
        /// High-resolution hull loft: 48 loop points (16 strips of 3 spans),
        /// sampled from a fractional half-profile (k in 0..12) so the old
        /// 24-point silhouettes come out Catmull-Rom smoothed. Paint fields
        /// keep their original 8-strip meaning — the callback receives the
        /// OLD (strip, span) cell plus the ring index for micro lookups.
        /// Returns stripVerts[16][rings][4] for cap fans and detail anchors.
        /// </summary>
        public static int[][][] HullLoft48(Builder b, int rings,
            System.Func<float, float, Vector2> halfPtF,
            System.Func<float, float> scAt, System.Func<float, float> liftAt,
            System.Func<float, float> zAt,
            System.Func<float, float> xwAt, System.Func<float, float> ywAt,
            float W, float H,
            System.Func<int, int, float, int, int> paint)
        {
            var sv = new int[16][][];
            for (int s = 0; s < 16; s++) sv[s] = new int[rings][];
            for (int j = 0; j < rings; j++)
            {
                float t = j / (float)(rings - 1);
                float sc = scAt(t);
                float lift = liftAt(t);
                float z = zAt(t);
                float xw = xwAt(t);
                float yw = ywAt(t);
                for (int s = 0; s < 16; s++)
                {
                    sv[s][j] = new int[4];
                    for (int p = 0; p < 4; p++)
                    {
                        int li = (s * 3 + p) % 48;
                        float k = li <= 24 ? li * 0.5f : (48 - li) * 0.5f;
                        var pt = halfPtF(k, t);
                        float x = li <= 24 ? pt.x : -pt.x;
                        sv[s][j][p] = b.Add(new Vector3(x * W * sc * xw, pt.y * H * sc * yw + lift, z));
                    }
                }
            }
            for (int i = 0; i < rings - 1; i++)
            {
                float tm = (i + 0.5f) / (rings - 1);
                for (int s = 0; s < 16; s++)
                    for (int p = 0; p < 3; p++)
                    {
                        int go = ((s * 3 + p) % 48) / 2;
                        int mat = paint(go / 3, go % 3, tm, i);
                        b.FaceQ(sv[s][i][p], sv[s][i + 1][p],
                            sv[s][i + 1][p + 1], sv[s][i][p + 1], mat);
                    }
            }
            return sv;
        }

        /// <summary>Cap fan closing one end of a HullLoft48 ring.</summary>
        public static void CapFan(Builder b, Vector3 center, int[][][] sv, int ring, bool front, int mat)
        {
            for (int s = 0; s < 16; s++)
                for (int p = 0; p < 3; p++)
                {
                    if (front) b.TriU(center, b.V[sv[s][ring][p + 1]], b.V[sv[s][ring][p]], mat);
                    else b.TriU(center, b.V[sv[s][ring][p]], b.V[sv[s][ring][p + 1]], mat);
                }
        }

        public static readonly float[] WingCf = { 0.00f, 0.10f, 0.34f, 0.62f, 0.86f, 1.00f };

        public static readonly float[] WingTf = { 0.22f, 0.85f, 1.00f, 0.80f, 0.45f, 0.16f };

        public static Vector3 CrChain(Vector3[] pts, float[] ts, float u)
        {
            var xs = new float[pts.Length];
            var ys = new float[pts.Length];
            var zs = new float[pts.Length];
            for (int i = 0; i < pts.Length; i++) { xs[i] = pts[i].x; ys[i] = pts[i].y; zs[i] = pts[i].z; }
            return new Vector3(CrSample(ts, xs, u), CrSample(ts, ys, u), CrSample(ts, zs, u));
        }

        public static Vector3 WingNormal(Vector3[] lead, float[] leadT, Vector3[] trail, float[] trailT, float u)
        {
            var le = CrChain(lead, leadT, u);
            var te = CrChain(trail, trailT, u);
            const float du = 0.05f;
            var mA = (CrChain(lead, leadT, Mathf.Clamp01(u - du)) + CrChain(trail, trailT, Mathf.Clamp01(u - du))) * 0.5f;
            var mB = (CrChain(lead, leadT, Mathf.Clamp01(u + du)) + CrChain(trail, trailT, Mathf.Clamp01(u + du))) * 0.5f;
            var n = Vector3.Cross(te - le, mB - mA).normalized;
            return n.y < 0f ? -n : n;
        }

        /// <summary>Mirror-consistent camber sign: bulge along the face of
        /// the wing whose mid-span normal points up (falls back to +x for
        /// near-vertical surfaces). Mirrored chains flip the normal, so this
        /// keeps left/right wings cambering symmetrically.</summary>
        public static float CamberSign(Vector3[] lead, float[] leadT, Vector3[] trail, float[] trailT)
        {
            var n = WingNormal(lead, leadT, trail, trailT, 0.5f);
            return n.y > 0.02f ? 1f : n.y < -0.02f ? -1f : (n.x >= 0f ? 1f : -1f);
        }

        public static void LoftWing(Builder b, Vector3[] lead, float[] leadT, Vector3[] trail, float[] trailT,
            float rootTh, float tipTh, int stations, bool flip, int skinMat = 0, float camberF = 0.06f)
        {
            int m = WingCf.Length, loop = m * 2;
            var idx = new int[stations][];
            float cSign = CamberSign(lead, leadT, trail, trailT);
            for (int i = 0; i < stations; i++)
            {
                float u = i / (float)(stations - 1);
                var le = CrChain(lead, leadT, u);
                var te = CrChain(trail, trailT, u);
                var n = WingNormal(lead, leadT, trail, trailT, u);
                float tmax = Mathf.Lerp(rootTh, tipTh, u);
                float camb = (te - le).magnitude * camberF + tmax * 0.35f;
                idx[i] = new int[loop];
                for (int k = 0; k < loop; k++)
                {
                    int j = k < m ? k : loop - 1 - k;
                    float c = WingCf[j];
                    float lift = cSign * camb * 4f * c * (1f - c);
                    float th = WingTf[j] * tmax * (k < m ? 1f : -1f);
                    idx[i][k] = b.Add(le + (te - le) * c + n * (lift + th));
                }
            }
            for (int i = 0; i < stations - 1; i++)
                for (int k = 0; k < loop; k++)
                {
                    int k2 = (k + 1) % loop;
                    // black bands hug both edges; the spar stays gold
                    int mat = (k <= 0 || k >= loop - 2 || (k >= m - 2 && k <= m)) ? 1 : skinMat;
                    if (!flip) b.FaceQ(idx[i][k], idx[i][k2], idx[i + 1][k2], idx[i + 1][k], mat);
                    else b.FaceQ(idx[i + 1][k], idx[i + 1][k2], idx[i][k2], idx[i][k], mat);
                }
            for (int k = 1; k < loop - 1; k++)
            {
                if (!flip)
                {
                    b.Face(idx[0][0], idx[0][k], idx[0][k + 1], 1);
                    b.Face(idx[stations - 1][0], idx[stations - 1][k + 1], idx[stations - 1][k], 1);
                }
                else
                {
                    b.Face(idx[0][0], idx[0][k + 1], idx[0][k], 1);
                    b.Face(idx[stations - 1][0], idx[stations - 1][k], idx[stations - 1][k + 1], 1);
                }
            }
        }

        public static Vector3 WingSurfPt(Vector3[] lead, float[] leadT, Vector3[] trail, float[] trailT,
            float rootTh, float tipTh, float u, float c, float raise, float camberF = 0.06f)
        {
            var le = CrChain(lead, leadT, u);
            var te = CrChain(trail, trailT, u);
            var n = WingNormal(lead, leadT, trail, trailT, u);
            float cSign = CamberSign(lead, leadT, trail, trailT);
            float tmax = Mathf.Lerp(rootTh, tipTh, u);
            float camb = (te - le).magnitude * camberF + tmax * 0.35f;
            return le + (te - le) * c
                + n * (cSign * camb * 4f * c * (1f - c) + CrSample(WingCf, WingTf, c) * tmax + raise);
        }

        public static void WingPlate(Builder b, Vector3[] lead, float[] leadT, Vector3[] trail, float[] trailT,
            float rootTh, float tipTh, float u0, float u1, int mat, float camberF = 0.06f)
        {
            const float c0 = 0.26f, c1 = 0.68f;
            var a = WingSurfPt(lead, leadT, trail, trailT, rootTh, tipTh, u0, c0, 0.024f, camberF);
            var b2 = WingSurfPt(lead, leadT, trail, trailT, rootTh, tipTh, u0, c1, 0.024f, camberF);
            var c = WingSurfPt(lead, leadT, trail, trailT, rootTh, tipTh, u1, c1, 0.024f, camberF);
            var d = WingSurfPt(lead, leadT, trail, trailT, rootTh, tipTh, u1, c0, 0.024f, camberF);
            b.QuadUDS(a, b2, c, d, mat);
        }


        // Cockpit canopy: a raked teardrop glass loft that hugs the deck
        // line (deckY samples the hull under each ring), with framed side
        // rails, a windscreen arch up front, a rear arch, optional ribs,
        // and a fairing skirt so it reads as part of the hull rather than
        // a blister set on top. Peak sits ~38% back for the raked look.
        public static void Canopy(Builder b, float zFront, float zRear, float width, float height,
            System.Func<float, float> deckY, int glassMat, int frameMat, int ribs)
        {
            const int n = 12, m = 9;
            var pts = new Vector3[n][];
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1);
                float z = Mathf.Lerp(zFront, zRear, u);
                float prof = Mathf.Max(0.045f, Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.72f)));
                float h = height * Mathf.Pow(prof, 0.90f);
                float w = width * Mathf.Pow(prof, 0.55f);
                float y0 = deckY(z) - 0.015f;
                pts[i] = new Vector3[m];
                for (int k = 0; k < m; k++)
                {
                    float a = Mathf.PI * k / (m - 1);
                    pts[i][k] = new Vector3(Mathf.Cos(a) * w, y0 + Mathf.Sin(a) * h, z);
                }
            }
            for (int i = 0; i < n - 1; i++)
            {
                bool rib = false;
                for (int r = 1; r <= ribs; r++)
                    if (i == 1 + r * (n - 3) / (ribs + 1)) rib = true;
                for (int k = 0; k < m - 1; k++)
                {
                    bool rail = k == 0 || k == m - 2;
                    bool arch = i == 1 || i == n - 3;
                    int mat = (rail || arch || rib) ? frameMat : glassMat;
                    b.QuadUDS(pts[i][k], pts[i][k + 1], pts[i + 1][k + 1], pts[i + 1][k], mat);
                }
            }
            // fairing skirt flowing out from the base rails into the deck
            for (int i = 0; i < n - 1; i++)
            {
                var a0 = pts[i][0];
                var a1 = pts[i + 1][0];
                var b0 = pts[i][m - 1];
                var b1 = pts[i + 1][m - 1];
                var outA = new Vector3(0.06f, -0.05f, 0f);
                var outB = new Vector3(-0.06f, -0.05f, 0f);
                b.QuadUDS(a0, a1, a1 + outA, a0 + outA, frameMat);
                b.QuadUDS(b0, b1, b1 + outB, b0 + outB, frameMat);
            }
        }

        public static void Basis(Vector3 dir, out Vector3 right, out Vector3 up)
        {
            var refUp = Mathf.Abs(dir.y) > 0.93f ? Vector3.forward : Vector3.up;
            right = Vector3.Cross(refUp, dir).normalized;
            up = Vector3.Cross(dir, right).normalized;
        }

        /// <summary>Recessed engine nozzle. `exit` is the center of the
        /// exhaust plane and `dir` points out of the exhaust, away from the
        /// hull. Builds a flared outer bell, an annular exit rim, an inner
        /// cavity cone recessed toward the hull, a glow disc at the cavity
        /// floor, and four radial heat fins. Bell/rim/cavity are single-sided
        /// (winding verified against the loft convention).</summary>
        public static void Nozzle(Builder b, Vector3 exit, Vector3 dir, float r,
            float len, int sides, int matBody, int matGlow)
        {
            dir = dir.normalized;
            Basis(dir, out var right, out var up);
            System.Func<int, float, float, Vector3> rp = (k, rad, back) =>
            {
                float a = k / (float)sides * Mathf.PI * 2f;
                return exit - dir * back + right * (Mathf.Cos(a) * rad) + up * (Mathf.Sin(a) * rad);
            };
            // outer bell: hull collar -> waist -> flared exit rim
            float[] rads = { 0.80f * r, 0.86f * r, 1.00f * r };
            float[] deps = { len, 0.42f * len, 0f };
            for (int i = 0; i < 2; i++)
                for (int k = 0; k < sides; k++)
                {
                    int k2 = (k + 1) % sides;
                    b.QuadU(rp(k, rads[i + 1], deps[i + 1]), rp(k2, rads[i + 1], deps[i + 1]),
                        rp(k2, rads[i], deps[i]), rp(k, rads[i], deps[i]), matBody);
                }
            float rIn = 0.74f * r;
            float rThroat = 0.40f * r, dThroat = 0.55f * len;
            for (int k = 0; k < sides; k++)
            {
                int k2 = (k + 1) % sides;
                b.QuadU(rp(k, rIn, 0f), rp(k2, rIn, 0f), rp(k2, r, 0f), rp(k, r, 0f), matBody);
                b.QuadU(rp(k, rThroat, dThroat), rp(k2, rThroat, dThroat),
                    rp(k2, rIn, 0f), rp(k, rIn, 0f), matBody);
            }
            var gc = exit - dir * dThroat;
            for (int k = 0; k < sides; k++)
                b.TriU(gc, rp(k, rThroat, dThroat), rp((k + 1) % sides, rThroat, dThroat), matGlow);
            for (int f = 0; f < 4; f++)
            {
                float a = (f + 0.5f) / 4f * Mathf.PI * 2f;
                var rad = right * Mathf.Cos(a) + up * Mathf.Sin(a);
                b.TriUDS(exit - dir * (0.92f * len) + rad * (0.82f * r),
                    exit - dir * (0.02f * len) + rad * (1.00f * r),
                    exit - dir * (0.45f * len) + rad * (1.16f * r), matBody);
            }
        }

        public static void Tube(Builder b, Vector3[] path, float[] radii, int sides, int mat, bool capEnd)
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

        public static void Ball(Builder b, Vector3 c, float r, int mat, int lat, int lon)
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

        public static void Box(Builder b, Vector3 c, Vector3 half, int mat)
        {
            var p000 = c + new Vector3(-half.x, -half.y, -half.z);
            var p001 = c + new Vector3(-half.x, -half.y, half.z);
            var p010 = c + new Vector3(-half.x, half.y, -half.z);
            var p011 = c + new Vector3(-half.x, half.y, half.z);
            var p100 = c + new Vector3(half.x, -half.y, -half.z);
            var p101 = c + new Vector3(half.x, -half.y, half.z);
            var p110 = c + new Vector3(half.x, half.y, -half.z);
            var p111 = c + new Vector3(half.x, half.y, half.z);
            b.QuadUDS(p011, p111, p110, p010, mat);
            b.QuadUDS(p001, p101, p100, p000, mat);
            b.QuadUDS(p011, p001, p101, p111, mat);
            b.QuadUDS(p010, p000, p100, p110, mat);
            b.QuadUDS(p111, p101, p100, p110, mat);
            b.QuadUDS(p011, p001, p000, p010, mat);
        }

        /// <summary>Fairing skirt: one ring of quads flaring from a snug
        /// collar (radius r0 at the attachment, height h along axis) down
        /// to a wide base (radius r1 at the hull surface). Kills the
        /// floating-primitive look under guns, masts, dishes, and legs.
        /// Single-sided; winding resolved per-quad.</summary>
        public static void Fairing(Builder b, Vector3 baseC, Vector3 axis, float r0, float r1,
            float h, int sides, int mat)
        {
            axis = axis.normalized;
            Basis(axis, out var right, out var up);
            System.Func<int, float, float, Vector3> rp = (k, rad, ht) =>
            {
                float a = k / (float)sides * Mathf.PI * 2f;
                return baseC + axis * ht + right * (Mathf.Cos(a) * rad) + up * (Mathf.Sin(a) * rad);
            };
            for (int k = 0; k < sides; k++)
            {
                int k2 = (k + 1) % sides;
                var b0 = rp(k, r1, 0f); var b1 = rp(k2, r1, 0f);
                var t0 = rp(k, r0, h); var t1 = rp(k2, r0, h);
                var n = Vector3.Cross(b1 - b0, t0 - b0);
                var outward = (b0 + b1 + t0 + t1) * 0.25f - (baseC + axis * (h * 0.5f));
                if (Vector3.Dot(n, outward) > 0f) b.QuadU(t0, t1, b1, b0, mat);
                else b.QuadU(t1, t0, b0, b1, mat);
            }
        }

        /// <summary>BevelBox with the standard 0.03 chamfer.</summary>
        public static void BevelBox(Builder b, Vector3 c, Vector3 half, int mat)
            => BevelBox(b, c, half, 0.03f, mat);

        /// <summary>Chamfered box: six inset faces, twelve 45-degree edge
        /// strips, eight corner triangles. Single-sided, winding verified
        /// against the hull-loft convention. Reads as machined plate where
        /// Box reads as a primitive; ~44 tris vs Box's 24.</summary>
        public static void BevelBox(Builder b, Vector3 c, Vector3 half, float bevel, int mat)
        {
            float v = Mathf.Min(bevel, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * 0.45f);
            // Three points per corner, one pushed to each face plane.
            var P = new Vector3[8][];
            for (int ci = 0; ci < 8; ci++)
            {
                float sx = (ci & 4) != 0 ? 1f : -1f;
                float sy = (ci & 2) != 0 ? 1f : -1f;
                float sz = (ci & 1) != 0 ? 1f : -1f;
                P[ci] = new[]
                {
                    c + new Vector3(sx * half.x, sy * (half.y - v), sz * (half.z - v)),
                    c + new Vector3(sx * (half.x - v), sy * half.y, sz * (half.z - v)),
                    c + new Vector3(sx * (half.x - v), sy * (half.y - v), sz * half.z),
                };
            }
            // Faces: corner order wound so normals point out (verified).
            int[][] faces =
            {
                new[] { 7, 5, 4, 6, 0 },  // +X
                new[] { 3, 2, 0, 1, 0 },  // -X
                new[] { 7, 6, 2, 3, 1 },  // +Y
                new[] { 5, 1, 0, 4, 1 },  // -Y
                new[] { 7, 3, 1, 5, 2 },  // +Z
                new[] { 6, 4, 0, 2, 2 },  // -Z
            };
            foreach (var f in faces)
                b.QuadU(P[f[3]][f[4]], P[f[2]][f[4]], P[f[1]][f[4]], P[f[0]][f[4]], mat);
            // Edge strips between adjacent faces.
            int[][] edges =
            {
                // corner A, corner B, axis of first face, axis of second face
                new[] { 7, 5, 0, 2 }, new[] { 6, 4, 0, 2 }, new[] { 3, 1, 0, 2 }, new[] { 2, 0, 0, 2 },
                new[] { 7, 6, 0, 1 }, new[] { 5, 4, 0, 1 }, new[] { 3, 2, 0, 1 }, new[] { 1, 0, 0, 1 },
                new[] { 7, 3, 1, 2 }, new[] { 6, 2, 1, 2 }, new[] { 5, 1, 1, 2 }, new[] { 4, 0, 1, 2 },
            };
            foreach (var e in edges)
            {
                var a0 = P[e[0]][e[2]]; var a1 = P[e[1]][e[2]];
                var b0 = P[e[0]][e[3]]; var b1 = P[e[1]][e[3]];
                // wind so the strip faces outward: test the cross product
                var n = Vector3.Cross(a1 - a0, b0 - a0);
                var outward = (a0 + a1 + b0 + b1) * 0.25f - c;
                if (Vector3.Dot(n, outward) > 0f) b.QuadU(b0, b1, a1, a0, mat);
                else b.QuadU(b1, b0, a0, a1, mat);
            }
            // Corner triangles.
            for (int ci = 0; ci < 8; ci++)
            {
                var n = Vector3.Cross(P[ci][1] - P[ci][0], P[ci][2] - P[ci][0]);
                var outward = (P[ci][0] + P[ci][1] + P[ci][2]) / 3f - c;
                if (Vector3.Dot(n, outward) > 0f) b.TriU(P[ci][0], P[ci][1], P[ci][2], mat);
                else b.TriU(P[ci][0], P[ci][2], P[ci][1], mat);
            }
        }

        public static Material Metal(Color c, float metallic, float smooth)
        {
            var m = SystemView.Mat(c);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glossiness", smooth);
            m.SetFloat("_Smoothness", smooth);
            return m;
        }
    }
}
