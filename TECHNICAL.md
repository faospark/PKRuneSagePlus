# PKRuneSagePlus — Technical Reference

Technical implementation notes, rune catalog, and configuration reference for `PKRuneSagePlus` (`v1.1.0`).

---

## 1. Implementation Overview

| Patch | Game | Notes |
|---|---|---|
| `S1JeaneRuneShopPatch.cs` | S1 | Re-indexes Jeane's Toran Castle command window (`Attach`, `Remove`, `Buy`, `Exit`) and hooks `S1::h_omise_c` to populate a 4-tier progressive rune shop scaled to Toran Castle expansion. |
| `S1QuickRunePatch.cs` | S1 | Polls Right Stick Down / `[F7]` on the field with state validation (avoiding battles, loading screens, and cutscenes) to launch Jeane's rune shop remotely. |
| `SajahShopUnlockPatch.cs` | S2 | Patches deserialized `vj17.json` event data in memory during `MACHICON.EventInit`, restoring the rare vendor sprite (`disp = 1`), activating the counter trigger, and bypassing Story Flag 143 checks. |

---

## 2. Suikoden I: Jeane's Castle Rune Catalog (Levels 1–4)

| Castle Level | Recruits | Newly Unlocked Rune Crystals / Pieces | Effects / Role |
|---|---|---|---|
| **Level 1** | 1–29 Stars | Fire, Water, Wind, Lightning, Earth, Fortune, Prosperity Orbs; Fire, Water, Wind, Lightning, Earth Pieces | Tier-1 elemental magic, 2x EXP (Fortune), 2x Potch (Prosperity), and Blacksmith weapon shards. |
| **Level 2** | 30–59 Stars | Resurrection, Killer, Spark, Counter, Clone, Double-Jab, Gale, Haziness, Phero Orbs | Holy healing/revive, crit boost, turn-order sync, counter rate, 3x attack, double hit, agility/evasion, and guard. |
| **Level 3** | 60–89 Stars | Rage, Flowing, Cyclone, Mother Earth, Thunder Orbs | Tier-4 ultimate elemental and recovery magic. |
| **Level 4** | 90–108 Stars | Sunbeam, Champion's, Turtle Orbs | Step HP regen, weak-encounter prevention, and status immunity. |

---

## 3. Configuration Reference (`BepInEx/config/faospark.pkrunesageplus.cfg`)

```ini
[Suikoden 1 Rune Sage]
EnableS1JeaneRuneShop = true             # Add 'Buy' option to Jeane's Toran Castle menu (Levels 1-4)
EnableS1QuickRuneMaster = false          # Open Jeane's shop anywhere via Right Stick Down or [F7]
S1QuickRuneMasterRequireJeane = false    # Require Jeane to be recruited in Antei for remote access

[Suikoden 2 Rune Sage]
UnlockSajahRareShopEarly = true          # Unlock Sajah Village (vj17) rare vendor before Beast Rune
```
