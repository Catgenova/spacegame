using System.Collections.Generic;
using UnityEngine;
using static SpaceGame.MeshKit;

namespace SpaceGame
{
    /// <summary>
    /// Asteroid rocks: three distinct models for every ore type, so a belt reads
    /// as a field of individual rocks instead of a row of identical spheres.
    ///
    /// Each ore gets its own shape language, taken from what the real mineral
    /// actually looks like in hand — the same discipline as the ore names
    /// themselves. Taenite is a metallic meteorite, smooth and thumbprinted with
    /// regmaglypts; Anorthite is blocky feldspar; Armalcolite is a dark dense
    /// prism; Rutile throws needle crystals; Beryl grows hexagonal columns. On
    /// top of that each type has three variants — a rounded mass, an elongated
    /// shard and a flattened slab — so you can tell rocks apart at a glance and
    /// still tell the ore from across the belt.
    ///
    /// Fifteen meshes total, built once and shared: a belt of twelve rocks costs
    /// twelve transforms, not twelve mesh builds.
    /// </summary>
    public static class AsteroidMesh
    {
        public const int Variants = 3;

        // Material slots.
        const int Rock = 0, Vein = 1, Dark = 2;

        static readonly Dictionary<string, Mesh> _meshes = new Dictionary<string, Mesh>();

        /// <summary>Which of the three models this particular rock uses. Derived
        /// from its id, so a given asteroid always looks the same — including
        /// after a belt respawn writes the same ids back.</summary>
        public static int VariantFor(string asteroidId)
            => asteroidId == null ? 0 : (int)(Rng.HashU(asteroidId) % Variants);

        public static GameObject Build(string oreId, int variant, Transform parent)
        {
            variant = ((variant % Variants) + Variants) % Variants;
            string key = oreId + ":" + variant;
            Mesh mesh;
            if (!_meshes.TryGetValue(key, out mesh))
            {
                mesh = BuildMesh(oreId, variant);
                _meshes[key] = mesh;
            }

            var ore = GameData.Ores.ContainsKey(oreId) ? GameData.Ores[oreId] : null;
            var tint = ore != null ? ore.Color : new Color(0.42f, 0.38f, 0.32f);
            // The body is rock first and ore second; the veins are the ore itself.
            var body = Color.Lerp(new Color(0.38f, 0.35f, 0.31f), tint, 0.42f);
            var vein = Color.Lerp(tint, Color.white, 0.18f);

            var go = new GameObject("Rock");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().mesh = mesh;
            go.AddComponent<MeshRenderer>().materials = new[]
            {
                Metal(body, 0.12f, 0.22f),
                Metal(vein, IsMetallic(oreId) ? 0.85f : 0.30f, 0.55f),
                Metal(new Color(0.10f, 0.09f, 0.09f), 0.20f, 0.30f),
            };
            return go;
        }

        /// <summary>Taenite is metal; the rest are silicates and oxides, so only
        /// the Fe-Ni rock should glint.</summary>
        static bool IsMetallic(string oreId) => oreId == "taenite";

        // ---------------- the rock body ----------------

        /// <summary>A lumpy closed rock. Radius is modulated by a handful of
        /// deterministic lobes (bulges) and dimples (pits), then squashed along
        /// the variant's axes. Faces are unshared, so it flat-shades into facets
        /// without any extra work.</summary>
        static void RockBody(Builder b, Rng.Roll rng, Vector3 axes, int lat, int lon,
            int lobeCount, float lobeAmp, int dimpleCount, float dimpleDepth, float facet)
        {
            var lobeDir = new Vector3[lobeCount];
            var lobeAmt = new float[lobeCount];
            for (int i = 0; i < lobeCount; i++)
            {
                lobeDir[i] = RandDir(rng);
                lobeAmt[i] = lobeAmp * (0.55f + (float)rng.NextDouble() * 0.75f);
            }
            var pitDir = new Vector3[dimpleCount];
            var pitAmt = new float[dimpleCount];
            var pitTight = new float[dimpleCount];
            for (int i = 0; i < dimpleCount; i++)
            {
                pitDir[i] = RandDir(rng);
                pitAmt[i] = dimpleDepth * (0.6f + (float)rng.NextDouble() * 0.8f);
                pitTight[i] = 5f + (float)rng.NextDouble() * 9f;
            }

            var ring = new Vector3[lat + 1][];
            for (int i = 0; i <= lat; i++)
            {
                float v = Mathf.PI * i / lat;
                ring[i] = new Vector3[lon];
                for (int j = 0; j < lon; j++)
                {
                    float u = Mathf.PI * 2f * j / lon;
                    var dir = new Vector3(Mathf.Sin(v) * Mathf.Cos(u), Mathf.Cos(v), Mathf.Sin(v) * Mathf.Sin(u));
                    float r = 1f;
                    for (int k = 0; k < lobeCount; k++)
                    {
                        float d = Vector3.Dot(dir, lobeDir[k]);
                        if (d > 0f) r += lobeAmt[k] * d * d;
                    }
                    for (int k = 0; k < dimpleCount; k++)
                    {
                        float d = Vector3.Dot(dir, pitDir[k]);
                        if (d > 0f) r -= pitAmt[k] * Mathf.Pow(d, pitTight[k]);
                    }
                    // Facetting: pull the radius toward coarse steps so the rock
                    // reads as cleaved planes rather than a smooth potato.
                    if (facet > 0f)
                    {
                        float stepped = Mathf.Round(r * 5f) / 5f;
                        r = Mathf.Lerp(r, stepped, facet);
                    }
                    ring[i][j] = new Vector3(dir.x * r * axes.x, dir.y * r * axes.y, dir.z * r * axes.z);
                }
            }
            for (int i = 0; i < lat; i++)
                for (int j = 0; j < lon; j++)
                {
                    int j2 = (j + 1) % lon;
                    b.QuadUDS(ring[i][j], ring[i][j2], ring[i + 1][j2], ring[i + 1][j], Rock);
                }
        }

        static Vector3 RandDir(Rng.Roll rng)
        {
            float z = (float)rng.NextDouble() * 2f - 1f;
            float a = (float)rng.NextDouble() * Mathf.PI * 2f;
            float r = Mathf.Sqrt(Mathf.Max(0f, 1f - z * z));
            return new Vector3(Mathf.Cos(a) * r, z, Mathf.Sin(a) * r);
        }

        /// <summary>Ore showing through the rock: shallow plates of vein material
        /// pressed onto the surface, so the ore colour is visible as deposits
        /// rather than a wash over the whole rock.</summary>
        static void Veins(Builder b, Rng.Roll rng, Vector3 axes, int count, float size, int mat)
        {
            for (int i = 0; i < count; i++)
            {
                var dir = RandDir(rng);
                var c = new Vector3(dir.x * axes.x, dir.y * axes.y, dir.z * axes.z) * 0.94f;
                Vector3 right, up;
                Basis(dir, out right, out up);
                float s = size * (0.6f + (float)rng.NextDouble() * 0.9f);
                // A shallow four-sided patch, tilted with the surface.
                b.QuadUDS(c + right * s + up * s * 0.7f, c - right * s * 0.8f + up * s,
                    c - right * s + up * -s * 0.75f, c + right * s * 0.9f - up * s * 0.6f, mat);
            }
        }

        /// <summary>A prismatic crystal standing out of the rock: a tapered column
        /// with a pointed cap. Rutile needles and beryl columns are the same
        /// primitive at different aspect ratios.</summary>
        static void Crystal(Builder b, Vector3 root, Vector3 dir, float len, float r, int sides, int mat)
        {
            var d = dir.normalized;
            Tube(b, new[] { root, root + d * len * 0.82f }, new[] { r, r * 0.86f }, sides, mat, false);
            Tube(b, new[] { root + d * len * 0.82f, root + d * len },
                new[] { r * 0.86f, 0.02f }, sides, mat, true);
        }

        // ---------------- per-ore models ----------------

        static Mesh BuildMesh(string oreId, int variant)
        {
            var b = new Builder();
            var rng = Rng.Stream("rock:" + oreId + ":" + variant);

            // The three variants are a rounded mass, an elongated shard and a
            // flattened slab — the silhouettes that read as different rocks.
            Vector3 axes = variant == 0 ? new Vector3(1.00f, 0.92f, 1.06f)
                : variant == 1 ? new Vector3(0.74f, 0.70f, 1.55f)
                : new Vector3(1.35f, 0.58f, 1.15f);

            switch (oreId)
            {
                case "taenite":
                    // Metallic meteorite: smooth, heavily thumbprinted, few lumps.
                    RockBody(b, rng, axes, 12, 16, 3, 0.10f, 9, 0.20f, 0f);
                    Veins(b, rng, axes, 7, 0.20f, Vein);
                    break;

                case "anorthite":
                    // Feldspar: blocky, cleaved, pale patches everywhere.
                    RockBody(b, rng, axes, 10, 12, 5, 0.20f, 2, 0.10f, 0.85f);
                    Veins(b, rng, axes, 9, 0.26f, Vein);
                    break;

                case "armalcolite":
                    // Dark dense titanate: angular, pitted, sparse bright ore.
                    RockBody(b, rng, axes, 11, 14, 6, 0.24f, 5, 0.16f, 0.55f);
                    Veins(b, rng, axes, 5, 0.18f, Vein);
                    for (int i = 0; i < 3; i++)
                    {
                        var dir = RandDir(rng);
                        var root = new Vector3(dir.x * axes.x, dir.y * axes.y, dir.z * axes.z) * 0.93f;
                        Crystal(b, root, dir, 0.55f, 0.15f, 4, Dark);
                    }
                    break;

                case "rutile":
                    // Needle habit: a knot of rock throwing prismatic spines.
                    RockBody(b, rng, axes, 11, 14, 4, 0.18f, 3, 0.12f, 0.35f);
                    Veins(b, rng, axes, 6, 0.20f, Vein);
                    {
                        int needles = 7 + variant * 2;
                        for (int i = 0; i < needles; i++)
                        {
                            var dir = RandDir(rng);
                            var root = new Vector3(dir.x * axes.x, dir.y * axes.y, dir.z * axes.z) * 0.90f;
                            Crystal(b, root, dir, 0.75f + (float)rng.NextDouble() * 0.70f,
                                0.055f, 4, Vein);
                        }
                    }
                    break;

                default: // beryl
                    // Hexagonal columns growing out of a pocketed host rock.
                    RockBody(b, rng, axes, 11, 14, 5, 0.22f, 4, 0.18f, 0.45f);
                    Veins(b, rng, axes, 4, 0.16f, Vein);
                    {
                        int columns = 4 + variant;
                        for (int i = 0; i < columns; i++)
                        {
                            var dir = RandDir(rng);
                            var root = new Vector3(dir.x * axes.x, dir.y * axes.y, dir.z * axes.z) * 0.90f;
                            Crystal(b, root, dir, 0.62f + (float)rng.NextDouble() * 0.40f,
                                0.15f + (float)rng.NextDouble() * 0.07f, 6, Vein);
                        }
                    }
                    break;
            }

            var mesh = new Mesh { name = "rock_" + oreId + "_" + variant };
            mesh.SetVertices(b.V);
            mesh.subMeshCount = 3;
            for (int m = 0; m < 3; m++) mesh.SetTriangles(b.Sub[m], m);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
