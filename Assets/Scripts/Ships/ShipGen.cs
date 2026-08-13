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

        public static Dictionary<string, float> MaterialCost(Blueprint bp)
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

        static string TypeName(Blueprint bp)
            => bp.TypeId == FinGenerator.TypeId ? "Fin"
             : bp.TypeId == ClawGenerator.TypeId ? "Claw"
             : bp.TypeId == TalonGenerator.TypeId ? "Talon"
             : bp.TypeId == ScaleGenerator.TypeId ? "Scale"
             : bp.TypeId == TrailGenerator.TypeId ? "Trail"
             : bp.TypeId == PackGenerator.TypeId ? "Pack" : "Hive";

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
