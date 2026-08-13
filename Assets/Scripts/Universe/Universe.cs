using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    public class Celestial
    {
        public string Id, Name;
        public ObjKind Kind;
        public Vector3 Pos;
        public string GateTo; // gates only: destination system id
        public float Hue;     // planets only

        /// <summary>Stations only: the generated ship lines this station holds
        /// production licences for. Null or empty means no shipyard here.</summary>
        public string[] YardLines;
    }

    public class AsteroidData
    {
        public string Id;
        public string Ore;
        public string BeltId;
        public Vector3 Pos;
        public float Amount; // m3 remaining
    }

    public class PirateConfig
    {
        public string[] Types;
        public int Max;
        public float Interval;
    }

    public class StarSystemData
    {
        public string Id, Name;
        public float Sec;
        public string[] Ores;
        public float Richness;
        public PirateConfig Pirates;
        public string StationName;
        public readonly List<Celestial> Celestials = new List<Celestial>();
        public readonly List<AsteroidData> Asteroids = new List<AsteroidData>();
        public readonly Dictionary<string, float> BeltRespawn = new Dictionary<string, float>();

        public Celestial Find(string id) => Celestials.Find(c => c.Id == id);
    }

    public class UniverseData
    {
        public readonly Dictionary<string, StarSystemData> Systems = new Dictionary<string, StarSystemData>();
    }

    /// <summary>
    /// Procedural but deterministic universe: a small constellation of star
    /// systems joined by stargates. Lower security = richer ore, worse pirates.
    /// </summary>
    public static class UniverseGenerator
    {
        static int _asteroidSeq;

        public static readonly string[][] GatePairs =
        {
            new[] { "solara", "verdant" },
            new[] { "verdant", "krios" },
            new[] { "krios", "nadir" },
            new[] { "verdant", "nadir" },
            new[] { "nadir", "abyss" },
        };

        static readonly string[] Roman = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII" };

        public static UniverseData Build()
        {
            var u = new UniverseData();
            AddSystem(u, "solara", "Solara", 1.0f, "Solara Prime", 4, 2,
                new[] { "hematite", "pyroxene" }, 0.7f, null,
                new[] { "hive", "claw", "fin" });
            AddSystem(u, "verdant", "Verdant", 0.7f, "Verdant Refinery", 3, 3,
                new[] { "hematite", "pyroxene", "plagioclase" }, 1.0f,
                new PirateConfig { Types = new[] { "rookie" }, Max = 1, Interval = 90f }, null);
            AddSystem(u, "krios", "Krios", 0.5f, "Krios Bastion", 5, 4,
                new[] { "pyroxene", "plagioclase", "ilmenite" }, 1.3f,
                new PirateConfig { Types = new[] { "rookie", "rookie", "marauder" }, Max = 2, Interval = 60f },
                new[] { "fin", "talon", "scale" });
            AddSystem(u, "nadir", "Nadir", 0.3f, "Nadir Freeport", 3, 4,
                new[] { "plagioclase", "ilmenite", "beryl" }, 1.7f,
                new PirateConfig { Types = new[] { "rookie", "marauder", "marauder" }, Max = 3, Interval = 45f },
                new[] { "claw", "trail", "pack" });
            AddSystem(u, "abyss", "Abyss", 0.0f, "Outlaw Den", 2, 5,
                new[] { "ilmenite", "beryl", "beryl" }, 2.4f,
                new PirateConfig { Types = new[] { "marauder", "overlord" }, Max = 4, Interval = 40f }, null);

            foreach (var pair in GatePairs)
            {
                AddGate(u.Systems[pair[0]], u.Systems[pair[1]]);
                AddGate(u.Systems[pair[1]], u.Systems[pair[0]]);
            }
            return u;
        }

        static void AddSystem(UniverseData u, string id, string name, float sec, string station,
            int planets, int belts, string[] ores, float richness, PirateConfig pirates,
            string[] yardLines)
        {
            var rng = Rng.Seeded(id);
            var sys = new StarSystemData
            {
                Id = id, Name = name, Sec = sec, StationName = station,
                Ores = ores, Richness = richness, Pirates = pirates,
            };

            sys.Celestials.Add(new Celestial
            {
                Id = id + "_sun", Name = name + " (Star)", Kind = ObjKind.Sun, Pos = Vector3.zero,
            });

            var planetPos = new List<Vector3>();
            for (int p = 0; p < planets; p++)
            {
                float ang = Rng.Range(rng, 0f, Mathf.PI * 2f);
                float r = Rng.Range(rng, 40000f, 250000f);
                var pos = new Vector3(Mathf.Cos(ang) * r, Rng.Range(rng, -300f, 300f), Mathf.Sin(ang) * r);
                planetPos.Add(pos);
                sys.Celestials.Add(new Celestial
                {
                    Id = id + "_planet" + p, Name = name + " " + Roman[p],
                    Kind = ObjKind.Planet, Pos = pos, Hue = Rng.Range(rng, 0f, 1f),
                });
            }

            for (int b = 0; b < belts; b++)
            {
                var near = planetPos[b % planetPos.Count];
                float ang = Rng.Range(rng, 0f, Mathf.PI * 2f);
                float r = Rng.Range(rng, 3000f, 9000f);
                var pos = near + new Vector3(Mathf.Cos(ang) * r, Rng.Range(rng, -200f, 200f), Mathf.Sin(ang) * r);
                sys.Celestials.Add(new Celestial
                {
                    Id = id + "_belt" + b, Name = name + " Belt " + Roman[b],
                    Kind = ObjKind.Belt, Pos = pos,
                });
            }

            if (!string.IsNullOrEmpty(station))
            {
                var near = planetPos[0];
                sys.Celestials.Add(new Celestial
                {
                    Id = id + "_station", Name = station,
                    Kind = ObjKind.Station,
                    YardLines = yardLines,
                    Pos = near + new Vector3(2500f, 150f, 1200f),
                });
            }

            foreach (var c in sys.Celestials)
                if (c.Kind == ObjKind.Belt)
                    SpawnBeltAsteroids(sys, c);

            u.Systems[id] = sys;
        }

        static void AddGate(StarSystemData sys, StarSystemData to)
        {
            var rng = Rng.Seeded(sys.Id + "->" + to.Id);
            float ang = Rng.Range(rng, 0f, Mathf.PI * 2f);
            float r = Rng.Range(rng, 250000f, 330000f);
            sys.Celestials.Add(new Celestial
            {
                Id = sys.Id + "_gate_" + to.Id,
                Name = "Stargate (" + to.Name + ")",
                Kind = ObjKind.Gate,
                GateTo = to.Id,
                Pos = new Vector3(Mathf.Cos(ang) * r, Rng.Range(rng, -300f, 300f), Mathf.Sin(ang) * r),
            });
        }

        public static List<AsteroidData> SpawnBeltAsteroids(StarSystemData sys, Celestial belt)
        {
            var spawned = new List<AsteroidData>();
            int count = Random.Range(6, 14);
            for (int i = 0; i < count; i++)
            {
                string ore = sys.Ores[Random.Range(0, sys.Ores.Length)];
                float ang = Random.Range(0f, Mathf.PI * 2f);
                float r = Random.Range(100f, 500f);
                var a = new AsteroidData
                {
                    Id = "ast_" + sys.Id + "_" + _asteroidSeq++,
                    Ore = ore,
                    BeltId = belt.Id,
                    Pos = belt.Pos + new Vector3(Mathf.Cos(ang) * r, Random.Range(-100f, 100f), Mathf.Sin(ang) * r),
                    Amount = Mathf.Round((600f + Random.value * 2800f) * sys.Richness),
                };
                sys.Asteroids.Add(a);
                spawned.Add(a);
            }
            return spawned;
        }
    }
}
