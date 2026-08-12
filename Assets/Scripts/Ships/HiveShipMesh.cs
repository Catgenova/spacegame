using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Generates the Hive body mesh from a 10-digit hash: an elongated,
    /// faceted wasp hull in reflective gold with black canopy and panel work,
    /// long swept antenna spikes, angular wing plates, folded insect legs,
    /// and a twin engine block. Flat-shaded (hard facets) for the angular
    /// threatening look; two submeshes carry the gold/black metals.
    /// Same hash in -> same body out, always.
    /// </summary>
    public static class HiveShipMesh
    {
        // Angular cross-section, 8 points clockwise from the top (front view).
        static readonly Vector2[] Profile =
        {
            new Vector2(0f, 1f), new Vector2(0.55f, 0.72f), new Vector2(1f, 0.18f),
            new Vector2(0.8f, -0.35f), new Vector2(0f, -0.75f), new Vector2(-0.8f, -0.35f),
            new Vector2(-1f, 0.18f), new Vector2(-0.55f, 0.72f),
        };

        // Hull stations nose -> tail: z fraction of length, girth scale, top lift.
        static readonly float[] StZ = { 0.46f, 0.30f, 0.12f, -0.06f, -0.24f, -0.38f, -0.50f };
        static readonly float[] StS = { 0.16f, 0.55f, 0.90f, 1.00f, 0.85f, 0.60f, 0.30f };
        static readonly float[] StLift = { 0.00f, 0.05f, 0.12f, 0.09f, 0.03f, 0.00f, 0.00f };

        class Builder
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<int> Gold = new List<int>();
            public readonly List<int> Black = new List<int>();

            public void Tri(Vector3 a, Vector3 b, Vector3 c, bool black)
            {
                int i = V.Count;
                V.Add(a); V.Add(b); V.Add(c);
                var l = black ? Black : Gold;
                l.Add(i); l.Add(i + 1); l.Add(i + 2);
            }

            // a=front-upper, b=front-lower, c=back-lower, d=back-upper (outward CW).
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool black)
            {
                Tri(a, d, c, black);
                Tri(a, c, b, black);
            }

            // Double-sided variants for thin/oriented pieces built blind.
            public void TriDS(Vector3 a, Vector3 b, Vector3 c, bool black)
            {
                Tri(a, b, c, black);
                Tri(a, c, b, black);
            }

            public void QuadDS(Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool black)
            {
                TriDS(a, d, c, black);
                TriDS(a, c, b, black);
            }
        }

        public static GameObject Build(string hash, Transform shipRoot)
        {
            var rng = Rng.Stream("hivebody:" + hash);
            System.Func<float, float, float> R = (lo, hi) => lo + (float)rng.NextDouble() * (hi - lo);

            float L = R(4.6f, 5.8f);          // hull length
            float W = R(0.75f, 1.1f);         // half width
            float H = R(0.55f, 0.8f);         // half height
            float noseLen = R(0.5f, 1.0f);
            float antLen = R(3.2f, 5.0f);
            float antUp = R(0.35f, 0.65f);
            float wingSpan = R(1.6f, 2.8f);
            float wingSweep = R(0.8f, 1.8f);
            int legsPerSide = rng.NextDouble() < 0.5 ? 1 : 2;
            float panelDensity = R(0.08f, 0.2f);
            float goldHue = R(0.09f, 0.135f);
            var gold = Color.HSVToRGB(goldHue, R(0.75f, 0.95f), R(0.72f, 0.92f));
            var b = new Builder();

            // ---- hull loft ----
            int n = StZ.Length;
            var rings = new Vector3[n][];
            for (int i = 0; i < n; i++)
            {
                rings[i] = new Vector3[Profile.Length];
                for (int j = 0; j < Profile.Length; j++)
                {
                    float jit = 1f + ((float)rng.NextDouble() - 0.5f) * 0.09f;
                    rings[i][j] = new Vector3(
                        Profile[j].x * W * StS[i] * jit,
                        Profile[j].y * H * StS[i] + StLift[i] * H,
                        StZ[i] * L);
                }
            }

            // Panel scheme: nose band, canopy stripe on top, sparse hash panels.
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j < Profile.Length; j++)
                {
                    int j2 = (j + 1) % Profile.Length;
                    bool black =
                        i == 0
                        || ((j == 7 || j == 0) && i >= 1 && i <= 2)
                        || rng.NextDouble() < panelDensity;
                    b.Quad(rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j], black);
                }
            }

            // Nose spike (black tip like the reference) and tail cap.
            var tip = new Vector3(0f, -0.05f * H, StZ[0] * L + noseLen);
            for (int j = 0; j < Profile.Length; j++)
                b.Tri(tip, rings[0][(j + 1) % Profile.Length], rings[0][j], true);
            var tailC = new Vector3(0f, -0.05f * H, StZ[n - 1] * L - 0.12f);
            for (int j = 0; j < Profile.Length; j++)
                b.Tri(tailC, rings[n - 1][j], rings[n - 1][(j + 1) % Profile.Length], true);

            // ---- antennae: long spikes swept up and back ----
            for (int side = -1; side <= 1; side += 2)
            {
                var mount = new Vector3(side * W * 0.3f, H * 0.45f, -0.12f * L);
                var dir = new Vector3(side * 0.14f, antUp, -0.85f).normalized;
                Spike(b, mount, dir, antLen, 0.07f, false);
            }

            // ---- wing plates: angular blades swept back ----
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                var rootF = new Vector3(s * W * 0.8f, 0.12f * H, 0.02f * L);
                var rootB = new Vector3(s * W * 0.72f, 0.06f * H, -0.2f * L);
                var tipB = new Vector3(s * (W * 0.8f + wingSpan), -0.25f * H, -0.2f * L - wingSweep);
                var tipF = new Vector3(s * (W * 0.8f + wingSpan * 0.75f), -0.15f * H, -0.02f * L - wingSweep * 0.4f);
                b.QuadDS(rootF, rootB, tipB, tipF, false);
                // Black leading edge accent.
                b.QuadDS(rootF, tipF, tipF + new Vector3(0f, 0.02f * H, 0.12f),
                    rootF + new Vector3(0f, 0.02f * H, 0.12f), true);
            }

            // ---- folded legs: gold upper segment, black lower spike ----
            for (int side = -1; side <= 1; side += 2)
            {
                for (int leg = 0; leg < legsPerSide; leg++)
                {
                    float zOff = -0.02f * L - leg * 0.16f * L;
                    var mount = new Vector3(side * W * 0.55f, -0.3f * H, zOff);
                    var d1 = new Vector3(side * 0.55f, 0.35f, -0.5f).normalized;
                    float len1 = R(1.1f, 1.5f);
                    var elbow = mount + d1 * len1;
                    Slab(b, mount, d1, len1, 0.22f, 0.14f, false);
                    var d2 = new Vector3(side * 0.12f, -0.82f, -0.45f).normalized;
                    Spike(b, elbow, d2, R(1.5f, 2.1f), 0.09f, true);
                }
            }

            // ---- twin engine block ----
            for (int side = -1; side <= 1; side += 2)
            {
                var c = new Vector3(side * W * 0.25f, -0.05f * H, StZ[n - 1] * L - 0.1f);
                Prism(b, c, 0.3f, 0.85f, true);
            }

            // ---- assemble ----
            var go = new GameObject("Hull");
            go.transform.SetParent(shipRoot, false);
            var mesh = new Mesh { name = "hive_" + hash };
            mesh.SetVertices(b.V);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(b.Gold, 0);
            mesh.SetTriangles(b.Black, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.materials = new[]
            {
                Metal(gold, 0.9f, 0.8f),
                Metal(new Color(0.08f, 0.08f, 0.1f), 0.85f, 0.6f),
            };

            // Engine glow.
            for (int side = -1; side <= 1; side += 2)
            {
                var glow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(glow.GetComponent<Collider>());
                glow.transform.SetParent(go.transform, false);
                glow.transform.localPosition = new Vector3(side * W * 0.25f, -0.05f * H, StZ[n - 1] * L - 0.55f);
                glow.transform.localScale = new Vector3(0.34f, 0.34f, 0.1f);
                glow.GetComponent<Renderer>().material =
                    SystemView.Mat(new Color(1f, 0.62f, 0.25f), true);
            }
            return go;
        }

        static Material Metal(Color c, float metallic, float smooth)
        {
            var m = SystemView.Mat(c);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glossiness", smooth);  // Built-in Standard
            m.SetFloat("_Smoothness", smooth);  // URP Lit
            return m;
        }

        static void Basis(Vector3 dir, out Vector3 right, out Vector3 up)
        {
            var refUp = Mathf.Abs(dir.y) > 0.93f ? Vector3.forward : Vector3.up;
            right = Vector3.Cross(refUp, dir).normalized;
            up = Vector3.Cross(dir, right).normalized;
        }

        /// <summary>Four-sided tapered spike from pos along dir (double-sided).</summary>
        static void Spike(Builder b, Vector3 pos, Vector3 dir, float len, float baseR, bool black)
        {
            Basis(dir, out var right, out var up);
            var tip = pos + dir * len;
            var p0 = pos + right * baseR;
            var p1 = pos + up * baseR;
            var p2 = pos - right * baseR;
            var p3 = pos - up * baseR;
            b.TriDS(tip, p0, p1, black);
            b.TriDS(tip, p1, p2, black);
            b.TriDS(tip, p2, p3, black);
            b.TriDS(tip, p3, p0, black);
            b.QuadDS(p0, p1, p2, p3, black);
        }

        /// <summary>Tapered rectangular limb segment (double-sided).</summary>
        static void Slab(Builder b, Vector3 start, Vector3 dir, float len, float w0, float w1, bool black)
        {
            Basis(dir, out var right, out var up);
            var end = start + dir * len;
            var a0 = start + right * w0 + up * w0 * 0.6f;
            var a1 = start - right * w0 + up * w0 * 0.6f;
            var a2 = start - right * w0 - up * w0 * 0.6f;
            var a3 = start + right * w0 - up * w0 * 0.6f;
            var c0 = end + right * w1 + up * w1 * 0.6f;
            var c1 = end - right * w1 + up * w1 * 0.6f;
            var c2 = end - right * w1 - up * w1 * 0.6f;
            var c3 = end + right * w1 - up * w1 * 0.6f;
            b.QuadDS(a0, a1, c1, c0, black);
            b.QuadDS(a1, a2, c2, c1, black);
            b.QuadDS(a2, a3, c3, c2, black);
            b.QuadDS(a3, a0, c0, c3, black);
            b.QuadDS(c0, c1, c2, c3, black);
        }

        /// <summary>Octagonal engine prism pointing backwards (double-sided).</summary>
        static void Prism(Builder b, Vector3 center, float radius, float len, bool black)
        {
            const int sides = 8;
            var front = new Vector3[sides];
            var back = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                var off = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
                front[i] = center + off + Vector3.forward * (len * 0.5f);
                back[i] = center + off - Vector3.forward * (len * 0.5f);
            }
            for (int i = 0; i < sides; i++)
            {
                int i2 = (i + 1) % sides;
                b.QuadDS(front[i], front[i2], back[i2], back[i], black);
            }
            for (int i = 0; i < sides; i++)
            {
                int i2 = (i + 1) % sides;
                b.TriDS(center - Vector3.forward * (len * 0.5f), back[i], back[i2], black);
            }
        }
    }
}
