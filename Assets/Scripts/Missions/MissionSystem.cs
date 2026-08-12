using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>An agent mission: bounty hunt, ore requisition, or courier run.</summary>
    public class Mission
    {
        public string Type; // "bounty" | "mining" | "courier"
        public string Title, Desc;
        public string OriginStationId, OriginSystemId;
        public long Reward;

        // bounty
        public string TargetSystemId;
        public int KillsRequired, KillsDone;

        // mining
        public string OreId;
        public float OreAmount;

        // courier
        public string DestStationId, DestSystemId;
        public float PackageM3;
    }

    /// <summary>
    /// Deterministic mission offers per (station, refresh counter): every
    /// station agent offers one bounty, one mining, and one courier contract.
    /// </summary>
    public static class Missions
    {
        public static List<Mission> GenerateOffers(UniverseData u, string stationId, string systemId, int counter)
        {
            var offers = new List<Mission>();
            var bounty = MakeBounty(u, stationId, systemId, counter);
            if (bounty != null) offers.Add(bounty);
            offers.Add(MakeMining(u, stationId, systemId, counter));
            var courier = MakeCourier(u, stationId, systemId, counter);
            if (courier != null) offers.Add(courier);
            return offers;
        }

        static Mission MakeBounty(UniverseData u, string stationId, string systemId, int counter)
        {
            var rng = Rng.Seeded(stationId + "#bounty#" + counter);
            // Target: one of the two nearest systems that actually have pirates.
            var candidates = new List<string>();
            foreach (var sys in u.Systems.Values)
                if (sys.Pirates != null) candidates.Add(sys.Id);
            if (candidates.Count == 0) return null;
            candidates.Sort((a, b) => JumpCount(systemId, a).CompareTo(JumpCount(systemId, b)));
            string target = candidates[rng.Next(Mathf.Min(2, candidates.Count))];
            var tsys = u.Systems[target];

            long avgBounty = 0;
            foreach (var t in tsys.Pirates.Types) avgBounty += GameData.Npcs[t].Bounty;
            avgBounty /= tsys.Pirates.Types.Length;

            int kills = 2 + rng.Next(4);
            long reward = (long)(kills * avgBounty * 0.8f + 5000f * (1f + (1f - tsys.Sec) * 2f));
            return new Mission
            {
                Type = "bounty",
                Title = "Cull the pirates of " + tsys.Name,
                Desc = "Destroy " + kills + " pirate ships in " + tsys.Name
                     + " (" + tsys.Sec.ToString("0.0") + " sec), then report back here.",
                OriginStationId = stationId, OriginSystemId = systemId,
                TargetSystemId = target, KillsRequired = kills,
                Reward = reward,
            };
        }

        static Mission MakeMining(UniverseData u, string stationId, string systemId, int counter)
        {
            var rng = Rng.Seeded(stationId + "#mining#" + counter);
            var sys = u.Systems[systemId];
            string ore = sys.Ores[rng.Next(sys.Ores.Length)];
            float amount = Mathf.Round(150f + (float)rng.NextDouble() * 350f);
            long reward = (long)(amount * GameData.Ores[ore].PricePerM3 * 1.6f);
            return new Mission
            {
                Type = "mining",
                Title = "Ore requisition: " + GameData.Ores[ore].Name,
                Desc = "Deliver " + amount + " m3 of " + GameData.Ores[ore].Name
                     + " to this station. Source it however you like.",
                OriginStationId = stationId, OriginSystemId = systemId,
                OreId = ore, OreAmount = amount,
                Reward = reward,
            };
        }

        static Mission MakeCourier(UniverseData u, string stationId, string systemId, int counter)
        {
            var rng = Rng.Seeded(stationId + "#courier#" + counter);
            // Any station other than the origin.
            var dests = new List<Celestial>();
            var destSys = new List<string>();
            foreach (var sys in u.Systems.Values)
                foreach (var c in sys.Celestials)
                    if (c.Kind == ObjKind.Station && c.Id != stationId)
                    {
                        dests.Add(c);
                        destSys.Add(sys.Id);
                    }
            if (dests.Count == 0) return null;
            int pick = rng.Next(dests.Count);
            var dest = dests[pick];
            string dsys = destSys[pick];
            float package = Mathf.Round(40f + (float)rng.NextDouble() * 80f);
            int jumps = JumpCount(systemId, dsys);
            long reward = 6000 + jumps * 9000 + (long)(package * 20f);
            return new Mission
            {
                Type = "courier",
                Title = "Courier run to " + dest.Name,
                Desc = "Haul a " + package + " m3 sealed package to " + dest.Name + " in "
                     + u.Systems[dsys].Name + " (" + jumps + " jump" + (jumps == 1 ? "" : "s") + ").",
                OriginStationId = stationId, OriginSystemId = systemId,
                DestStationId = dest.Id, DestSystemId = dsys, PackageM3 = package,
                Reward = reward,
            };
        }

        /// <summary>Gate-graph BFS distance between systems.</summary>
        public static int JumpCount(string from, string to)
        {
            if (from == to) return 0;
            var adj = new Dictionary<string, List<string>>();
            foreach (var pair in UniverseGenerator.GatePairs)
            {
                if (!adj.ContainsKey(pair[0])) adj[pair[0]] = new List<string>();
                if (!adj.ContainsKey(pair[1])) adj[pair[1]] = new List<string>();
                adj[pair[0]].Add(pair[1]);
                adj[pair[1]].Add(pair[0]);
            }
            var dist = new Dictionary<string, int> { [from] = 0 };
            var queue = new Queue<string>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur == to) return dist[cur];
                if (!adj.ContainsKey(cur)) continue;
                foreach (var next in adj[cur])
                    if (!dist.ContainsKey(next))
                    {
                        dist[next] = dist[cur] + 1;
                        queue.Enqueue(next);
                    }
            }
            return 99;
        }
    }
}
