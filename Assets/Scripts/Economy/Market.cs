using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Station markets. Prices are deterministic per station, so every station
    /// has a stable personality: some pay more for ore, some sell cheap
    /// modules. Hauling between stations is a viable career.
    /// </summary>
    public static class Market
    {
        public struct StationMults
        {
            public float Ore, Ship;
        }

        public static StationMults Mults(string stationId)
        {
            var r = Rng.Seeded(stationId);
            float ore = Rng.Range(r, 0.85f, 1.30f);
            // Stations no longer stock modules, but this draw has to stay: the
            // three multipliers come off one stream in order, so dropping it
            // would silently reprice every hull in the game.
            Rng.Range(r, 0.90f, 1.25f);
            return new StationMults { Ore = ore, Ship = Rng.Range(r, 0.95f, 1.15f) };
        }

        static float Jitter(string stationId, string itemId, float spread)
        {
            var r = Rng.Seeded(stationId + "::" + itemId);
            return 1f - spread / 2f + (float)r.NextDouble() * spread;
        }

        /// <summary>Price a station pays per m3 of ore or mineral (before trade skill).</summary>
        public static long OreSellPrice(string stationId, string commodityId)
        {
            float basePrice = GameData.Commodity(commodityId).PricePerM3;
            return (long)Mathf.Max(1f, Mathf.Round(basePrice * Mults(stationId).Ore * Jitter(stationId, commodityId, 0.3f)));
        }

        // Stations do not sell modules at all: every fittable module in the game
        // is salvaged from a drifting cache or printed from a blueprint. A module
        // has a Price only so blueprint material bills and fees can scale off it.

        public static long ShipBuyPrice(string stationId, string shipId)
        {
            float basePrice = GameData.ResolveShip(shipId).Price;
            return (long)Mathf.Max(1f, Mathf.Round(basePrice * Mults(stationId).Ship * Jitter(stationId, shipId, 0.1f)));
        }

        /// <summary>Trade-skill price adjustment: 2% per level.</summary>
        public static long ApplyTradeSkill(long price, int tradeLevel, bool selling)
        {
            float f = 0.02f * tradeLevel;
            return (long)Mathf.Round(selling ? price * (1f + f) : price * (1f - f));
        }

        public static long ShipTradeInValue(string stationId, string shipId)
            => (long)Mathf.Round(ShipBuyPrice(stationId, shipId) * 0.6f);
    }
}
