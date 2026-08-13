using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame
{
    /// <summary>
    /// Module blueprints and the crafted modules they produce.
    ///
    /// Module blueprints invert the ship-blueprint economy. A ship blueprint
    /// gets *more* runs as it gets rarer; a module blueprint gets *fewer*, but
    /// each run comes out better:
    ///
    ///   Common    5 runs   1x materials   base stats only
    ///   Uncommon  3 runs   2x materials   1 modifier
    ///   Rare      2 runs   3x materials   3 modifiers
    ///   Pristine  1 run    5x materials   5 modifiers
    ///
    /// So a Common print is a production line and a Pristine print is a single
    /// masterwork you will never make again.
    ///
    /// A crafted module is identified by a generated id — "m:claw2:1234567890:3"
    /// — which resolves to a ModuleDef with its modifiers already applied. That
    /// keeps every existing string-keyed path (fitting arrays, station storage,
    /// cargo, saves) working untouched, exactly as generated hulls do.
    /// </summary>
    public static class ModGen
    {
        public const string Prefix = "m:";

        public static bool IsModId(string id)
            => !string.IsNullOrEmpty(id) && id.StartsWith(Prefix);

        /// <summary>Base module ids that can be printed, by slot family.</summary>
        // Turrets print here too. Stations sell no gear at all, so blueprints are
        // the only route to a bigger gun, a second mining claw, or a replacement
        // for anything you flew into a rock. Mining is the Claw line's job now,
        // so the mining tools here are claws — there are no high-slot lasers.
        public static readonly string[] Hardpoint =
            { "blaster1", "rail1", "rail2",
              "claw1", "claw2", "web1", "web2", "disrupt1", "disrupt2",
              "sensor1", "sensor2", "collector1", "collector2", "drone1", "drone2" };
        public static readonly string[] MidSlot =
            { "shieldboost1", "shieldboost2", "afterburner1", "afterburner2" };
        public static readonly string[] LowSlot =
            { "cargo1", "cargo2", "plate1", "plate2", "capbattery1", "capbattery2" };

        public static IEnumerable<string> AllPrintable()
        {
            foreach (var id in Hardpoint) yield return id;
            foreach (var id in MidSlot) yield return id;
            foreach (var id in LowSlot) yield return id;
        }

        // ---------- modifiers ----------
        // Each modifier only applies to a stat the base module actually uses,
        // so a Cargo Expander rolls capacity perks and a claw rolls yield perks.

        public enum Stat { Cycle, Yield, Dmg, Range, CapUse, Boost, Speed, Cargo, Armor, Cap, Track }

        struct Mod
        {
            public Stat S;
            public string Label;
            public float Amount;   // fraction of the base value
        }

        static readonly Mod[] Pool =
        {
            new Mod { S = Stat.Cycle,  Label = "Overclocked",  Amount = -0.10f },
            new Mod { S = Stat.Yield,  Label = "High-Grade",   Amount = 0.14f },
            new Mod { S = Stat.Dmg,    Label = "Uprated",      Amount = 0.14f },
            new Mod { S = Stat.Range,  Label = "Extended",     Amount = 0.18f },
            new Mod { S = Stat.CapUse, Label = "Efficient",    Amount = -0.15f },
            new Mod { S = Stat.Boost,  Label = "Amplified",    Amount = 0.16f },
            new Mod { S = Stat.Speed,  Label = "Tuned",        Amount = 0.10f },
            new Mod { S = Stat.Cargo,  Label = "Cavernous",    Amount = 0.20f },
            new Mod { S = Stat.Armor,  Label = "Reinforced",   Amount = 0.20f },
            new Mod { S = Stat.Cap,    Label = "Capacious",    Amount = 0.20f },
            new Mod { S = Stat.Track,  Label = "Gyrostable",   Amount = 0.22f },
        };

        /// <summary>Which modifiers make sense on this base module.</summary>
        static List<int> Eligible(ModuleDef b)
        {
            var ok = new List<int>();
            for (int i = 0; i < Pool.Length; i++)
            {
                switch (Pool[i].S)
                {
                    case Stat.Cycle: if (b.Cycle > 0f) ok.Add(i); break;
                    case Stat.Yield: if (b.Yield > 0f) ok.Add(i); break;
                    case Stat.Dmg: if (b.Dmg > 0f) ok.Add(i); break;
                    case Stat.Range: if (b.Range > 0f) ok.Add(i); break;
                    case Stat.CapUse: if (b.CapUse > 0f) ok.Add(i); break;
                    case Stat.Boost: if (b.BoostAmount > 0f) ok.Add(i); break;
                    case Stat.Speed: if (b.SpeedMult > 1f) ok.Add(i); break;
                    case Stat.Cargo: if (b.CargoBonus > 0f) ok.Add(i); break;
                    case Stat.Armor: if (b.ArmorBonus > 0f) ok.Add(i); break;
                    case Stat.Cap: if (b.CapBonus > 0f) ok.Add(i); break;
                    case Stat.Track: if (b.Tracking > 0f) ok.Add(i); break;
                }
            }
            return ok;
        }

        /// <summary>Compose a crafted-module id.</summary>
        public static string CraftId(string baseId, string hash, int rarity)
            => Prefix + baseId + ":" + hash + ":" + Mathf.Clamp(rarity, 0, 3);

        static bool Split(string id, out string baseId, out string hash, out int rarity)
        {
            baseId = null; hash = null; rarity = 0;
            if (!IsModId(id)) return false;
            var parts = id.Substring(Prefix.Length).Split(':');
            if (parts.Length != 3) return false;
            baseId = parts[0];
            hash = parts[1];
            if (!GameData.Modules.ContainsKey(baseId)) return false;
            int.TryParse(parts[2], out rarity);
            rarity = Mathf.Clamp(rarity, 0, 3);
            return true;
        }

        static readonly Dictionary<string, ModuleDef> _cache = new Dictionary<string, ModuleDef>();

        /// <summary>Resolve a crafted-module id into a finished ModuleDef with
        /// its rolled modifiers baked in. Deterministic: the same id always
        /// yields the same stats.</summary>
        public static ModuleDef Resolve(string id)
        {
            if (_cache.TryGetValue(id, out var cached)) return cached;
            if (!Split(id, out string baseId, out string hash, out int rarity)) return null;

            var b = GameData.Modules[baseId];
            int want = GameData.ModBpMods[rarity];

            // Copy the base sheet, then apply modifiers.
            var d = new ModuleDef
            {
                Id = id, Name = b.Name, Short = b.Short, Slot = b.Slot, Kind = b.Kind,
                Price = b.Price, Cycle = b.Cycle, Range = b.Range, CapUse = b.CapUse,
                Yield = b.Yield, Dmg = b.Dmg, Tracking = b.Tracking,
                BoostAmount = b.BoostAmount, SpeedMult = b.SpeedMult,
                CargoBonus = b.CargoBonus, ArmorBonus = b.ArmorBonus, CapBonus = b.CapBonus,
            };

            var picks = RollMods(baseId, hash, want);
            var labels = new List<string>();
            // A module with few eligible stats would otherwise stack every
            // modifier on one number; repeats past the first apply with
            // diminishing strength so power stays in band across families.
            var seen = new Dictionary<Stat, int>();
            foreach (int mi in picks)
            {
                var m = Pool[mi];
                seen.TryGetValue(m.S, out int rep);
                seen[m.S] = rep + 1;
                m.Amount *= Falloff(rep);
                labels.Add(rep == 0 ? m.Label : m.Label + "+");
                switch (m.S)
                {
                    case Stat.Cycle: d.Cycle = Mathf.Max(0.4f, d.Cycle * (1f + m.Amount)); break;
                    case Stat.Yield: d.Yield *= 1f + m.Amount; break;
                    case Stat.Dmg: d.Dmg *= 1f + m.Amount; break;
                    case Stat.Range: d.Range *= 1f + m.Amount; break;
                    case Stat.CapUse: d.CapUse = Mathf.Max(0.5f, d.CapUse * (1f + m.Amount)); break;
                    case Stat.Boost: d.BoostAmount *= 1f + m.Amount; break;
                    case Stat.Speed: d.SpeedMult = 1f + (d.SpeedMult - 1f) * (1f + m.Amount); break;
                    case Stat.Cargo: d.CargoBonus *= 1f + m.Amount; break;
                    case Stat.Armor: d.ArmorBonus *= 1f + m.Amount; break;
                    case Stat.Cap: d.CapBonus *= 1f + m.Amount; break;
                    case Stat.Track: d.Tracking *= 1f + m.Amount; break;
                }
            }

            // Name and describe the piece by what it turned out to be.
            if (labels.Count == 0)
            {
                d.Name = b.Name + " (Standard)";
                d.Desc = b.Desc + "  Factory-standard build — no modifiers.";
            }
            else
            {
                d.Name = labels[0] + " " + b.Name;
                d.Desc = b.Desc + "  " + GameData.RarityNames[rarity] + " build ("
                    + labels.Count + " modifier" + (labels.Count == 1 ? "" : "s") + "): "
                    + string.Join(", ", labels.ToArray()) + ".";
            }
            // Better builds are worth more when sold on.
            d.Price = (long)(b.Price * (1f + 0.35f * labels.Count));

            _cache[id] = d;
            return d;
        }

        static readonly float[] Repeat = { 1f, 0.55f, 0.35f, 0.25f, 0.20f };

        static float Falloff(int repeatIndex)
            => Repeat[Mathf.Min(repeatIndex, Repeat.Length - 1)];

        /// <summary>Pick `want` distinct eligible modifiers, deterministically.
        /// If the module has fewer eligible stats than the rarity allows, the
        /// remaining rolls stack onto stats already picked.</summary>
        static List<int> RollMods(string baseId, string hash, int want)
        {
            var picks = new List<int>();
            if (want <= 0) return picks;
            var b = GameData.Modules[baseId];
            var pool = Eligible(b);
            if (pool.Count == 0) return picks;

            var rng = Rng.Stream("modmods:" + baseId + ":" + hash);
            var left = new List<int>(pool);
            for (int i = 0; i < want; i++)
            {
                if (left.Count == 0) left = new List<int>(pool);  // allow a second layer
                int k = rng.Next(left.Count);
                picks.Add(left[k]);
                left.RemoveAt(k);
            }
            return picks;
        }

        /// <summary>Human-readable summary of what a print will produce.</summary>
        public static string Describe(Blueprint bp)
        {
            var b = GameData.Modules[bp.ModuleId];
            int mods = GameData.ModBpMods[bp.Rarity];
            return b.Name + " blueprint  [" + GameData.RarityNames[bp.Rarity] + ", "
                + bp.RunsLeft + " run" + (bp.RunsLeft == 1 ? "" : "s") + " left, "
                + (mods == 0 ? "base stats" : mods + " modifier" + (mods == 1 ? "" : "s"))
                + ", " + GameData.ModBpMatMult[bp.Rarity] + "x materials]";
        }

        // ---------- material cost ----------
        // Scaled off the module's shop price, then multiplied by rarity, so a
        // Pristine run really does cost five times a Common one.

        public static Dictionary<string, float> MaterialCost(Blueprint bp)
        {
            var b = GameData.Modules[bp.ModuleId];
            float mult = GameData.ModBpMatMult[bp.Rarity];
            float unit = b.Price / 1000f;   // ~1 m3 of feedstock per 1000 cr of value
            var cost = new Dictionary<string, float>
            {
                ["iron"] = Mathf.Round(unit * 6f * mult),
                ["aluminium"] = Mathf.Round(unit * 3.5f * mult),
                ["nickel"] = Mathf.Round(unit * 1.8f * mult),
                ["titanium"] = Mathf.Round(unit * 1.4f * mult),
            };
            // The finest builds need beryllium for their precision internals.
            if (bp.Rarity >= 2) cost["beryllium"] = Mathf.Round(unit * 0.6f * mult);
            // ...and exotic metal that exists nowhere but a deep-space anomaly.
            // This is the gate: no exploration, no top-tier gear.
            if (bp.Rarity == 2)
                cost["tantalum"] = Mathf.Max(1f, Mathf.Round(unit * 0.10f * mult));
            else if (bp.Rarity == 3)
            {
                cost["hafnium"] = Mathf.Max(1f, Mathf.Round(unit * 0.09f * mult));
                cost["rhenium"] = Mathf.Max(1f, Mathf.Round(unit * 0.045f * mult));
            }
            return cost;
        }

        public static long Fee(Blueprint bp)
            => (long)(GameData.Modules[bp.ModuleId].Price * 0.12f
                      * GameData.ModBpMatMult[bp.Rarity]);

        // ---------- loot ----------

        static string NewHash()
        {
            var s = "";
            for (int i = 0; i < 10; i++) s += Random.Range(0, 10);
            return s;
        }

        /// <summary>Roll a module blueprint. Tougher targets bias toward the
        /// rarer, fewer-run, better-outcome prints and toward Tech II bases.</summary>
        /// <summary>Print blueprint for a wreck. The pirate's class fixes the
        /// print's rarity — that is how "an equivalent-class module" is
        /// expressed, since module prints have no class of their own — while how
        /// tough the target was still decides Tech I versus Tech II.</summary>
        public static Blueprint RollBlueprint(string npcId, int rarity)
        {
            float pT2;
            switch (npcId)
            {
                case "convoyhauler": pT2 = 0.55f; break;
                case "overlord": pT2 = 0.40f; break;
                case "marauder": pT2 = 0.22f; break;
                default: pT2 = 0.08f; break;
            }

            // Pick a family, then a tier within it.
            float fr = Random.value;
            var family = fr < 0.42f ? Hardpoint : fr < 0.71f ? MidSlot : LowSlot;
            bool t2 = Random.value < pT2;
            var choices = new List<string>();
            foreach (var id in family)
                if (id.EndsWith("2") == t2) choices.Add(id);
            if (choices.Count == 0) choices.AddRange(family);
            string baseId = choices[Random.Range(0, choices.Count)];

            rarity = Mathf.Clamp(rarity, 0, GameData.ModBpRuns.Length - 1);
            return new Blueprint
            {
                Hash = NewHash(),
                TypeId = "mod",
                ModuleId = baseId,
                Class = 0,
                Rarity = rarity,
                RunsLeft = GameData.ModBpRuns[rarity],
            };
        }
    }
}
