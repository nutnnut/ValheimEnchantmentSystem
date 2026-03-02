# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build

This is a BepInEx Harmony mod for Valheim (C# .NET 4.8). Build with MSBuild:

```
msbuild ValheimEnchantmentSystem.csproj /p:Configuration=Release
```

After a successful build, the post-build step automatically copies the DLL to `ThunderstorePackage/plugins/ChaosValheimEnchantmentSystem/`.

There are no automated tests. Verification requires running the mod in-game.

## Project Structure

- `ValheimEnchantmentSystem.cs` — BepInEx plugin entry point, config init
- `Enchantment_Core.cs` — `Enchanted` ItemData subclass, `EquipmentEffectCache`, `PlayerExtension` stat helpers
- `Enchantment_Enchanted_Item.cs` — `EnchantedItem` (level + effects list) and `EnchantmentEffect` (name + float multiplier)
- `Configs/SyncedData.cs` — `Stat_Data`, `Chance_Data`, all synced config values, YAML loading/watching, `GetRandomizedMultiplier`, `ApplyMultiplier`
- `Configs/Defaults.cs` — Default YAML content baked into code (weapons/armor stats, chances, colors, reqs)
- `EnchantmentEffects/` — One file per bonus type, each a Harmony patch
- `Managers/` — ServerSync, ItemManager, ItemDataManager (third-party helpers)

## Architecture: Two Stat Effect Systems

**Per-item system** (`en.Stats.fieldName`) — used for weapon damage, armor, and resistance bonuses:
- `Enchanted.Stats` property: calls `Stat_Data.ApplyMultiplier(enchantedItem)` using the stored per-item multiplier effects
- Result is cached as `cachedMultipliedStats` on the `Enchanted` instance; cleared only on re-enchant
- Used in: `ModifyDamage`, `ModifyArmorAndBlockPower`, `ModifyResistances`

**Per-player system** (`player.GetTotalEnchantedValue("field_name")`) — used for player-wide buffs:
- `GetEnchantedEffects(effectType)` iterates all equipped enchanted items, computes `effect.value * baseStatAtLevel`
- Results are cached per-player per-field in `EquipmentEffectCache`; cleared on equip/unequip
- Used in: `ModifyHealth`, `ModifyStamina`, `ModifyMovementSpeed`, `ModifyAttackSpeed`, `ModifyEtir`, `ModifySkillLevel`, `ModifyStagger`, `ModifyStaminaUsage`, `ModifyDurability`

## Randomized Stat Multiplier System

Each enchanted item stores a list of `EnchantmentEffect(name, floatMultiplier)` in JSON on the item's ZDO. The multiplier is a random value between 0.5–1.0 (with a 10% chance of 2×). During `Stats` computation, `ApplyMultiplier` multiplies each base config stat by its stored multiplier.

**Enum fields (resistance_*)** are a special case:
- Always stored with `floatMultiplier = 1.0f` (forced in `GetRandomizedMultiplier`)
- `ApplyMultiplier` checks `if (multiplier > 0)` — copies the base enum value if the effect was rolled; otherwise leaves it as `Normal`
- Resistance is binary: either applied at config value, or not applied

**Line count**: `2 + (level / 4)` base lines, random extras (50% chance each), clamped by pity system (`oldLineCount - 1` minimum on reroll).

## Config Files (YAML, server-synced)

| File | Purpose |
|------|---------|
| `EnchantmentStats_Weapons.yml` | Per-level base stats for weapons |
| `EnchantmentStats_Armor.yml` | Per-level base stats for armor |
| `EnchantmentChancesV2.yml` | Per-level success/destroy/reroll % |
| `EnchantmentColors.yml` | Per-level VFX color + variant |
| `EnchantmentReqs.yml` | Scroll requirements per item |
| `AdditionalOverrides_*` dirs | Item-specific overrides |

**Note**: Default configs have NO resistance values. Resistances require custom YAML entries with non-Normal `resistance_*` fields.

## Key Patterns

- `[VES_Autoload]` / `[VES_Autoload(Priority.First)]` — attribute-driven auto-init system; `OnInit()` called automatically
- `[ClientOnlyPatch]` — Harmony patches that should only run client-side
- `Enchanted` extends `ItemData` from the `ItemDataManager` library; accessed via `item.Data().Get<Enchanted>()`
- `cachedMultipliedStats` is **not** cleared on YAML config reload — players must re-equip/re-enchant to pick up config changes
