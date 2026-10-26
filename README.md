# PKRuneSagePlus (Suikoden I & II Rune Sage Enhancements)

A **BepInEx 6 (IL2CPP)** mod for **Suikoden I & II HD Remaster** (Steam) that enhances Rune Sage and Rune Shop mechanics across both games: transforming Jeane into a full-fledged Castle Rune Master shop in Suikoden I, and unlocking the endgame Sajah Village Rare Rune Shop early in Suikoden II.

---

## Overview & Background: Why This Mod Is Essential

Runes represent the core magic and combat customization system in the Suikoden series. However, in both *Suikoden I* and *Suikoden II*, vanilla game design oversights and structural limitations severely restrict rune availability, creating unnecessary backtracking and locking the series' most iconic magic runes behind post-game points of no return.

### 1. Suikoden I: Castle Rune Master (Jeane) Shop Expansion
- **The Backtracking Dilemma**: In vanilla Suikoden I, Rune Mistress Jeane at Toran Castle only provides two options: **Attach** and **Remove** runes. Unlike standard town rune shops, she does not sell a single rune crystal.
- **Constant Party Disruption**: With over 75 playable characters to equip and experiment with, recruiting a new companion often means wanting to slot elemental or combat runes immediately. Because Jeane sells nothing, players are forced to halt story progression, look up or memorize which distant town sells Fire, Water, Wind, Lightning, or Earth crystals, hike or teleport across the world map, buy the crystals from town merchants, and then hike back to Toran Castle just to have Jeane attach them.
- **Scattered Specialty Runes**: Specialty runes (Killer, Champion, Clone, Spark, Gale, Haziness) are scattered across far-flung settlements (Warriors' Village, Qlon Temple, Antei, Kirov) or locked away behind late-game sidequests.
- **How PKRuneSagePlus Solves This**:
  - Re-indexes Jeane's Toran Castle menu to a native 4-choice layout: `[0] Attach`, `[1] Remove`, `[2] Buy`, and `[3] Exit`.
  - Turns Jeane into the castle's **Chandler of Runes**: her shop dynamically offers base elemental rune crystals and progressively scales with **Toran Castle's expansion tiers** (Levels 1 through 4), unlocking advanced combat, utility, and high-tier elemental runes as your army expands.
  - Selecting "Buy" seamlessly invokes the native shop purchasing flow (`S1::h_omise_c`), complete with item icons, prices, inventory counts, and authentic sound effects.

---

### 2. Suikoden II: Sajah Village Rare Rune Shop Early Unlock
- **The Point-of-No-Return Lockout**: Sajah Village is the final settlement accessible in Highland Kingdom territory near the border during the late game of Suikoden II. Its local item shop (`vj17`) contains a dedicated rare items vendor who sells the most powerful and coveted magic runes and equipment in the entire game:
  - **High-Tier Elemental Runes**: Rage Rune, Thunder Rune, Flowing Rune, Mother Earth Rune, and Cyclone Rune.
  - **Endgame Gear & Consumables**: Master-grade armor, shields, and items.
- **The Vanilla Developer Oversight**: In vanilla Suikoden II, the rare vendor sprite is hidden (`disp = 0`) and the counter interaction trigger is deactivated behind **Story Flag 143** (defeat of the final boss / Beast Rune). Defeating the Beast Rune immediately forces the player into the final story sequence and ending cutscenes, with no free-roam travel permitted afterwards. Consequently, players can never actually purchase and enjoy these legendary runes when building their dream teams for the final battles!
- **How PKRuneSagePlus Solves This**:
  - Utilizes safe in-memory asset byte-patching on map initialization (`vj17.json` via `MACHICON.EventInit`).
  - Restores vendor sprite visibility (`disp = 1`), activates the counter interaction trigger, and neutralizes the room re-hide script check.
  - Bypasses Flag 143 in the shop dialogue scripts so players can browse and purchase all rare runes and items the very moment they arrive in Sajah Village.
  - Fully immune to official game updates, requiring zero archive extraction or repacking.

---

## Features

### Suikoden I: Castle Rune Master (Jeane) Shop
- **Native 4-Choice Menu**: Dynamically adjusts window geometry and text rows to present:
  - `[0] Attach`
  - `[1] Remove`
  - `[2] Buy`
  - `[3] Exit`
- **Progressive Castle Catalog**:
  - **Level 1 (1–29 Stars)**: Fire, Water, Wind, Lightning, Earth, Fortune, and Prosperity crystals available immediately upon founding the castle.
  - **Level 2 (30–59 Stars)**: Expands to advanced combat and utility runes (Resurrection, Killer, Spark, Counter, Clone, Double-Jab, Gale, Haziness, Phero).
  - **Level 3 (60–89 Stars)**: Unlocks devastating ultimate magic runes (Rage, Flowing, Cyclone, Mother Earth, Thunder).
  - **Level 4 (90–108 Stars)**: Unlocks master runes (Sunbeam, Champion's, Turtle).
- **Native Input & Audio**:
  - Full analog stick, D-Pad, and keyboard navigation with proper boundary clamping.
  - Authentic Suikoden sound effects for cursor movement, confirmation, and cancellation.
  - Smooth transitions between Jeane's command window and the purchase catalog.

### Suikoden II: Sajah Village Rare Shop
- **Early Rare Shop Access**: Unlocks the hidden vendor and counter trigger in Sajah Village (`vj17`) prior to defeating the Beast Rune.
- **Legendary Magic Runes Available**: Freely purchase Rage, Thunder, Flowing, Mother Earth, and Cyclone Runes to prepare for the endgame.
- **Safe In-Memory Byte-Patching**: Modifies `vj17.json` deserialized event data directly in memory during `MACHICON.EventInit`, keeping original game files 100% pristine.

---

## Suikoden I: Jeane's Castle Rune Catalog & Progression

Jeane's inventory scales dynamically with **Toran Castle Expansion (Recruited Stars of Destiny)** across 4 distinct tiers. Native pricing, icons, descriptions, and purchase dialogues are driven automatically by the game engine's internal item tables (`h_omise_c`).

### Progression by Toran Castle Expansion Tier

As Tir recruits more of the 108 Stars of Destiny, Toran Castle grows in size and prestige, unlocking higher tiers of combat, utility, and advanced magic runes in Jeane's shop:

| Castle Level | Recruits Milestone | Newly Unlocked Rune Crystals (Sealed Orbs) | Primary In-Game Effects / Role |
| :--- | :--- | :--- | :--- |
| **Level 1** | Castle Founded (Prologue / 1–29 Stars) | **Fire Orb**<br>**Water Orb**<br>**Wind Orb**<br>**Lightning Orb**<br>**Earth Orb**<br>**Fortune Orb**<br>**Prosperity Orb** | • Baseline tier-1 elemental magic.<br>• **Fortune**: Doubles EXP earned by bearer in combat.<br>• **Prosperity**: Doubles Potch awarded from battle victories. |
| **Level 2** | Castle Expansion 1 (30–59 Stars) | **Resurrection Orb**<br>**Killer Orb**<br>**Spark Orb**<br>**Counter Orb**<br>**Clone Orb**<br>**Double-Jab Orb**<br>**Gale Orb**<br>**Haziness Orb**<br>**Phero (Pheromone) Orb** | • **Resurrection**: High-tier holy healing & revive spells.<br>• **Killer**: Doubles bearer's critical strike probability.<br>• **Spark**: Forces party turn order to follow bearer's speed.<br>• **Counter**: Drastically boosts physical counter-attack rate.<br>• **Clone**: Massive 3x damage single-target attack.<br>• **Double-Jab**: Chance to perform two consecutive physical hits.<br>• **Gale**: Doubles bearer combat agility.<br>• **Haziness**: Increases bearer physical evasion rate.<br>• **Phero**: Opposite-gender party member shields bearer from damage. |
| **Level 3** | Castle Expansion 2 (60–89 Stars) | **Rage Orb**<br>**Flowing Orb**<br>**Cyclone Orb**<br>**Mother Earth Orb**<br>**Thunder Orb** | • **Advanced Tier-4 High Magic Runes**.<br>• Unlocks devastating ultimate group-clearing spells and master-level recovery magic (Firestorm, Mother Earth, Shining Wind, Earthquake, Heavenly Thunder). |
| **Level 4** | Castle Fully Built (90–108 Stars) | **Sunbeam Orb**<br>**Champion's Orb**<br>**Turtle Orb** | • **Master Utility & Quality of Life**.<br>• **Sunbeam**: Continuously regenerates HP with every step taken on maps.<br>• **Champion's**: Completely suppresses random wandering encounters with weak monster mobs.<br>• **Turtle**: Bestows permanent immunity against all status ailments (Sleep, Silence, Poison, Unbalance). |

---

## Requirements

1. **Suikoden I & II HD Remaster** (Steam edition, 64-bit Windows)
2. **BepInEx 6.0 (Unity IL2CPP x64)**
   - Download **[BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip](https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip)**.

---

## Installation Guide (Steam)

### 1. Locate Game Root Directory
In your Steam client:
1. Open your **Library** and right-click **Suikoden I & II HD Remaster**.
2. Select **Manage** → **Browse local files**.
3. A file explorer window will open at your game's root directory:
   ```text
   ...\steamapps\common\Suikoden I and II HD Remaster\
   ```
   *(This folder contains `Suikoden I and II HD Remaster.exe` and game files).*

---

### 2. Install BepInEx 6 (If not already installed)
1. Download **[BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip](https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip)**.
2. Extract the contents of the ZIP archive directly into your **`Suikoden I and II HD Remaster`** root folder.
3. You should see `doorstop_config.ini`, `winhttp.dll` (or `version.dll`), and the `BepInEx/` directory alongside the game executable.
4. **Launch the game once via Steam**, then quit to desktop.
   - This generates necessary IL2CPP interop assemblies and configuration directories.

---

### 3. Install PKRuneSagePlus
The release package is bundled with the complete directory structure for instant drag-and-drop installation.

1. Download **`PKRuneSagePlus-vX.X.X.zip`** from [Releases](https://github.com/faospark/PKRuneSagePlus/releases) (or grab `PKRuneSagePlus.dll`).
2. Open the ZIP archive. You will see a `BepInEx` folder.
3. **Drag and drop the `BepInEx` folder directly into your game's root directory**:
   ```text
   Suikoden I and II HD Remaster/
   ├── BepInEx/
   │   ├── config/
   │   │   └── faospark.pkrunesageplus.cfg (created automatically on first run)
   │   └── plugins/
   │       └── PKRuneSagePlus.dll
   ├── doorstop_config.ini
   └── Suikoden I and II HD Remaster.exe
   ```
4. If prompted by Windows, choose **Replace the files in the destination**.

---

### 4. Verify Installation
1. Start the game through Steam.
2. Check `BepInEx/LogOutput.log` (or console if enabled). You will see:
   ```text
   [Info   :   BepInEx] Loading [PK Rune Sage Plus 1.0.0]
   [Info   :PK Rune Sage Plus] Loading PKRuneSagePlus (Suikoden I & II Rune Sage Enhancements)...
   [Info   :PK Rune Sage Plus] Applying Suikoden 1 Jeane Rune Shop expansion patch...
   [Info   :PK Rune Sage Plus] Applying Suikoden 2 Sajah Village Rare Shop Early Unlock patch...
   [Info   :PK Rune Sage Plus] PKRuneSagePlus loaded successfully!
   ```
3. In Suikoden 1, speak to Jeane at Toran Castle to view the expanded `[0] Attach`, `[1] Remove`, `[2] Buy`, `[3] Exit` menu.
4. In Suikoden 2, enter Sajah Village item shop (`vj17`) to find the rare item vendor active and ready to trade!

---

## Configuration Options

Edit `BepInEx/config/faospark.pkrunesageplus.cfg` with Notepad or any text editor:

```ini
[Suikoden 1 Rune Sage]

## Expands Jeane's Castle Rune Master menu with a 'Buy' option (Attach, Remove, Buy, Exit). She acts as the Chandler of Runes, progressively selling Rune Crystals from town rune shops visited across the world.
# Setting type: Boolean
# Default value: true
EnableS1JeaneRuneShop = true

[Suikoden 2 Rune Sage]

## Unlocks the hidden rare item vendor and counter trigger inside the Sajah Village item shop before defeating the Beast Rune, allowing late-game access to rare Magic Runes (Rage, Thunder, Flowing, Mother Earth, Cyclone).
# Setting type: Boolean
# Default value: true
UnlockSajahRareShopEarly = true
```

---

## Building from Source

Prerequisites:
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or newer
- BepInEx 6.x IL2CPP installed in your game directory

To compile:
```bash
dotnet build -c Release
```

---

## Community & Support

Join the **Moogles & Mods** Discord community for updates, mod releases, technical support, and discussions:
- **Discord**: [Moogles & Mods Discord](https://discord.gg/bSnpVBV)

---

## Credits & Acknowledgments

- **faospark**: Reverse engineering, IL2CPP in-memory byte-patching implementation (`MACHICON.EventInit`), dynamic shop flow reconstruction (`h_omise_c` & `h_monsyo_c`), and progressive world rune catalog design.
- **Moogles & Mods Discord Community**: For testing, feedback, research, collaboration, and keeping the Suikoden modding scene thriving.
- **Framework**: [BepInEx](https://github.com/BepInEx/BepInEx) & [HarmonyX](https://github.com/BepInEx/HarmonyX).
- **Konami**: For creating the legendary Suikoden series.

