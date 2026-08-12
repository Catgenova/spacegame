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

        public static void LoftWing(Builder b, Vector3[] lead, float[] leadT, Vector3[] trail, float[] trailT,
            float rootTh, float tipTh, int stations, bool flip, int skinMat = 0)
        {
            int m = WingCf.Length, loop = m * 2;
            var idx = new int[stations][];
            for (int i = 0; i < stations; i++)
            {
                float u = i / (float)(stations - 1);
                var le = CrChain(lead, leadT, u);
                var te = CrChain(trail, trailT, u);
                var n = WingNormal(lead, leadT, trail, trailT, u);
                float tmax = Mathf.Lerp(rootTh, tipTh, u);
                float camb = tmax * 0.35f;
                idx[i] = new int[loop];
                for (int k = 0; k < loop; k++)
                {
                    int j = k < m ? k : loop - 1 - k;
                    float c = WingCf[j];
                    float lift = camb * 4f * c * (1f - c);
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
            float rootTh, float tipTh, float u, float c, float raise)
        {
            var le = CrChain(lead, leadT, u);
            var te = CrChain(trail, trailT, u);
            var n = WingNormal(lead, leadT, trail, trailT, u);
            float tmax = Mathf.Lerp(rootTh, tipTh, u);
            float camb = tmax * 0.35f;
            return le + (te - le) * c + n * (camb * 4f * c * (1f - c) + CrSample(WingCf, WingTf, c) * tmax + raise);
        }

        public static void WingPlate(Builder b, Vector3[] lead, float[] leadT, Vector3[] trail, float[] trailT,
            float rootTh, float tipTh, float u0, float u1, int mat)
        {
            const float c0 = 0.26f, c1 = 0.68f;
            var a = WingSurfPt(lead, leadT, trail, trailT, rootTh, tipTh, u0, c0, 0.024f);
            var b2 = WingSurfPt(lead, leadT, trail, trailT, rootTh, tipTh, u0, c1, 0.024f);
            var c = WingSurfPt(lead, leadT, trail, trailT, rootTh, tipTh, u1, c1, 0.024f);
            var d = WingSurfPt(lead, leadT, trail, trailT, rootTh, tipTh, u1, c0, 0.024f);
            b.QuadUDS(a, b2, c, d, mat);
        }

        public static void Basis(Vector3 dir, out Vector3 right, out Vector3 up)
        {
            var refUp = Mathf.Abs(dir.y) > 0.93f ? Vector3.forward : Vector3.up;
            right = Vector3.Cross(refUp, dir).normalized;
            up = Vector3.Cross(dir, right).normalized;
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
