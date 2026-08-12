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
            public float Ore, Module, Ship;
        }

        public static StationMults Mults(string stationId)
        {
            var r = Rng.Seeded(stationId);
            return new StationMults
            {
                Ore = Rng.Range(r, 0.85f, 1.30f),
                Module = Rng.Range(r, 0.90f, 1.25f),
                Ship = Rng.Range(r, 0.95f, 1.15f),
            };
        }

        static float Jitter(string stationId, string itemId, float spread)
        {
            var r = Rng.Seeded(stationId + "::" + itemId);
            return 1f - spread / 2f + (float)r.NextDouble() * spread;
        }

        /// <summary>Price a station pays per m3 of ore (before trade skill).</summary>
        public static long OreSellPrice(string stationId, string oreId)
        {
            float basePrice = GameData.Ores[oreId].PricePerM3;
            return (long)Mathf.Max(1f, Mathf.Round(basePrice * Mults(stationId).Ore * Jitter(stationId, oreId, 0.3f)));
        }

        /// <summary>Price a station charges for a module (before trade skill).</summary>
        public static long ModuleBuyPrice(string stationId, string modId)
        {
            float basePrice = GameData.Modules[modId].Price;
            return (long)Mathf.Max(1f, Mathf.Round(basePrice * Mults(stationId).Module * Jitter(stationId, modId, 0.2f)));
        }

        public static long ShipBuyPrice(string stationId, string shipId)
        {
            float basePrice = GameData.Ships[shipId].Price;
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
