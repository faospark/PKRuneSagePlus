# P.K. - Rune Sage Plus

**By [faospark](https://github.com/faospark)**

A **BepInEx 6 (IL2CPP)** mod for **Suikoden I & II HD Remaster** that turns Jeane into a full-fledged Castle Rune Master shop in Suikoden I and unlocks the endgame Sajah Village Rare Rune Shop early in Suikoden II.

## Features

### Suikoden I
- **Jeane's Castle Rune Shop (`EnableS1JeaneRuneShop`)**: Adds a **Buy** option (`Attach`, `Remove`, `Buy`, `Exit`) to Jeane at Toran Castle, progressively unlocking elemental orbs, weapon pieces, combat/utility runes, and ultimate magic runes across Castle Levels 1–4.
- **Remote Castle Rune Master (`EnableS1QuickRuneMaster`)**: Press **Right Stick Down** (or `[F7]` on keyboard) to open Jeane's rune shop from anywhere on the field (optionally gated by `S1QuickRuneMasterRequireJeane`).

### Suikoden II
- **Sajah Village Early Rare Shop (`UnlockSajahRareShopEarly`)**: Unlocks the hidden rare item & rune vendor in Sajah Village (`vj17`) before defeating the Beast Rune, giving early access to **Rage**, **Thunder**, **Flowing**, **Mother Earth**, and **Cyclone** Runes plus endgame gear.

> Looking for the full 4-tier rune catalog, technical hooks, and `.cfg` reference? See [TECHNICAL.md](TECHNICAL.md).

## Configuration

Settings generate in `BepInEx/config/faospark.pkrunesageplus.cfg` on launch (`[Suikoden 1 Rune Sage]` and `[Suikoden 2 Rune Sage]`).

## Requirements

- **BepInEx 6.0.0-pre.2 IL2CPP**
- **Suikoden I & II HD Remaster**
- **Recommended:** [Suikoden Fix](https://github.com/d3xMachina/SuikodenFix) by d3xMachina

## Installation

1. Install [BepInEx 6.0 (Unity IL2CPP x64)](https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip) into your game root directory.
2. Place `PKRuneSagePlus.dll` into `BepInEx/plugins/`.
3. Launch the game once to generate `BepInEx/config/faospark.pkrunesageplus.cfg`.

## Credits & License

- **[faospark](https://github.com/faospark)** — Primary Author & Lead Researcher
- **[d3xMachina](https://github.com/d3xMachina/Suikoden-Fix)** — Creator of Suikoden-Fix & BepInEx IL2CPP foundation
- **[asilverthorn](https://github.com/asilverthorn/suikoden_ref)** — Reference tables and internal IDs
- **Mod Suite:** [Project Kyaro](https://www.nexusmods.com/suikoden1and2hdremaster/mods/6)
- **Framework:** [BepInEx](https://github.com/BepInEx/BepInEx) & [HarmonyX](https://github.com/BepInEx/HarmonyX)

Distributed under the [MIT License](LICENSE).
