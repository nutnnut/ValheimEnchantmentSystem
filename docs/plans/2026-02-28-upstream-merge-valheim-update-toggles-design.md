# Design: Upstream Merge, Valheim Update, and Randomization Toggles

**Date:** 2026-02-28
**Branch:** `nut`
**Upstream:** `kg/kg` (https://github.com/war3i4i/ValheimEnchantmentSystem)

---

## Context

This repository is a fork of the upstream Valheim Enchantment System mod. The upstream is no longer maintained; this fork is becoming the new primary. The fork diverges from upstream at commit `8df3526` ("new patch", Mar 2025).

**Fork-unique features (must be preserved):**
- Randomized stat selection and value system (`GetRandomizedMultiplier`, `RandomizeAndSaveEnchantedItem`)
- Multiple enchantment effects per item (`EnchantedItem.effects` list)
- Pity system for re-rolls
- Extended stats: Etir, Stagger, StaminaUsage, AttackSpeed, SkillLevel, MovementSpeed
- VFX on armor (default `true`)
- Different default config values

**Upstream features to integrate (6 commits ahead):**
- `ccf6ef7` — SettingsUI `save()` vanilla UI bug fix
- `d8e38e6` — `BlessedScrollsPreventBreak` config, `BlessedScrollsAdditionalChance` config, enchantment skill XP gain per scroll tier, improved bless-toggle UI logic
- `ae30a80` — Bug fix: regular scrolls must not add enchant chance bonus
- `70ec251` — Merge commit (combines above)

**Asset policy:** Unity/UI assets (`Resources/kg_enchantment`, bundled DLLs in `ThunderstorePackage/plugins/`) come from upstream. Game logic takes the fork's version.

---

## Task 1: Merge Upstream (`kg/kg`)

### Approach
Run `git merge kg/kg` and resolve conflicts file-by-file, with the following rules:

| File | Resolution rule |
|---|---|
| `Configs/SyncedData.cs` | Keep fork's randomization code; add upstream's `BlessedScrollsPreventBreak` and `BlessedScrollsAdditionalChance` config entries |
| `Enchantment_Core.cs` | Keep fork's `Enchant()` signature; integrate upstream's bless-prevent-break logic and regular-scroll bug fix |
| `UI/MainUI.cs` | Keep fork's UI base; integrate upstream's try-catch in `UseBless_ButtonClick`, chance label update, and skill XP gain code |
| `ValheimEnchantmentSystem.cs` | Keep fork's plugin name/version; ignore upstream's rename |
| `UI/SettingsUI.cs` | Take upstream's save() bug fix |
| `translations/English.yml` | Merge both (upstream adds 1 new key) |
| `Resources/kg_enchantment` | Take upstream's version (UI asset bundle) |
| `ThunderstorePackage/plugins/` | Take upstream's versions |

### Conflict expectations
- `Enchantment_Core.cs`: `Enchant()` signature already matches between fork and upstream (`bool, bool, out string`); verify the `CheckRandom` call uses the new bless parameter correctly
- `UI/MainUI.cs`: Upstream adds ~30 lines of skill XP code in `StartButton_Click` and rewrites `UseBless_ButtonClick`; fork has minimal changes here

---

## Task 2: Valheim Version Update

### Approach
Copy updated game assemblies from the local Valheim installation at:
`D:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed\`

**Assemblies to update in `Libs/`:**
- `assembly_valheim.dll`
- `assembly_utils.dll`
- `assembly_guiutils.dll`

After updating, attempt a build and fix any API breakage (renamed methods, changed signatures, removed members). Valheim game updates typically change internal method signatures in patched classes — the Harmony patches are the most likely breakage points.

---

## Task 3: Randomization Toggle + Config Exposure

### New Config Entries (all server-synced, in `SyncedData.OnInit`)

```
[Randomization]
RandomizeStatSelection    = true    # 3a: pick random subset of stats
RandomizeStatValues       = true    # 3b: multiply each stat by random factor

BaseStatLines             = 2       # base number of stat lines
ExtraLineLevelThreshold   = 4       # every N levels adds +1 base line
ExtraLineChance           = 0.50    # per-roll probability of an extra line
PityMaxLineDrop           = 1       # max lines fewer than previous roll (pity floor)

MultiplierMin             = 0.50    # minimum multiplier step
MultiplierMax             = 1.00    # maximum base multiplier
MultiplierInterval        = 0.25    # step size for rounding multiplier
BonusMultiplierChance     = 0.10    # probability of exceptional roll
BonusMultiplier           = 2.00    # factor applied on exceptional roll
```

### Behavior Matrix

| RandomizeStatSelection | RandomizeStatValues | Result |
|---|---|---|
| true | true | Current behavior (random stats, random values) |
| true | false | Random stat subset, exact YAML values (×1.0) |
| false | true | All stats active, each with random multiplier |
| false | false | All stats at exact YAML values — deterministic |

### Implementation

Modify `SyncedData.GetRandomizedMultiplier()`:

1. **When `RandomizeStatSelection = false`**: Take all `possibleFields` (skip the `Take(lineCount)` selection step and line-count/pity logic)
2. **When `RandomizeStatValues = false`**: Set `floatMultiplier = 1.0f` (skip random range, skip bonus multiplier)
3. Replace all hardcoded numbers with reads from the new config entries when both toggles are enabled

The `bonusLineCount` parameter (passed from scroll-tier logic) is unaffected by these toggles — it remains an additive bonus to `BaseStatLines` when `RandomizeStatSelection = true`.

---

## Implementation Order

1. Merge upstream (`git merge kg/kg` + conflict resolution)
2. Update Valheim assemblies + fix build
3. Add toggle configs and expose hardcoded values

---

## Task IDs (assigned during planning)

- Task A: Merge upstream
- Task B: Update Valheim assemblies
- Task C: Add randomization toggles and expose config values
