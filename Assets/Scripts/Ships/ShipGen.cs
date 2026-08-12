using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Routes generated-ship calls to the right line (Hive, Fin) so the
    /// rest of the game never cares which type a blueprint or hull id is.
    /// </summary>
    public static class ShipGen
    {
        public static bool IsGeneratedId(string id)
            => HiveGenerator.IsHiveId(id) || FinGenerator.IsFinId(id) || ClawGenerator.IsClawId(id);

        public static ShipDef ResolveGenerated(string id)
        {
            if (HiveGenerator.IsHiveId(id))
                return HiveGenerator.Def(HiveGenerator.HashFromId(id), HiveGenerator.ClassFromId(id));
            if (FinGenerator.IsFinId(id))
                return FinGenerator.Def(FinGenerator.HashFromId(id), FinGenerator.ClassFromId(id));
            if (ClawGenerator.IsClawId(id))
                return ClawGenerator.Def(ClawGenerator.HashFromId(id), ClawGenerator.ClassFromId(id));
            return null;
        }

        public static ShipDef Def(Blueprint bp)
            => bp.TypeId == FinGenerator.TypeId ? FinGenerator.Def(bp.Hash, bp.Class)
             : bp.TypeId == ClawGenerator.TypeId ? ClawGenerator.Def(bp.Hash, bp.Class)
             : HiveGenerator.Def(bp.Hash, bp.Class);

        public static Dictionary<string, float> MaterialCost(Blueprint bp)
            => bp.TypeId == FinGenerator.TypeId ? FinGenerator.MaterialCost(bp.Class)
             : bp.TypeId == ClawGenerator.TypeId ? ClawGenerator.MaterialCost(bp.Class)
             : HiveGenerator.MaterialCost(bp.Class);

        public static long Fee(Blueprint bp)
            => bp.TypeId == FinGenerator.TypeId ? FinGenerator.Fee(bp.Class)
             : bp.TypeId == ClawGenerator.TypeId ? ClawGenerator.Fee(bp.Class)
             : HiveGenerator.Fee(bp.Class);

        /// <summary>Wreck loot: Fins drop often, Claws feed industry, Hives carry the exotics.</summary>
        public static Blueprint RollBlueprint(string npcId)
        {
            float r = Random.value;
            if (r < 0.40f) return FinGenerator.RollBlueprint(npcId);
            if (r < 0.62f) return ClawGenerator.RollBlueprint(npcId);
            return HiveGenerator.RollBlueprint(npcId);
        }

        static string TypeName(Blueprint bp)
            => bp.TypeId == FinGenerator.TypeId ? "Fin"
             : bp.TypeId == ClawGenerator.TypeId ? "Claw" : "Hive";

        public static string DescribeBlueprint(Blueprint bp)
        {
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
            return HiveShipMesh.Build(HiveGenerator.HashFromId(hullId), HiveGenerator.ClassFromId(hullId), shipRoot);
        }
    }
}
