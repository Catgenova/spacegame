# Spacegame

An EVE Online-inspired space game built in Unity (3D). Fly a ship through a
small constellation of star systems joined by stargates; mine asteroids, trade
on station markets, fight pirates, fit modules, and train skills in real time.

## Requirements

- **Unity 6 (6000.x)** via Unity Hub

## Getting started

1. Open Unity Hub → **Add** → **Add project from disk** → select this folder.
2. Open the project with a Unity 6 editor (first import takes a minute).
3. Open any scene (the default empty scene is fine — `File > New Scene` works too).
4. Press **Play**.

There is deliberately nothing to set up in the editor: `GameBootstrap` builds
the entire game at runtime — camera, lighting, universe, ships, and UI are all
created from code. No prefabs, no serialized assets, no scene wiring.

## How to play

You start docked at **Solara Prime** with 5,000 credits, a rookie **Wasp**
frigate, a mining laser, and a blaster.

| Input | Action |
| --- | --- |
| `W` / `S` | Throttle up / down |
| `A` / `D` | Turn |
| `X` | Full stop |
| Left click | Select object in space |
| Right drag | Orbit camera |
| Scroll | Zoom camera |
| `1`–`8` | Toggle fitted modules |
| `K` | Skill training window |
| `M` | Galaxy map (click a system to set a route) |
| `F9` | Mute audio |
| `F10` | Toggle UI Toolkit / legacy IMGUI HUD |

Use the **Overview** (right panel) to select things, then **Approach / Orbit /
Warp** from the target panel. Warp to an asteroid belt, select a rock, activate
your miner, fill your hold, warp back, **Dock**, and sell on the Market tab.

Selecting an asteroid or a pirate starts a **target lock** (EVE-style) — the
target panel shows lock progress, and mining lasers / weapons only fire on a
locked target within lock range (60 km). Pirates need a moment to lock you
too, so an early warp-out beats their first volley.

### The loop

- **Mine** ore in asteroid belts; richer, rarer ore lives in low-sec systems.
- **Refine** ore into minerals (Iron/Aluminium/Titanium/Beryllium) at any
  station — yield starts at 66% and grows with the Refining skill, so
  refined mineral hauling out-earns raw ore once you're trained.
- **Sell** at stations — each station has stable price personalities, so
  hauling ore, minerals, or cheap modules between systems is a real career.
- **Fight** pirates for bounties. Lower security means nastier spawns:
  Solara (1.0) is safe; Abyss (0.0) is overlord country. Turrets have
  tracking speeds: orbit fast and close to make big slow guns miss you
  (the target panel shows your expected hit quality).
- **Salvage** wrecks — destroyed pirates never carry fittable gear. Their
  hulks yield **graded scrap** (Class 1/2/3, priced 45/130/380 cr per m3)
  and, rarely, a blueprint chip. Tougher targets leave a richer grade and
  more of it, so a full hold of Class 3 scrap is worth the trip. Fittable
  modules come from the market or from **drifting caches** turned up by a
  sensor sweep.
- **Watch them run** — badly damaged pirates break off and warp out
  (overlords fight to the death), idle pirates roam between belts, and
  **pirate convoys** appear in low-sec: a fat hauler with a 60k bounty, a
  hold full of Class 3 scrap and a 60% blueprint chance, escorted by
  guards.
- **Build standing** with the Frontier Authority — every kill raises it.
  Tiers (Trusted / Honored / Legend) grant better mission pay and repair
  discounts, and Trusted unlocks **"The Abyss Job"**, a three-stage story
  arc that ends in a convoy hunt.
- **More contracts** — salvage recovery jobs, RUSH couriers with hard
  deadlines and 1.8× pay, and low-sec agents pay up to +50% on everything.
- **Hunt blueprints, build Hive ships** — pirate wrecks can carry blueprint
  chips (convoy haulers are the best source). Each blueprint holds a
  10-digit body hash that deterministically generates a one-of-a-kind
  **Hive-class scout**: stats, rolled traits, and a procedurally generated
  faceted wasp-metal mesh — angular gold/black hull, swept antennae, wing
  blades, folded legs. Rarity sets the run count (Common 1 → Pristine 5);
  manufacture runs at any station's **Industry** tab using refined
  minerals from your hold. When the runs are spent, that body is gone
  from the universe forever — and if you lose the ship, so is yours.
- **Web them** — Hive hulls carry a dedicated web slot for the Stasis
  Webifier (half target speed): pin fleeing pirates before they warp out.
  Their single hardpoint takes turrets only; wasps don't mine.
- **Navigate** with the galaxy map (`M`): click a system to plot a route;
  a route bar in space names the next gate and warps you to it one click
  at a time until you arrive.
- **Hear it** — every sound is synthesized in code (no audio files):
  engine hum pitched to your speed, blaster crack vs railgun thud
  (glancing hits sound weaker), mining chunks, shield zaps vs armor
  thumps, warp swell, lock chime, dock clunks, and payout arpeggios.
- **Store** anything at any station — each has an **unlimited storage bay**
  on the Storage tab. Deposit ore, minerals, scrap or blueprints and they
  wait there forever, but the bay is strictly local: you must fly back to
  that station to collect it. Salvaged and purchased modules land in the
  local bay too, so plan where you stage your gear.
- **Fit** your ship at stations: weapons, miners, shield boosters,
  afterburners, cargo/armor/cap passives.
- **Upgrade** hulls: Wasp → Prospector (miner) / Talon (fighter) →
  Mule (hauler) → Aurora (cruiser).
- **Train skills** passively (Mining, Gunnery, Engineering, Navigation,
  Trade) — pick one in the `K` menu; it trains in real time.
- **Run missions** from the station **Agent** tab: bounty hunts, ore
  requisitions, and courier runs (the package really occupies your hold).
  One active mission at a time; turn in at the agent for the reward.

Dying costs you your ship and cargo; you respawn docked at Solara Prime in a
loaner Wasp. Progress autosaves (PlayerPrefs) every 20 seconds and on every
dock/jump.

## The universe

```
Solara (1.0) ── Verdant (0.7) ── Krios (0.5) ── Nadir (0.3) ── Abyss (0.0)
                     └──────────────────────────────┘
```

## Project layout

```
Assets/Scripts/
  Ships/      HiveGenerator (type/class envelopes, hash -> stats/traits),
              HiveShipMesh (hash -> procedural wasp body mesh)
  Core/       GameBootstrap (runtime entry), GameManager (central state),
              SaveSystem, CameraRig
  Data/       GameData (all static defs, code-defined), Rng
  Universe/   UniverseGenerator + system/celestial/asteroid data
  Economy/    Market (deterministic per-station prices)
  Missions/   Mission generation (bounty / mining / courier) per station agent
  Player/     PlayerState (credits/skills/fitting/cargo), ShipController
  World/      SystemView (spawns the scene), SpaceObject, NpcPirate,
              ShipVisuals (code-built hull/NPC models + starfield)
  UI/         UitHud (primary UI Toolkit interface, built fully in code),
              HudUI (legacy IMGUI fallback), UiSwitcher (F10), UiSkin
```

World scale: **1 Unity unit = 100 m**.

The UI is UI Toolkit, generated entirely at runtime (PanelSettings and the
whole visual tree are created in code — no UXML/USS/theme assets). If it
fails to initialize on a given setup, the game automatically falls back to
the legacy IMGUI HUD, and `F10` switches between the two at any time.
