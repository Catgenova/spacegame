using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>An agent mission: bounty, mining, courier, salvage, or convoy kill.</summary>
    public class Mission
    {
        public string Type; // "bounty" | "mining" | "courier" | "salvage" | "convoykill"
        public string Title, Desc;
        public string OriginStationId, OriginSystemId;
        public long Reward;

        // bounty / convoykill
        public string TargetSystemId;
        public int KillsRequired, KillsDone;

        // mining
        public string OreId;
        public float OreAmount;

        // courier
        public string DestStationId, DestSystemId;
        public float PackageM3;

        // rush contracts: seconds remaining; 0 = no time limit
        public float TimeLeft;

        // salvage
        public int SalvageRequired, SalvageDone;

        // story arc stage (0 = not an arc mission)
        public int ArcStage;
    }

    /// <summary>
    /// Deterministic mission offers per (station, refresh counter): every
    /// station agent offers one bounty, one mining, and one courier contract.
    /// </summary>
    public static class Missions
    {
        /// <summary>Low-sec agents pay more for everything.</summary>
        static float SecPayFactor(UniverseData u, string systemId)
            => 1f + (1f - u.Systems[systemId].Sec) * 0.5f;

        public static List<Mission> GenerateOffers(UniverseData u, string stationId, string systemId, int counter)
        {
            var offers = new List<Mission>();
            var bounty = MakeBounty(u, stationId, systemId, counter);
            if (bounty != null) offers.Add(bounty);
            offers.Add(MakeMining(u, stationId, systemId, counter));
            var courier = MakeCourier(u, stationId, systemId, counter);
            if (courier != null) offers.Add(courier);
            offers.Add(MakeSalvage(u, stationId, systemId, counter));
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
            long reward = (long)((6000 + jumps * 9000 + (long)(package * 20f)) * SecPayFactor(u, systemId));

            // Half of all courier contracts are rush jobs: bigger pay, hard deadline.
            bool rush = rng.NextDouble() < 0.5;
            float timeLimit = rush ? jumps * 90f + 150f : 0f;
            if (rush) reward = (long)(reward * 1.8f);

            return new Mission
            {
                Type = "courier",
                Title = (rush ? "RUSH courier to " : "Courier run to ") + dest.Name,
                Desc = "Haul a " + package + " m3 sealed package to " + dest.Name + " in "
                     + u.Systems[dsys].Name + " (" + jumps + " jump" + (jumps == 1 ? "" : "s") + ")."
                     + (rush ? " Deadline: " + GameData.FmtTime(timeLimit) + " from acceptance." : ""),
                OriginStationId = stationId, OriginSystemId = systemId,
                DestStationId = dest.Id, DestSystemId = dsys, PackageM3 = package,
                TimeLeft = timeLimit,
                Reward = reward,
            };
        }

        static Mission MakeSalvage(UniverseData u, string stationId, string systemId, int counter)
        {
            var rng = Rng.Seeded(stationId + "#salvage#" + counter);
            int n = 3 + rng.Next(4);
            long reward = (long)(n * 5200f * SecPayFactor(u, systemId));
            return new Mission
            {
                Type = "salvage",
                Title = "Salvage contract: " + n + " modules",
                Desc = "Recover " + n + " modules from pirate wrecks (loot them in space), "
                     + "then report back to this agent. The salvage itself is yours to keep.",
                OriginStationId = stationId, OriginSystemId = systemId,
                SalvageRequired = n,
                Reward = reward,
            };
        }

        /// <summary>
        /// "The Abyss Job" — a hand-authored 3-stage arc offered to pilots with
        /// Trusted standing. Courier -> combat -> convoy hunt.
        /// </summary>
        public static Mission BuildArcStage(UniverseData u, int stage, string stationId, string systemId)
        {
            switch (stage)
            {
                case 1:
                    return new Mission
                    {
                        Type = "courier", ArcStage = 1,
                        Title = "The Abyss Job (1/3): quiet delivery",
                        Desc = "An unmarked 60 m3 crate needs to reach Nadir Freeport. "
                             + "No questions, no manifest. Return here afterwards? No — "
                             + "report to the agent at the destination... on second thought, "
                             + "bring the receipt back here.",
                        OriginStationId = stationId, OriginSystemId = systemId,
                        DestStationId = "nadir_station", DestSystemId = "nadir",
                        PackageM3 = 60f,
                        Reward = 40000,
                    };
                case 2:
                    return new Mission
                    {
                        Type = "bounty", ArcStage = 2,
                        Title = "The Abyss Job (2/3): clearing the way",
                        Desc = "The crate stirred something up. Destroy 4 pirate ships in "
                             + "Abyss to thin out the response, then report back.",
                        OriginStationId = stationId, OriginSystemId = systemId,
                        TargetSystemId = "abyss", KillsRequired = 4,
                        Reward = 80000,
                    };
                default:
                    return new Mission
                    {
                        Type = "convoykill", ArcStage = 3,
                        Title = "The Abyss Job (3/3): the shipment",
                        Desc = "The real cargo moves under escort. Find a pirate convoy "
                             + "(they run the belts of low-sec systems), destroy its hauler, "
                             + "and report back. This settles the job.",
                        OriginStationId = stationId, OriginSystemId = systemId,
                        KillsRequired = 1,
                        Reward = 250000,
                    };
            }
        }

        /// <summary>One-line live progress text, shared by both HUDs.</summary>
        public static string ProgressText(GameManager gm, Mission m)
        {
            string text;
            switch (m.Type)
            {
                case "bounty":
                    text = m.KillsDone + "/" + m.KillsRequired + " pirates in "
                        + gm.Universe.Systems[m.TargetSystemId].Name
                        + (m.KillsDone >= m.KillsRequired
                            ? " — return to " + gm.StationName(m.OriginSystemId, m.OriginStationId) : "");
                    break;
                case "mining":
                {
                    gm.Player.Cargo.TryGetValue(m.OreId, out float have);
                    text = Mathf.Round(Mathf.Min(have, m.OreAmount)) + "/" + m.OreAmount + " m3 "
                        + GameData.Ores[m.OreId].Name + " — deliver to "
                        + gm.StationName(m.OriginSystemId, m.OriginStationId);
                    break;
                }
                case "courier":
                    text = "Deliver package to " + gm.StationName(m.DestSystemId, m.DestStationId)
                        + " in " + gm.Universe.Systems[m.DestSystemId].Name;
                    break;
                case "salvage":
                    text = m.SalvageDone + "/" + m.SalvageRequired + " salvage recovered"
                        + (m.SalvageDone >= m.SalvageRequired
                            ? " — report to " + gm.StationName(m.OriginSystemId, m.OriginStationId) : "");
                    break;
                case "convoykill":
                    text = m.KillsDone >= 1
                        ? "Hauler destroyed — report to " + gm.StationName(m.OriginSystemId, m.OriginStationId)
                        : "Destroy a convoy hauler (convoys roam low-sec belts)";
                    break;
                default:
                    text = "";
                    break;
            }
            if (m.TimeLeft > 0f) text += "   [" + GameData.FmtTime(m.TimeLeft) + " left]";
            return text;
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
