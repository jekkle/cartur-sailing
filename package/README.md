# Cartur's Sailing

A Sailing skill for Valheim. Steer a ship under sail to level it, and the better a sailor you are,
the faster your ships go, the tougher the ones you build, and the more of the map you see from the
deck.

Inspired by **Smoothbrain's [Sailing](https://thunderstore.io/c/valheim/p/Smoothbrain/Sailing/)**
(deprecated in 2026) - credit to Smoothbrain for the idea. This is a separate mod, written from
scratch for Valheim 1.0; it shares no code with theirs.

## What it does

The game has no sailing skill, so this adds one, listed with your other skills. It rises while you
hold the helm of a moving ship under half or full sail - faster at full sail.

| At Sailing 100 | You get |
| --- | --- |
| Speed | Ships you steer sail half again as fast. It's the helmsman's level that counts. |
| Hull | Ships you build have double hull health. It's the builder's level when the ship was built. |
| Exploring | The map reveals twice as far around you while you're aboard. |

Every bonus scales with level - at 50 you get half of it.

**Hold Left Shift while using a ship's ladder** to push the ship away from you instead of climbing
aboard - for getting a ship off a beach. It only works when nobody is at the helm.

## Settings

`BepInEx/config/com.jekkle.valheim.cartursailing.cfg`

| Setting | Default | What it does |
| --- | --- | --- |
| `SpeedBonus` | 0.5 | Extra speed at level 100. 0.5 = half again as fast, 0 = vanilla. |
| `HealthBonus` | 1 | Extra hull health for ships built at level 100. 1 = double, 0 = vanilla. |
| `ExploreBonus` | 1 | Extra map reveal radius aboard at level 100. 1 = twice as far, 0 = vanilla. |
| `ShipSpeeds` | empty | Speed per ship by prefab name, on top of everything else, e.g. `Raft=1.2, Karve=1, VikingShip=0.9`. Works for ships from other mods too. |
| `HalfSailLevel` | 0 | Level needed for half sail. Below it, half sail catches no wind and the ship coasts - row instead. 0 = no lock. |
| `FullSailLevel` | 0 | Level needed for full sail. Below it, full sail works as half. 0 = no lock. |
| `NudgeSpeed` | 2 | How hard the ladder nudge pushes, in metres per second. |
| `EnforceVersion` | true | On a server, refuse clients on a different version. |
| `NudgeKey` | Left Shift | Hold while using a ladder to nudge the ship. |

Install it on the server too and its numbers apply to everyone. The key stays your own.

Ships built before you installed it keep vanilla hull health - the builder's level is recorded when
a ship is placed.

## Install

r2modman or Thunderstore Mod Manager. Needs BepInEx and Jotunn (which registers the new skill).

---

*Free, and always will be. If it improved your game you can [tip me on Patreon](https://www.patreon.com/c/cartur).*

**[Discord](https://discord.gg/nd5RqpwNkz)** — bug reports, install help, and mod requests.
Bug reports get their own thread so nothing is lost in a chat scroll, and requests are voted on.

**More from Cartur:**
[Map Pins](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Map_Pins/) ·
[HD Blood](https://thunderstore.io/c/valheim/p/Cartur/Carturs_HD_Blood/) ·
[Compass and Clock](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Compass_and_Clock/) ·
[Safe Stamina](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Safe_Stamina/) ·
[Follow Command](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Follow_Command/) ·
[Flooring](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Flooring/) ·
[UI](https://thunderstore.io/c/valheim/p/Cartur/Carturs_UI_HUD/) ·
[Waste Management](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Waste_Management/) ·
[Feeding Trough](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Feeding_Trough/) ·
[Build Camera](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Build_Camera/) ·
[Combat Text](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Combat_Text/)
