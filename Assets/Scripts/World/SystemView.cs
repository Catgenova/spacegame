using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Builds and owns the scene representation of the current star system:
    /// primitives for celestials, asteroids, and NPCs. Everything is created
    /// from code — no prefabs or serialized assets required.
    /// </summary>
    public class SystemView : MonoBehaviour
    {
        public readonly List<SpaceObject> Objects = new List<SpaceObject>();
        public readonly List<NpcPirate> Npcs = new List<NpcPirate>();

        Transform _root;
        static Shader _shader;
        static int _idSeq;

        /// <summary>Which shader the world resolved to — reported at boot, since
        /// it tells you whether the URP path or the Built-in path is live.</summary>
        public static string ShaderName()
        {
            Mat(Color.white);              // force the lookup
            return _shader == null ? "none found" : _shader.name;
        }

        public static Material Mat(Color c, bool emissive = false)
        {
            if (_shader == null)
            {
                _shader = Shader.Find("Universal Render Pipeline/Lit");
                if (_shader == null) _shader = Shader.Find("Standard");
            }
            var m = new Material(_shader) { color = c };
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 2f);
            }
            return m;
        }

        public void LoadSystem(StarSystemData sys)
        {
            if (_root != null) Destroy(_root.gameObject);
            Objects.Clear();
            Npcs.Clear();
            _root = new GameObject("System_" + sys.Id).transform;

            foreach (var c in sys.Celestials) SpawnCelestial(c);
            foreach (var a in sys.Asteroids) SpawnAsteroid(a);
        }

        void SpawnCelestial(Celestial c)
        {
            GameObject go;
            switch (c.Kind)
            {
                case ObjKind.Sun:
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.localScale = Vector3.one * 3000f;
                    go.GetComponent<Renderer>().material = Mat(new Color(1f, 0.93f, 0.75f), true);
                    break;
                case ObjKind.Planet:
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.localScale = Vector3.one * 800f;
                    go.GetComponent<Renderer>().material = Mat(Color.HSVToRGB(c.Hue, 0.45f, 0.65f));
                    break;
                case ObjKind.Station:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.localScale = new Vector3(70f, 25f, 45f);
                    go.GetComponent<Renderer>().material = Mat(new Color(0.35f, 0.55f, 0.85f));
                    AddDetailCube(go.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.3f, 1.5f, 0.3f), new Color(0.5f, 0.7f, 1f));
                    break;
                case ObjKind.Gate:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.transform.localScale = new Vector3(45f, 6f, 45f);
                    go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    go.GetComponent<Renderer>().material = Mat(new Color(1f, 0.8f, 0.4f), true);
                    break;
                case ObjKind.Belt:
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.localScale = Vector3.one * 15f;
                    go.GetComponent<Renderer>().material = Mat(new Color(0.72f, 0.66f, 0.53f, 1f), true);
                    break;
                default:
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    break;
            }
            go.name = c.Name;
            go.transform.SetParent(_root, false);
            go.transform.position = c.Pos;
            var body = go.AddComponent<CelestialBody>();
            body.Id = c.Id;
            body.DisplayName = c.Name;
            body.Kind = c.Kind;
            body.Data = c;
            Objects.Add(body);
        }

        static void AddDetailCube(Transform parent, Vector3 localPos, Vector3 localScale, Color color)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(d.GetComponent<Collider>());
            d.transform.SetParent(parent, false);
            d.transform.localPosition = localPos;
            d.transform.localScale = localScale;
            d.GetComponent<Renderer>().material = Mat(color, true);
        }

        public AsteroidBody SpawnAsteroid(AsteroidData a)
        {
            // One of three models for this ore, picked from the rock's own id so
            // it never changes under the player. The mesh sits on a child so
            // AsteroidBody.SyncScale can keep shrinking the whole rock as it is
            // mined out.
            var go = new GameObject(a.Id);
            go.transform.SetParent(_root, false);
            go.transform.position = a.Pos;
            go.transform.rotation = Random.rotation;
            var ore = GameData.Ores[a.Ore];
            AsteroidMesh.Build(a.Ore, AsteroidMesh.VariantFor(a.Id), go.transform);
            var col = go.AddComponent<SphereCollider>();
            col.radius = 1.15f;
            var body = go.AddComponent<AsteroidBody>();
            body.Id = a.Id;
            body.DisplayName = ore.Name + " Asteroid";
            body.Kind = ObjKind.Asteroid;
            body.Data = a;
            body.SyncScale();
            Objects.Add(body);
            return body;
        }

        public void RemoveObject(SpaceObject obj)
        {
            Objects.Remove(obj);
            if (obj is NpcPirate npc) Npcs.Remove(npc);
            if (obj != null) Destroy(obj.gameObject);
        }

        public Wreck SpawnWreck(NpcDef def, Vector3 pos, List<string> loot)
        {
            var go = new GameObject(def.Name + " Wreck");
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            float s = def.Id == "overlord" ? 2f : def.Id == "marauder" ? 1.4f : 1f;
            ShipVisuals.BuildWreckVisual(go.transform, s);
            var col = go.AddComponent<SphereCollider>();
            col.radius = 4f * s;
            var wreck = go.AddComponent<Wreck>();
            wreck.Id = "wreck_" + _idSeq++;
            wreck.DisplayName = def.Name + " Wreck";
            wreck.Kind = ObjKind.Wreck;
            if (loot != null) wreck.Loot.AddRange(loot);
            Objects.Add(wreck);
            return wreck;
        }

        /// <summary>Spawn an anomaly with its spread of containers. Tier sets
        /// how rich it is, how long the window lasts, and what answers the
        /// noise.</summary>
        public AnomalySite SpawnSite(int tier, Vector3 pos)
        {
            tier = Mathf.Clamp(tier, 1, 3);
            var go = new GameObject("Anomaly T" + tier);
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            ShipVisuals.BuildSiteVisual(go.transform, tier);
            var col = go.AddComponent<SphereCollider>();
            col.radius = 12f;
            var site = go.AddComponent<AnomalySite>();
            site.Id = "site_" + _idSeq++;
            site.Kind = ObjKind.Site;
            site.Tier = tier;
            site.DisplayName = (tier == 3 ? "Shattered Relic Field"
                : tier == 2 ? "Collapsed Survey Hulk" : "Drifting Debris Pocket")
                + " (T" + tier + ")";

            // The window: richer sites give you longer, but not proportionally.
            site.TotalLife = tier == 3 ? 240f : tier == 2 ? 195f : 165f;
            site.Life = site.TotalLife;
            site.RatTimer = site.TotalLife * 0.45f;
            site.RatWave = tier == 3
                ? new[] { "overlord", "marauder", "marauder", "rookie" }
                : tier == 2 ? new[] { "marauder", "marauder", "rookie" }
                : new[] { "rookie", "rookie" };

            int cans = tier == 3 ? 5 : tier == 2 ? 4 : 3;
            string exoticId = GameData.ExoticIdForTier(tier);
            float perCan = tier == 3 ? 11f : tier == 2 ? 7f : 4.5f;
            for (int i = 0; i < cans; i++)
            {
                // Spread them out so clearing the site means covering ground.
                float ang = (i / (float)cans) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                float rad = Random.Range(180f, 120f + 95f * cans);
                var cpos = pos + new Vector3(Mathf.Cos(ang) * rad,
                    Random.Range(-40f, 40f), Mathf.Sin(ang) * rad);
                var cgo = new GameObject("Container");
                cgo.transform.SetParent(_root, false);
                cgo.transform.position = cpos;
                cgo.transform.rotation = Quaternion.Euler(Random.Range(0f, 360f),
                    Random.Range(0f, 360f), Random.Range(0f, 360f));
                ShipVisuals.BuildContainerVisual(cgo.transform, tier);
                var ccol = cgo.AddComponent<SphereCollider>();
                ccol.radius = 5f;
                var can = cgo.AddComponent<SiteContainer>();
                can.Id = "can_" + _idSeq++;
                can.Kind = ObjKind.Container;
                can.DisplayName = "Sealed Container " + (i + 1);
                can.Site = site;
                can.Exotics[exoticId] = Mathf.Round(perCan * Random.Range(0.7f, 1.4f));
                // The deepest container in a rich field may hold a print, of the
                // field's own tier — a Tier 3 anomaly teaches Class 3 work.
                if (Random.value < (tier == 3 ? 0.26f : tier == 2 ? 0.14f : 0.06f))
                {
                    string src = tier == 3 ? "overlord" : "marauder";
                    int rarity = GameData.BpRarityForClass(tier);
                    can.BpLoot.Add(Random.value < 0.6f
                        ? ModGen.RollBlueprint(src, rarity)
                        : ShipGen.RollBlueprint(src, tier, rarity));
                }
                site.Containers.Add(can);
                Objects.Add(can);
            }
            Objects.Add(site);
            return site;
        }

        public NpcPirate SpawnNpc(string typeId, Vector3 pos)
        {
            var def = GameData.Npcs[typeId];
            var go = new GameObject(def.Name);
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            ShipVisuals.BuildNpcVisual(def, go.transform);
            var col = go.AddComponent<SphereCollider>();
            col.radius = def.Id == "convoyhauler" ? 10f : def.Id == "overlord" ? 8f : 5f;
            var npc = go.AddComponent<NpcPirate>();
            npc.Id = "npc_" + _idSeq++;
            npc.Init(def);
            Objects.Add(npc);
            Npcs.Add(npc);
            return npc;
        }
    }
}
