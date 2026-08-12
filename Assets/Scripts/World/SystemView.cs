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
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = a.Id;
            go.transform.SetParent(_root, false);
            go.transform.position = a.Pos;
            go.transform.rotation = Random.rotation;
            var ore = GameData.Ores[a.Ore];
            go.GetComponent<Renderer>().material = Mat(Color.Lerp(new Color(0.42f, 0.38f, 0.32f), ore.Color, 0.4f));
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

        public NpcPirate SpawnNpc(string typeId, Vector3 pos)
        {
            var def = GameData.Npcs[typeId];
            var go = new GameObject(def.Name);
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            ShipVisuals.BuildNpcVisual(def, go.transform);
            var col = go.AddComponent<SphereCollider>();
            col.radius = def.Id == "overlord" ? 8f : 5f;
            var npc = go.AddComponent<NpcPirate>();
            npc.Id = "npc_" + go.GetInstanceID();
            npc.Init(def);
            Objects.Add(npc);
            Npcs.Add(npc);
            return npc;
        }
    }
}
