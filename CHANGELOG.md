# Changelog - PKRuneSagePlus

All notable changes to PKRuneSagePlus will be documented in this file.

---

## [1.0.1] - 2026-11-03

### Added
- **Elemental Weapon Pieces at Jeane's Castle Shop (S1)**:
  - Added all 5 socketable elemental shards to Jeane's shop inventory at Castle Level 1: **Fire Piece**, **Water Piece**, **Wind Piece**, **Lightning Piece**, and **Earth Piece** in [S1JeaneRuneShopPatch.cs](Patches/S1JeaneRuneShopPatch.cs).
  - Allows players to acquire weapon enchantment shards directly for socketing at Toran Castle Blacksmiths (Maas, Meese, Mose, Moose) without relying on random monster drops.

### Fixed
- **Canonical Item ID Mapping & Inventory Leakage (S1)**:
  - Fixed rune crystal item IDs in [S1JeaneRuneShopPatch.cs](Patches/S1JeaneRuneShopPatch.cs), eliminating accidental yellow flower seed item appearances.
  - Corrected Mother Earth and Thunder sealed orb item IDs, eliminating Old Book item leakage.

---

## [1.0.0] - 2026-10-26

### Added
- **Suikoden I: Castle Rune Master (Jeane) Shop Expansion ([S1JeaneRuneShopPatch.cs](Patches/S1JeaneRuneShopPatch.cs))**:
  - Re-indexed Jeane's Toran Castle menu to a native 4-choice layout: `[0] Attach`, `[1] Remove`, `[2] Buy`, and `[3] Exit`.
  - Dynamically scales Jeane's inventory with **Toran Castle expansion tiers**:
    - **Level 1 (1–29 Stars)**: Fire, Water, Wind, Lightning, Earth, Fortune, and Prosperity crystals.
    - **Level 2 (30–59 Stars)**: Advanced combat/utility runes (Resurrection, Killer, Spark, Counter, Clone, Double-Jab, Gale, Haziness, Phero).
    - **Level 3 (60–89 Stars)**: High-tier magic runes (Rage, Flowing, Cyclone, Mother Earth, Thunder).
    - **Level 4 (90–108 Stars)**: Master runes (Sunbeam, Champion's, Turtle).
  - Native purchasing flow integration through `S1::h_omise_c` with authentic pricing, item icons, and sound effects.
- **Suikoden II: Sajah Village Rare Rune Shop Early Unlock ([SajahShopUnlockPatch.cs](Patches/SajahShopUnlockPatch.cs))**:
  - Resolves the vanilla point-of-no-return lockout where Sajah Village's rare rune vendor is gated behind Story Flag 143 (Beast Rune defeat).
  - Performs 100% in-memory bytecode patching of `vj17.json` during `MACHICON.EventInit`.
  - Restores rare vendor sprite visibility (`disp = 1`), activates counter interaction triggers, and neutralizes room re-hide script checks so players can purchase high-tier runes (Rage, Thunder, Flowing, Mother Earth, Cyclone) for the final battle.
