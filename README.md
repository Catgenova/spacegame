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

Use the **Overview** (right panel) to select things, then **Approach / Orbit /
Warp** from the target panel. Warp to an asteroid belt, select a rock, activate
your miner, fill your hold, warp back, **Dock**, and sell on the Market tab.

Selecting an asteroid or a pirate starts a **target lock** (EVE-style) — the
target panel shows lock progress, and mining lasers / weapons only fire on a
locked target within lock range (60 km). Pirates need a moment to lock you
too, so an early warp-out beats their first volley.

### The loop

- **Mine** ore in asteroid belts; richer, rarer ore lives in low-sec systems.
- **Sell** at stations — each station has stable price personalities, so
  hauling ore (or cheap modules) between systems is a real career.
- **Fight** pirates for bounties. Lower security means nastier spawns:
  Solara (1.0) is safe; Abyss (0.0) is overlord country.
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
  Core/       GameBootstrap (runtime entry), GameManager (central state),
              SaveSystem, CameraRig
  Data/       GameData (all static defs, code-defined), Rng
  Universe/   UniverseGenerator + system/celestial/asteroid data
  Economy/    Market (deterministic per-station prices)
  Missions/   Mission generation (bounty / mining / courier) per station agent
  Player/     PlayerState (credits/skills/fitting/cargo), ShipController
  World/      SystemView (spawns the scene), SpaceObject, NpcPirate,
              ShipVisuals (code-built hull/NPC models + starfield)
  UI/         HudUI (IMGUI prototype interface)
```

World scale: **1 Unity unit = 100 m**. The HUD/UI is a deliberate
programmer-art placeholder (IMGUI) so gameplay can iterate before art.
