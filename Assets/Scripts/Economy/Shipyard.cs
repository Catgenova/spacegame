using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Station shipyards. A yard holds production licences for a few of the
    /// seven generated ship lines and keeps finished <b>Class 1</b> hulls on the
    /// pad, so a pilot with credits but no blueprint can still fly one.
    ///
    /// Stock is deterministic from the station id, like every other price in the
    /// game: a yard's line-up never changes, so "the Fennec on the pad at Nadir"
    /// is something you can plan a trip around. Only three of the five stations
    /// hold licences, and no yard holds all seven lines, so which hull you can
    /// buy depends on where you are.
    ///
    /// Licensed bodies are production runs, not one-offs, and they carry a
    /// licence premium — buying is always dearer than building. A blueprint
    /// stays the cheap route to a hull, the only route to Class 2 and 3, and
    /// the only route to a body nobody else will ever fly.
    /// </summary>
    public static class Shipyard
    {
        /// <summary>Only the entry class is ever sold finished.</summary>
        public const int StockClass = 1;

        /// <summary>Finished bodies on the pad per licensed line.</summary>
        public const int PerLine = 2;

        /// <summary>Licence premium over the raw hull value.</summary>
        public const float LicenceMult = 1.2f;

        public static bool Has(Celestial station)
            => station != null && station.Kind == ObjKind.Station
               && station.YardLines != null && station.YardLines.Length > 0;

        /// <summary>Hull ids this yard has finished and waiting, in a stable order.</summary>
        public static List<string> Stock(Celestial station)
        {
            var list = new List<string>();
            if (!Has(station)) return list;
            foreach (var line in station.YardLines)
                for (int i = 0; i < PerLine; i++)
                    list.Add(HullId(station.Id, line, i));
            return list;
        }

        /// <summary>True only for a hull actually sitting on this station's pad.</summary>
        public static bool Sells(Celestial station, string hullId)
        {
            if (!Has(station) || string.IsNullOrEmpty(hullId)) return false;
            foreach (var line in station.YardLines)
                for (int i = 0; i < PerLine; i++)
                    if (HullId(station.Id, line, i) == hullId) return true;
            return false;
        }

        /// <summary>Sticker price before trade skill and trade-in.</summary>
        public static long Price(string stationId, string hullId)
            => (long)Mathf.Round(Market.ShipBuyPrice(stationId, hullId) * LicenceMult);

        public static string HullId(string stationId, string line, int index)
            => ShipGen.IdFromHash(line, BodyHash(stationId, line, index), StockClass);

        /// <summary>Ten digits derived from the station, so the pad never shuffles.</summary>
        public static string BodyHash(string stationId, string line, int index)
        {
            var rng = Rng.Stream("yard:" + stationId + ":" + line + ":" + index);
            var s = "";
            for (int i = 0; i < 10; i++) s += rng.Next(10).ToString();
            return s;
        }

        /// <summary>"Solara Prime, Krios Bastion, Nadir Freeport" — for telling a
        /// docked pilot where the yards actually are.</summary>
        public static string YardNames(UniverseData u)
        {
            var names = new List<string>();
            if (u != null)
                foreach (var sys in u.Systems.Values)
                    foreach (var c in sys.Celestials)
                        if (Has(c)) names.Add(c.Name);
            return string.Join(", ", names.ToArray());
        }

        /// <summary>The lines a yard is licensed for, as display names.</summary>
        public static string LineNames(Celestial station)
        {
            if (!Has(station)) return "";
            var names = new List<string>();
            foreach (var line in station.YardLines) names.Add(ShipGen.LineName(line));
            return string.Join(" / ", names.ToArray());
        }
    }
}
