# Spacegame

An EVE Online-inspired space game built in Unity (3D). Fly a ship through a
small constellation of star systems joined by stargates; mine asteroids, trade
on station markets, fight pirates, fit modules, and train skills in real time.

## Requirements

- **Unity 6 (6000.x)** via Unity Hub

## Which build am I running?

The version is printed in the **middle of the HUD top bar** and written to the
**message log** every time you press Play (see `BuildInfo.cs`). Double-clicking
`update.bat` prints the version it just pulled, so the two should match.

If the game shows an **older** version than `update.bat` reported, the pull
landed but Unity has not recompiled. Click into the editor and watch for the
spinner in the bottom right; if nothing happens, open **Window > General >
Console** and look for red errors — a compile error makes Unity keep running
the last assembly that built successfully, so the game silently stays on the
previous version.

If `update.bat` says **ALREADY UP TO DATE** but you expected changes, the
folder Unity has open is not the folder that got updated. Right-click any
script in Unity's Project window, choose *Show in Explorer*, and check the path
matches the folder `update.bat` printed. Run `update.bat force` to discard
local edits and match GitHub exactly.

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
  Asteroids are named for what is actually in them, so the rock in the
  overview tells you what it mills out into — every type is a real mineral
  and every yield matches its real composition:

  | Asteroid | Composition | Refines into | cr/m3 | Found in |
  | --- | --- | --- | --- | --- |
  | **Taenite** | Fe-Ni meteoric alloy | Iron + **Nickel** | 12 | Solara, Verdant |
  | **Anorthite** | CaAl₂Si₂O₈ feldspar | Aluminium + Iron | 18 | Solara → Krios |
  | **Armalcolite** | (Mg,Fe)Ti₂O₅ | Titanium + Iron | 27 | Verdant → Nadir |
  | **Rutile** | TiO₂ | Titanium | 42 | Krios → Abyss |
  | **Beryl** | Be₃Al₂Si₆O₁₈ | Beryllium + Aluminium | 65 | Nadir, Abyss |

  Taenite is the iron-nickel alloy that metallic meteorites are made of,
  Anorthite is the aluminium feldspar of the lunar highlands, and Armalcolite
  was first identified in the Apollo 11 samples. **Nickel** comes only out of
  Taenite, and every hull and nearly every module needs nickel superalloy — so
  the safest belts in the game never stop being worth mining.
- **Refine** ore *and recovered scrap* into minerals
  (Iron/Nickel/Aluminium/Titanium/Beryllium) at any station — yield starts at 66%
  and grows with the Refining skill, so refining out-earns raw selling once
  you're trained. Scrap is compacted hull, so it mills out into several
  times its own volume in metal (Class 3 gives 8.4 m3 of metal per m3), and
  it is the only route to **titanium and beryllium that doesn't involve
  mining low-sec belts** — one convoy kill covers the exotic metals for a
  hull, leaving you to top up bulk iron from ore. Anything that won't fit
  your hold is banked in the station's storage bay.
- **Sell** at stations — each station has stable price personalities, so
  hauling ore and minerals between systems is a real career. Stations buy
  raw materials and sell hulls, refining, repairs and licences. **No station
  sells a single fittable module**, at any price.
- **Fight** pirates for bounties. Lower security means nastier spawns:
  Solara (1.0) is safe; Abyss (0.0) is overlord country. Turrets have
  tracking speeds: orbit fast and close to make big slow guns miss you
  (the target panel shows your expected hit quality).
- **Salvage** wrecks — destroyed pirates never carry fittable gear. Their
  hulks yield **graded scrap** (Class 1/2/3, priced 45/130/380 cr per m3)
  and, rarely, a blueprint chip. Tougher targets leave a richer grade and
  more of it, so a full hold of Class 3 scrap is worth the trip.
- **Everything you fit is found or built.** Credits cannot buy gear anywhere
  in the game, so there are exactly two routes to a module: **drifting
  caches** turned up by a sensor sweep, which hold basic Tech I pieces
  (including a mining laser and a gun, so a wiped-out pilot always has a way
  back), and **module blueprints** off pirate wrecks, which cover the entire
  catalogue up to Tech II. A better gun is something you earn, not something
  you shop for.
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
- **Explore for exotics (Trail hulls)** — a Trail hull's Pathfinder Array
  sweeps for deep-space **anomalies**, and they are the *only* source of
  **Tantalum, Hafnium and Rhenium**. Nothing can be mined or refined into
  them, and the best blueprints will not run without them: Rare and Pristine
  module prints and every Class 3 hull need exotics.
  The catch is the clock. A field holds 3-5 sealed containers spread out
  across tens of km, and it is structurally unstable — 165s to 240s before it
  **detonates**, destroying everything left inside and shockwaving anyone
  within 26 km. Partway through the countdown the noise draws a **response
  fleet** that warps straight in on you (two rookies at Tier 1, an overlord
  and escorts at Tier 3). So it is a race: fly the route, crack the cans, get
  out. That is exactly what a fast Trail hull with a Salvage Collector II and
  an afterburner is for — the collector's cycle time is the difference between
  clearing a field and losing it. Richer tiers hide in lower security, so the
  best fields sit where the law does not.
- **Print gear from module blueprints** — pirate wrecks also drop blueprints
  for **Tech I and Tech II** hardpoint gear (**turrets, mining lasers**,
  claws, webs, disruptors, drones, sensors, collectors) and mid/low slot gear
  (boosters, afterburners, cargo, armour, capacitor) — the whole catalogue,
  since nothing is for sale. These run **opposite to ship blueprints**: a Common
  print gives **5 runs of plain, base-stat gear**, while a Pristine print is
  a **single run at 5x material cost that rolls 5 modifiers** onto the
  finished piece. Modifiers only touch stats the module actually uses — a
  Cavernous Cargo Expander II gains capacity, an Overclocked claw cycles
  faster — and each print names itself after what it rolled. Finished
  modules land in that station's storage bay.
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
- **Fit** your ship at stations from what you have recovered or printed:
  weapons, miners, shield boosters, afterburners, cargo/armor/cap passives.
- **Upgrade** hulls: Wasp → Prospector (miner) / Talon (fighter) →
  Mule (hauler) → Aurora (cruiser). These five are stocked everywhere.
- **Buy a real hull at a Shipyard** — three of the five stations hold
  production licences and keep finished **Class 1** hulls of the generated
  lines on the pad, so you don't need a blueprint to fly one. Solara Prime
  licences **Hive / Claw / Fin**, Krios Bastion **Fin / Talon / Scale**, and
  Nadir Freeport **Claw / Trail / Pack** — no yard holds all seven, so the
  hull you want may be a few jumps away. Each pad carries two distinct rolled
  bodies per line, listed with their traits so you can compare before signing.
  Verdant Refinery and the Outlaw Den have no yard at all.
  A licence costs money: pad prices run about **1.7x to 2.9x** what the same
  hull costs to build. Blueprints stay the cheap route to a hull, the *only*
  route to Class 2 and Class 3, and the only route to a body nobody else will
  ever fly. Yard stock is fixed and deterministic — "the Fennec on the pad at
  Nadir" is something you can plan a trip around.
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
