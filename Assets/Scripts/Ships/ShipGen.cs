using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Routes generated-ship calls to the right line (Hive, Fin, Claw,
    /// Talon, Scale, Trail, Pack) so the rest of the game never cares
    /// which type a blueprint or hull id is.
    /// </summary>
    public static class ShipGen
    {
        /// <summary>Every generated line, in catalogue order.</summary>
        public static readonly string[] TypeIds =
        {
            HiveGenerator.TypeId, FinGenerator.TypeId, ClawGenerator.TypeId,
            TalonGenerator.TypeId, ScaleGenerator.TypeId, TrailGenerator.TypeId,
            PackGenerator.TypeId,
        };

        /// <summary>The free hull a new — or freshly cloned — pilot is issued.
        ///
        /// It is a Claw-class Urchin: the generated fleet's mining line, and the
        /// only line whose hulls are not turret-only up top. It carries a Mining
        /// Claw, has no gun, and mines at 1.6x, so a rookie starts by working
        /// safe Solara rock and buys their way into a gunship. Fixed body, so
        /// every pilot's loaner is the same recognisable hull.</summary>
        public const string RookieLine = ClawGenerator.TypeId;

        public static string RookieHullId => IdFromHash(RookieLine, BodyHash("rookie"), 1);

        /// <summary>Ten digits from a stable key, for hulls the game itself has
        /// to mint (the loaner, and replacements for retired catalogue ships).</summary>
        public static string BodyHash(string key)
        {
            var rng = Rng.Stream("hull:" + key);
            var s = "";
            for (int i = 0; i < 10; i++) s += rng.Next(10).ToString();
            return s;
        }

        /// <summary>The hand-written catalogue hulls (Wasp, Prospector, Talon,
        /// Mule, Aurora) are gone — every ship is generated now. A save from
        /// before that holds one of their ids, and SaveSystem rejects a save
        /// whose hull does not resolve, so without this the whole save would be
        /// discarded and the pilot would lose everything. Each retires into a
        /// Class 1 body on the line that took over its job.</summary>
        static readonly Dictionary<string, string> LegacyHullLines = new Dictionary<string, string>
        {
            { "wasp", ClawGenerator.TypeId },        // rookie   -> the loaner itself
            { "prospector", ClawGenerator.TypeId },  // miner    -> Claw Urchin
            { "talon", TalonGenerator.TypeId },      // gunboat  -> Talon Kestrel
            { "mule", PackGenerator.TypeId },        // hauler   -> Pack Bactrian
            { "aurora", ScaleGenerator.TypeId },     // cruiser  -> Scale Python
        };

        /// <summary>Map a possibly-retired hull id onto a current one. Unknown
        /// ids pass through untouched for the caller to reject.</summary>
        public static string MigrateHullId(string id)
        {
            if (id == null) return null;
            if (id == "wasp") return RookieHullId;
            return LegacyHullLines.TryGetValue(id, out var line)
                ? IdFromHash(line, BodyHash("legacy:" + id), 1)
                : id;
        }

        public static bool IsGeneratedId(string id)
            => HiveGenerator.IsHiveId(id) || FinGenerator.IsFinId(id)
               || ClawGenerator.IsClawId(id) || TalonGenerator.IsTalonId(id)
               || ScaleGenerator.IsScaleId(id) || TrailGenerator.IsTrailId(id)
               || PackGenerator.IsPackId(id);

        public static ShipDef ResolveGenerated(string id)
        {
            if (HiveGenerator.IsHiveId(id))
                return HiveGenerator.Def(HiveGenerator.HashFromId(id), HiveGenerator.ClassFromId(id));
            if (FinGenerator.IsFinId(id))
                return FinGenerator.Def(FinGenerator.HashFromId(id), FinGenerator.ClassFromId(id));
            if (ClawGenerator.IsClawId(id))
                return ClawGenerator.Def(ClawGenerator.HashFromId(id), ClawGenerator.ClassFromId(id));
            if (TalonGenerator.IsTalonId(id))
                return TalonGenerator.Def(TalonGenerator.HashFromId(id), TalonGenerator.ClassFromId(id));
            if (ScaleGenerator.IsScaleId(id))
                return ScaleGenerator.Def(ScaleGenerator.HashFromId(id), ScaleGenerator.ClassFromId(id));
            if (TrailGenerator.IsTrailId(id))
                return TrailGenerator.Def(TrailGenerator.HashFromId(id), TrailGenerator.ClassFromId(id));
            if (PackGenerator.IsPackId(id))
                return PackGenerator.Def(PackGenerator.HashFromId(id), PackGenerator.ClassFromId(id));
            return null;
        }

        public static ShipDef Def(Blueprint bp)
            => bp.TypeId == FinGenerator.TypeId ? FinGenerator.Def(bp.Hash, bp.Class)
             : bp.TypeId == ClawGenerator.TypeId ? ClawGenerator.Def(bp.Hash, bp.Class)
             : bp.TypeId == TalonGenerator.TypeId ? TalonGenerator.Def(bp.Hash, bp.Class)
             : bp.TypeId == ScaleGenerator.TypeId ? ScaleGenerator.Def(bp.Hash, bp.Class)
             : bp.TypeId == TrailGenerator.TypeId ? TrailGenerator.Def(bp.Hash, bp.Class)
             : bp.TypeId == PackGenerator.TypeId ? PackGenerator.Def(bp.Hash, bp.Class)
             : HiveGenerator.Def(bp.Hash, bp.Class);

        /// <summary>Material bill for one hull run. Returns a fresh dictionary —
        /// the generators hand back their shared static sheets, which callers must
        /// never mutate. Class 3 hulls additionally need anomaly exotics, so the
        /// top of the ship tree is gated behind exploration too.</summary>
        public static Dictionary<string, float> MaterialCost(Blueprint bp)
        {
            var basis = BaseMaterialCost(bp);
            var cost = new Dictionary<string, float>(basis);
            float bulk = 0f;
            foreach (var kv in basis) bulk += kv.Value;
            // Nickel superalloy for the drive and the hot structure — every hull
            // in the game needs some, which is what makes the common Fe-Ni rock
            // worth mining rather than just the rich stuff.
            cost["nickel"] = Mathf.Max(4f, Mathf.Round(bulk * 0.07f));
            if (bp.Class >= 3)
            {
                cost["tantalum"] = Mathf.Max(2f, Mathf.Round(bulk * 0.006f));
                cost["hafnium"] = Mathf.Max(1f, Mathf.Round(bulk * 0.0025f));
            }
            return cost;
        }

        static Dictionary<string, float> BaseMaterialCost(Blueprint bp)
            => bp.TypeId == FinGenerator.TypeId ? FinGenerator.MaterialCost(bp.Class)
             : bp.TypeId == ClawGenerator.TypeId ? ClawGenerator.MaterialCost(bp.Class)
             : bp.TypeId == TalonGenerator.TypeId ? TalonGenerator.MaterialCost(bp.Class)
             : bp.TypeId == ScaleGenerator.TypeId ? ScaleGenerator.MaterialCost(bp.Class)
             : bp.TypeId == TrailGenerator.TypeId ? TrailGenerator.MaterialCost(bp.Class)
             : bp.TypeId == PackGenerator.TypeId ? PackGenerator.MaterialCost(bp.Class)
             : HiveGenerator.MaterialCost(bp.Class);

        public static long Fee(Blueprint bp)
            => bp.TypeId == FinGenerator.TypeId ? FinGenerator.Fee(bp.Class)
             : bp.TypeId == ClawGenerator.TypeId ? ClawGenerator.Fee(bp.Class)
             : bp.TypeId == TalonGenerator.TypeId ? TalonGenerator.Fee(bp.Class)
             : bp.TypeId == ScaleGenerator.TypeId ? ScaleGenerator.Fee(bp.Class)
             : bp.TypeId == TrailGenerator.TypeId ? TrailGenerator.Fee(bp.Class)
             : bp.TypeId == PackGenerator.TypeId ? PackGenerator.Fee(bp.Class)
             : HiveGenerator.Fee(bp.Class);

        /// <summary>Wreck loot: Fins drop often, Claws feed industry, Talons
        /// command the flock, Scales anchor the line, Trails find what the
        /// rest missed, Packs haul it all home, Hives carry the
        /// exotics.</summary>
        public static Blueprint RollBlueprint(string npcId)
        {
            float r = Random.value;
            if (r < 0.26f) return FinGenerator.RollBlueprint(npcId);
            if (r < 0.44f) return ClawGenerator.RollBlueprint(npcId);
            if (r < 0.58f) return TalonGenerator.RollBlueprint(npcId);
            if (r < 0.64f) return ScaleGenerator.RollBlueprint(npcId);
            if (r < 0.72f) return TrailGenerator.RollBlueprint(npcId);
            if (r < 0.80f) return PackGenerator.RollBlueprint(npcId);
            return HiveGenerator.RollBlueprint(npcId);
        }

        /// <summary>Hull id for a body on a named line, e.g. ("fin","0421…",1).</summary>
        public static string IdFromHash(string typeId, string hash, int cls)
            => typeId == FinGenerator.TypeId ? FinGenerator.IdFromHash(hash, cls)
             : typeId == ClawGenerator.TypeId ? ClawGenerator.IdFromHash(hash, cls)
             : typeId == TalonGenerator.TypeId ? TalonGenerator.IdFromHash(hash, cls)
             : typeId == ScaleGenerator.TypeId ? ScaleGenerator.IdFromHash(hash, cls)
             : typeId == TrailGenerator.TypeId ? TrailGenerator.IdFromHash(hash, cls)
             : typeId == PackGenerator.TypeId ? PackGenerator.IdFromHash(hash, cls)
             : HiveGenerator.IdFromHash(hash, cls);

        public static string LineName(string typeId)
            => typeId == FinGenerator.TypeId ? "Fin"
             : typeId == ClawGenerator.TypeId ? "Claw"
             : typeId == TalonGenerator.TypeId ? "Talon"
             : typeId == ScaleGenerator.TypeId ? "Scale"
             : typeId == TrailGenerator.TypeId ? "Trail"
             : typeId == PackGenerator.TypeId ? "Pack" : "Hive";

        static string TypeName(Blueprint bp) => LineName(bp.TypeId);

        public static string DescribeBlueprint(Blueprint bp)
        {
            if (bp.IsModule) return ModGen.Describe(bp);
            var def = Def(bp);
            return def.Name + " (" + TypeName(bp) + " C" + bp.Class
                + ")  [" + GameData.RarityNames[bp.Rarity] + ", "
                + bp.RunsLeft + " run" + (bp.RunsLeft == 1 ? "" : "s") + " left]";
        }

        public static GameObject BuildHull(string hullId, Transform shipRoot)
        {
            if (FinGenerator.IsFinId(hullId))
                return FinShipMesh.Build(FinGenerator.HashFromId(hullId), FinGenerator.ClassFromId(hullId), shipRoot);
            if (ClawGenerator.IsClawId(hullId))
                return ClawShipMesh.Build(ClawGenerator.HashFromId(hullId), ClawGenerator.ClassFromId(hullId), shipRoot);
            if (TalonGenerator.IsTalonId(hullId))
                return TalonShipMesh.Build(TalonGenerator.HashFromId(hullId), TalonGenerator.ClassFromId(hullId), shipRoot);
            if (ScaleGenerator.IsScaleId(hullId))
                return ScaleShipMesh.Build(ScaleGenerator.HashFromId(hullId), ScaleGenerator.ClassFromId(hullId), shipRoot);
            if (TrailGenerator.IsTrailId(hullId))
                return TrailShipMesh.Build(TrailGenerator.HashFromId(hullId), TrailGenerator.ClassFromId(hullId), shipRoot);
            if (PackGenerator.IsPackId(hullId))
                return PackShipMesh.Build(PackGenerator.HashFromId(hullId), PackGenerator.ClassFromId(hullId), shipRoot);
            return HiveShipMesh.Build(HiveGenerator.HashFromId(hullId), HiveGenerator.ClassFromId(hullId), shipRoot);
        }
    }
}
