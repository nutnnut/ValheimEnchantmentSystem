# Upstream Merge, Valheim Update, and Randomization Toggles — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers-extended-cc:executing-plans to implement this plan task-by-task.

**Goal:** Merge 6 upstream commits into the fork, update Valheim game assemblies to the current version, and expose the randomization system (stat selection, stat values, and all hardcoded parameters) as server-synced config entries.

**Architecture:** Three sequential phases. Phase 1 merges upstream code changes while preserving fork-unique features (randomized stats, pity system, equipment cache, new effects). Phase 2 swaps stale game DLLs. Phase 3 adds BepInEx config entries in `SyncedData` and updates `GetRandomizedMultiplier` to read them. Everything is server-synced so multiplayer hosts control behavior.

**Tech Stack:** C# .NET 4.8, BepInEx, Harmony, YamlDotNet, Newtonsoft.Json, Valheim game assemblies

**Merge policy:** Fork's game logic takes priority in all conflicts. Unity/UI assets (`Resources/kg_enchantment`, `ThunderstorePackage/plugins/`) come from upstream. Never take upstream's changes that simplify away fork features.

---

## Phase 1 — Merge upstream

---

### Task 1: Run the merge

**Files:** none (git operation)

**Step 1: Start the merge**

```bash
git merge kg/kg
```

Expected: git reports conflicts in several files. Do NOT abort. Let the conflict markers land in the working tree — that's the desired state before conflict resolution.

**Step 2: List conflicted files**

```bash
git diff --name-only --diff-filter=U
```

Expected output (roughly):
```
Configs/SyncedData.cs
Enchantment_Core.cs
UI/MainUI.cs
UI/SettingsUI.cs
ValheimEnchantmentSystem.cs
translations/English.yml
```

Binary files (`Resources/kg_enchantment`, `ThunderstorePackage/plugins/*.dll`, `ThunderstorePackage/kg.ValheimEnchantmentSystem.dll`) will be marked as conflicts too — resolve them by taking upstream's version (`git checkout --theirs`).

**Step 3: Take upstream's binary assets**

```bash
git checkout --theirs -- "Resources/kg_enchantment"
git checkout --theirs -- "ThunderstorePackage/plugins/ChaosValheimEnchantmentSystem/Jewelcrafting.dll"
git checkout --theirs -- "ThunderstorePackage/plugins/ChaosValheimEnchantmentSystem/YamlDotNet.dll"
git checkout --theirs -- "ThunderstorePackage/plugins/ChaosValheimEnchantmentSystem/fastJSON.dll"
git checkout --theirs -- "ThunderstorePackage/kg.ValheimEnchantmentSystem.dll"
git add Resources/kg_enchantment ThunderstorePackage/
```

Do NOT commit yet — the text file conflicts still need manual resolution.

---

### Task 2: Resolve `ValheimEnchantmentSystem.cs`

**Files:**
- Modify: `ValheimEnchantmentSystem.cs`

This file only has whitespace and plugin name/version differences. The upstream renamed the plugin to "Valheim Enchantment System" and bumped version to "1.8.0". Keep the fork's name and version.

**Step 1: Open the file and remove conflict markers**

Read `ValheimEnchantmentSystem.cs`. It will contain `<<<<<<<`, `=======`, `>>>>>>>` markers. The resolution rule is: keep everything from the `<<<<<<< HEAD` side, discard everything from the `>>>>>>> kg/kg` side, including the markers themselves.

The only meaningful upstream change here is version bump to `1.8.0` — we will handle versioning manually at release time, not now. Keep fork's `"0.1.7"` for now (we'll bump it after all changes are done).

After removing conflict markers, the file should be identical to its current state with no `<<<`, `===`, `>>>` lines.

**Step 2: Stage**

```bash
git add ValheimEnchantmentSystem.cs
```

---

### Task 3: Resolve `Configs/SyncedData.cs`

**Files:**
- Modify: `Configs/SyncedData.cs`

**What upstream added (must integrate):**
- `DropEnchantmentOnUpgrade` config entry
- `BlessedScrollsPreventBreak` config entry
- `BlessedScrollsAdditionalChance` config entry
- Simplified `GetEnchantmentChance` logic
- Simplified `GetStatIncrease` to use `en.level` directly

**What fork has (must keep):**
- `using UnityEngine;`, `using ItemManager;`, `using fastJSON;`, `using static fastJSON.Reflection;`
- Fork's defaults: `ItemFailureType = CombinedEasy`, `AdditionalEnchantmentChancePerLevel = 0.00f`, `AllowVFXArmor = true`, `EnchantmentNotificationMinLevel = 5`
- `GetRandomizedMultiplier()` method (entire method — this is the fork's core feature)
- `GetStatIncrease(Enchanted en, string effectType)` overload
- `GetPrevEnchantmentChance()` method
- All fork-added `public static ConfigEntry<...>` field declarations at the bottom

**Step 1: Read the conflicted file, identify conflict blocks**

Each conflict block looks like:
```
<<<<<<< HEAD
// fork code
=======
// upstream code
>>>>>>> kg/kg
```

**Step 2: Resolve `OnInit()` method conflict**

In the `OnInit()` method, after the `SafetyLevel` line, the upstream adds `DropEnchantmentOnUpgrade`. Keep fork's version of all existing lines, but insert the three new upstream lines after `SafetyLevel`:

```csharp
SafetyLevel = ValheimEnchantmentSystem.config("Enchantment", "SafetyLevel", 3,
    "The level until which enchantments won't destroy the item. Set to 0 to disable.");
DropEnchantmentOnUpgrade = ValheimEnchantmentSystem.config("Enchantment", "DropEnchantmentOnUpgrade", false, "Drop enchantment on item upgrade.");
ItemFailureType = ValheimEnchantmentSystem.config("Enchantment", "ItemFailureType", ItemDesctructionTypeEnum.CombinedEasy, "LevelDecrease - downgrade level by 1 on failure\nDestroy - destroy item on failure\nCombined - uses yaml, downgrade or destroy on failure\nCombinedEasy - uses yaml, no change or downgrade on failure");
BlessedScrollsPreventBreak = ValheimEnchantmentSystem.config("Enchantment", "BlessedScrollsPreventBreak", true, "Blessed enchant scrolls prevent breaking of item in case of failed enchant. If set to false enchanting chance is increased instead of preventing item break.");
BlessedScrollsAdditionalChance = ValheimEnchantmentSystem.config("Enchantment", "BlessedScrollsAdditionalChance", 25, "Enchanting chance added when using blessed enchant scrolls if the option to prevent breaking of an item in case of failed enchant is set to false.");
AllowJewelcraftingMirrorCopyEnchant = ValheimEnchantmentSystem.config("Enchantment", "AllowJewelcraftingMirrorCopyEnchant", false, "Allow jewelcrafting to copy enchantment from one item to another using mirror.");
AdditionalEnchantmentChancePerLevel = ValheimEnchantmentSystem.config("Enchantment", "AdditionalEnchantmentChancePerLevel", 0.00f, "Additional enchantment chance per level of Enchantment skill. (ex. 0.05 = 5% at max level)");
AllowVFXArmor = ValheimEnchantmentSystem.config("Enchantment", "AllowVFXArmor", true, "Allow VFX on armor.");
EnchantmentEnableNotifications = ValheimEnchantmentSystem.config("Notifications", "EnchantmentEnableNotifications", true, "Enable enchantment notifications.");
EnchantmentNotificationMinLevel = ValheimEnchantmentSystem.config("Notifications", "EnchantmentNotificationMinLevel", 5, "The minimum level of enchantment to show notification.");
```

**Step 3: Resolve `GetEnchantmentChance` conflict**

The upstream simplified this method. Take the upstream's cleaner version (remove the long version from fork). Replace the fork's `GetEnchantmentChance(string dropPrefab, int level)` with:

```csharp
private static Chance_Data GetEnchantmentChance(string dropPrefab, int level)
{
    if (level == 0) return new Chance_Data() { success = 100 };
    if (dropPrefab != null && OPTIMIZED_Overrides_EnchantmentChances.TryGetValue(dropPrefab, out Dictionary<int, Chance_Data> overriden))
    {
        if (overriden.TryGetValue(level, out Chance_Data overrideChance))
            return overrideChance;
    }

    return Synced_EnchantmentChances.Value.TryGetValue(level, out Chance_Data chance) ? chance : new Chance_Data() { success = 0 };
}
```

Keep the `GetPrevEnchantmentChance` method — the fork uses it.

**Step 4: Resolve `GetStatIncrease` conflict**

Take upstream's version using `en.level` directly (simpler). Keep the overload `GetStatIncrease(Enchanted en, string effectType)` — it is used by effect classes. Keep `GetRandomizedMultiplier` entirely (upstream removed it, but it's the fork's feature).

```csharp
public static Stat_Data GetStatIncrease(Enchantment_Core.Enchanted en)
{
    if (en.level == 0) return null;
    string dropPrefab = en.Item.m_dropPrefab?.name;
    if (dropPrefab != null && OPTIMIZED_Overrides_EnchantmentStats.TryGetValue(dropPrefab, out Dictionary<int, Stat_Data> overriden))
    {
        return overriden.TryGetValue(en.level, out Stat_Data overrideChance) ? overrideChance : null;
    }

    Dictionary<int, Stat_Data> target = en.Item.IsWeapon() ? Synced_EnchantmentStats_Weapons.Value : Synced_EnchantmentStats_Armor.Value;
    return target.TryGetValue(en.level, out Stat_Data increase) ? increase : null;
}
```

**Step 5: Add new field declarations**

In the field declarations section (bottom of file, around line 389), add the three new entries from upstream:

```csharp
public static ConfigEntry<bool> DropEnchantmentOnUpgrade;
public static ConfigEntry<bool> BlessedScrollsPreventBreak;
public static ConfigEntry<int> BlessedScrollsAdditionalChance;
```

Place them after `SafetyLevel` and before `ItemFailureType`.

**Step 6: Stage**

```bash
git add Configs/SyncedData.cs
```

---

### Task 4: Resolve `Enchantment_Core.cs`

**Files:**
- Modify: `Enchantment_Core.cs`

This is the most complex conflict. The upstream's version stripped away all fork features and reverted to a simpler model. The fork's version must be kept almost entirely. We cherry-pick only specific upstream changes.

**What to take from upstream:**
1. `FrameSkipEquip`: add `|| !weapon.IsWeapon()` to the guard, and restore the equip call at the end
2. `CheckRandom`: add `bool useBless, bool preventBreak` parameters and bless-bonus logic
3. `Enchant`: add `bool blessPreventBreak` second parameter; update `CheckRandom` call; update safety-level guard
4. Bug fix from `ae30a80`: the `useBless` flag is passed to `CheckRandom` so only blessed scrolls get the addedChance bonus

**What to keep from fork (do NOT take upstream):**
- All of `EquipmentEffectCache` class
- All of the `Enchanted` class internals (`enchantedItem`, `effects`, `cachedMultipliedStats`, `RandomizeAndSaveEnchantedItem`, `EnchantReroll`, `Reroll`, etc.)

**Step 1: Read the conflicted file**

Read `Enchantment_Core.cs`. Most of the fork's code will be in `<<<<<<< HEAD` blocks.

**Step 2: Fix `FrameSkipEquip`**

Change (in the fork's version of this method):
```csharp
if (!Player.m_localPlayer.IsItemEquiped(weapon)) yield break;
```
to:
```csharp
if (!Player.m_localPlayer.IsItemEquiped(weapon) || !weapon.IsWeapon()) yield break;
```

Also, the upstream restores the equip after the yield frames. The fork has this commented out. Uncomment/restore it:
```csharp
if (Player.m_localPlayer && Player.m_localPlayer.m_inventory.ContainsItem(weapon))
    Player.m_localPlayer?.EquipItem(weapon);
```

**Step 3: Update `CheckRandom` signature**

Change:
```csharp
private bool CheckRandom(out bool destroy)
{
    float random = Random.Range(0f, 100f);
    SyncedData.Chance_Data chanceData = GetEnchantmentChanceData();
    float additionalChance = SyncedData.GetAdditionalEnchantmentChance();
    destroy = chanceData.destroy > 0 && Random.Range(0f, 100f) <= chanceData.destroy;
    return random <= chanceData.success + additionalChance;
}
```

To (full upstream version with bless logic):
```csharp
private bool CheckRandom(bool useBless, bool preventBreak, out bool destroy)
{
    float random = Random.Range(0f, 100f);
    Int32.TryParse(SyncedData.BlessedScrollsAdditionalChance.Value.ToString(), out int addedChance);
    SyncedData.Chance_Data chanceData = GetEnchantmentChanceData();
    float additionalChance = SyncedData.GetAdditionalEnchantmentChance();
    int reduceDestroyChance = useBless ? addedChance : 0;
    destroy = chanceData.destroy > 0 && Random.Range(0f, 100f) <= chanceData.destroy - reduceDestroyChance;
    float chance = chanceData.success + additionalChance;
    if (useBless && !preventBreak)
    {
        chance += addedChance;
    }
    return random <= chance;
}
```

**Step 4: Update `Enchant` signature and call sites**

Change:
```csharp
public bool Enchant(bool safeEnchant, out string msg)
```
to:
```csharp
public bool Enchant(bool safeEnchant, bool blessPreventBreak, out string msg)
```

Inside `Enchant`, change:
```csharp
if (CheckRandom(out bool destroy))
```
to:
```csharp
if (CheckRandom(safeEnchant, blessPreventBreak, out bool destroy))
```

Change the safety level guard:
```csharp
if (SyncedData.SafetyLevel.Value <= level && !safeEnchant)
```
to:
```csharp
if (SyncedData.SafetyLevel.Value <= level && (!safeEnchant || (safeEnchant && !blessPreventBreak)))
```

**Step 5: Stage**

```bash
git add Enchantment_Core.cs
```

---

### Task 5: Resolve `UI/MainUI.cs`

**Files:**
- Modify: `UI/MainUI.cs`

**What to take from upstream:**
1. In the `Update` method / `StartButton_Click` (timer completion block): after the `enchanted`/`msg` result is obtained, add skill XP gain based on scroll tier
2. In `UseBless_ButtonClick`: add early-return guard if enchant chance is 0

**What to keep from fork:**
- Everything else — the fork's MainUI has reroll support and other features the upstream stripped

**Step 1: Read `UI/MainUI.cs`, remove conflict markers keeping fork's code**

The fork has `_reroll` support, `Reroll_ButtonClick`, `Reroll_Transform`, `Reroll_Icon` etc. — all of these are fork features to keep.

**Step 2: Update `Enchant` call site**

Find (around line 312):
```csharp
enchanted = en.Enchant(_useBless, out msg);
```

Change to:
```csharp
enchanted = en.Enchant(_useBless, SyncedData.BlessedScrollsPreventBreak.Value, out msg);
```

**Step 3: Add skill XP gain after enchant result**

After the `enchanted = en.Enchant(...)` line, add the XP block from upstream. Insert:

```csharp
string dropName = _currentItem.m_dropPrefab
    ? _currentItem.m_dropPrefab.name
    : Utils.GetPrefabNameByItemName(_currentItem.m_shared.m_name);
if (SyncedData.GetReqs(dropName) is { } reqs)
{
    int exp = 0;
    switch (reqs.enchant_prefab.prefab.ToString().Substring(reqs.enchant_prefab.prefab.Length - 2))
    {
        case "_F": exp = 2; break;
        case "_D": exp = 7; break;
        case "_C": exp = 14; break;
        case "_B": exp = 23; break;
        case "_A": exp = 34; break;
        case "_S": exp = 47; break;
    }
    Utils.IncreaseSkillEXP(Enchantment_Skill.SkillType_Enchantment, exp);
}
```

**Step 4: Update `UseBless_ButtonClick`**

Add an early-return guard at the top of the method so toggling bless when enchant chance is 0 does nothing. After the null check, add:

```csharp
if (_currentItem == null) return;
Enchantment_Core.Enchanted en = _currentItem.Data().Get<Enchantment_Core.Enchanted>();
if (en != null && en.GetEnchantmentChance() <= 0) return;
```

Wrap the existing body in try-catch per the upstream pattern:
```csharp
private static void UseBless_ButtonClick()
{
    try
    {
        if (_currentItem == null) return;
        Enchantment_Core.Enchanted en = _currentItem.Data().Get<Enchantment_Core.Enchanted>();
        if (en != null && en.GetEnchantmentChance() <= 0) return;
        // ... existing fork body unchanged ...
    }
    catch (Exception) { }
}
```

**Step 5: Stage**

```bash
git add UI/MainUI.cs
```

---

### Task 6: Resolve `UI/SettingsUI.cs`

**Files:**
- Modify: `UI/SettingsUI.cs`

Minimal change. The upstream adds `this.Saved?.Invoke();` and whitespace. Keep fork's code, add the one new line.

**Step 1: Read the file, resolve markers keeping fork's code**

**Step 2: Add `this.Saved?.Invoke();`**

After `Enchantment_VFX._enableHotbarVisual.ConfigFile.Save();` in the `Save()` method, add:
```csharp
this.Saved?.Invoke();
```

**Step 3: Stage**

```bash
git add UI/SettingsUI.cs
```

---

### Task 7: Resolve `translations/English.yml`

**Files:**
- Modify: `translations/English.yml`

**Keep from fork:** All reroll-related keys (`enchantment_reroll`, `enchantment_success_reroll`, `enchantment_rerollchance`), `enchantment_movement_skill`, `enchantment_stagger_limit`, `enchantment_stagger_recovery`, `enchantment_matching_weapon_skill`.

**Add from upstream:** `enchantment_missingskill: "Your skill of $3 for enchanting $1 is too low, required skill is $2"`

**Take from upstream:** Updated descriptions for `enchantment_bonusespercentdamage: "Damage increase"` and `enchantment_bonusespercentarmor: "Armor increase"`.

**Step 1: Read the file, manually edit conflict blocks**

Apply changes above. The resulting file should have both fork's keys AND the new upstream key.

**Step 2: Stage**

```bash
git add translations/English.yml
```

---

### Task 8: Verify and commit the merge

**Step 1: Check no remaining conflict markers**

```bash
grep -rn "<<<<<<\|=======\|>>>>>>>" --include="*.cs" --include="*.yml" .
```

Expected: no output. If any markers remain, go back and fix them.

**Step 2: Build**

Build the project using your IDE (Visual Studio) or:
```bash
msbuild ValheimEnchantmentSystem.sln /p:Configuration=Debug
```

Expected: Build succeeds with 0 errors. Warnings are acceptable.

If build fails, read the error messages and fix the broken lines. Common issues after this merge:
- `Enchanted.enchantedItem` vs `Enchanted.level` — upstream changed the internal model; ensure any remaining references to `en.enchantedItem.level` are still valid (they should be — fork kept the `enchantedItem` field)
- Missing `DropEnchantmentOnUpgrade` reference — check it's declared in `SyncedData`

**Step 3: Commit the merge**

```bash
git add -u
git commit -m "$(cat <<'EOF'
merge: integrate upstream kg/kg changes

- Add BlessedScrollsPreventBreak and BlessedScrollsAdditionalChance configs
- Add DropEnchantmentOnUpgrade config
- Update CheckRandom/Enchant to use bless-prevent-break parameter correctly
- Fix: regular scrolls no longer add enchant chance bonus (ae30a80)
- Fix: SettingsUI save() now fires Saved event
- Add skill XP gain on enchant based on scroll tier
- Update UseBless button to guard against 0-chance items
- Update Unity/UI assets from upstream
- Preserve all fork features: randomization, EquipmentEffectCache, reroll, new effects

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
```

---

## Phase 2 — Update Valheim assemblies

---

### Task 9: Copy updated game assemblies

**Files:**
- Replace: `Libs/assembly_valheim.dll`
- Replace: `Libs/assembly_utils.dll`
- Replace: `Libs/assembly_guiutils.dll`

**Step 1: Copy from Valheim install**

```bash
VALHEIM_MANAGED="/d/Program Files (x86)/Steam/steamapps/common/Valheim/valheim_Data/Managed"
cp "$VALHEIM_MANAGED/assembly_valheim.dll" Libs/assembly_valheim.dll
cp "$VALHEIM_MANAGED/assembly_utils.dll" Libs/assembly_utils.dll
cp "$VALHEIM_MANAGED/assembly_guiutils.dll" Libs/assembly_guiutils.dll
```

**Step 2: Also copy Unity DLLs if they changed (optional)**

If the build fails citing Unity module issues, check if the Unity engine DLLs in Libs/ are outdated. Copy from the same Managed folder:
```bash
cp "$VALHEIM_MANAGED/UnityEngine.CoreModule.dll" Libs/UnityEngine.CoreModule.dll
# Repeat for any other failing Unity DLLs
```

---

### Task 10: Build after assembly update and fix breakage

**Step 1: Build**

```bash
msbuild ValheimEnchantmentSystem.sln /p:Configuration=Debug
```

**Step 2: Interpret errors**

Valheim game updates most commonly break Harmony patches due to renamed/removed private methods or changed method signatures. For each error:

- `CS0117: 'ClassName' does not contain a definition for 'MethodName'` — the game method was renamed. Search `assembly_valheim.dll` decompiled sources (use dnSpy or ILSpy) to find the new name.
- `CS1061: 'ClassName' does not have a member 'FieldName'` — field was renamed or removed. Find replacement.
- `CS0246: The type or namespace name 'X' could not be found` — a class was moved or renamed.

**Step 3: Fix each error**

Apply fixes using the Edit tool. Each fix should be minimal — only change what the compiler complains about.

**Step 4: Build again**

Repeat until 0 errors.

**Step 5: Commit**

```bash
git add Libs/assembly_valheim.dll Libs/assembly_utils.dll Libs/assembly_guiutils.dll
# Add any other changed files
git add -u
git commit -m "$(cat <<'EOF'
fix: update Valheim game assemblies and fix API compatibility

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
```

---

## Phase 3 — Randomization toggles and config exposure

---

### Task 11: Add config entries to `SyncedData`

**Files:**
- Modify: `Configs/SyncedData.cs`

**Step 1: Add field declarations**

In the field declarations section (near the other `ConfigEntry<>` fields, around line 389), add:

```csharp
// Randomization toggles
public static ConfigEntry<bool> RandomizeStatSelection;
public static ConfigEntry<bool> RandomizeStatValues;

// Randomization parameters
public static ConfigEntry<int> BaseStatLines;
public static ConfigEntry<int> ExtraLineLevelThreshold;
public static ConfigEntry<float> ExtraLineChance;
public static ConfigEntry<int> PityMaxLineDrop;
public static ConfigEntry<float> MultiplierMin;
public static ConfigEntry<float> MultiplierMax;
public static ConfigEntry<float> MultiplierInterval;
public static ConfigEntry<float> BonusMultiplierChance;
public static ConfigEntry<float> BonusMultiplier;
```

**Step 2: Register configs in `OnInit()`**

At the end of the `OnInit()` method (after the notification configs), add:

```csharp
RandomizeStatSelection = ValheimEnchantmentSystem.config("Randomization", "RandomizeStatSelection", true,
    "If true, a random subset of available stats is applied. If false, all non-zero stats are always applied.");
RandomizeStatValues = ValheimEnchantmentSystem.config("Randomization", "RandomizeStatValues", true,
    "If true, each stat is scaled by a random multiplier. If false, exact YAML values are used (multiplier = 1.0).");
BaseStatLines = ValheimEnchantmentSystem.config("Randomization", "BaseStatLines", 2,
    "Base number of stat lines before level scaling. Used when RandomizeStatSelection is true.");
ExtraLineLevelThreshold = ValheimEnchantmentSystem.config("Randomization", "ExtraLineLevelThreshold", 4,
    "Every N enchantment levels adds +1 base stat line. Used when RandomizeStatSelection is true.");
ExtraLineChance = ValheimEnchantmentSystem.config("Randomization", "ExtraLineChance", 0.5f,
    "Per-roll probability (0.0-1.0) of gaining one extra stat line. Used when RandomizeStatSelection is true.");
PityMaxLineDrop = ValheimEnchantmentSystem.config("Randomization", "PityMaxLineDrop", 1,
    "Max stat lines that can drop below the previous roll count (pity floor). Used when RandomizeStatSelection is true.");
MultiplierMin = ValheimEnchantmentSystem.config("Randomization", "MultiplierMin", 0.5f,
    "Minimum multiplier applied to each stat. Used when RandomizeStatValues is true.");
MultiplierMax = ValheimEnchantmentSystem.config("Randomization", "MultiplierMax", 1.0f,
    "Maximum base multiplier applied to each stat. Used when RandomizeStatValues is true.");
MultiplierInterval = ValheimEnchantmentSystem.config("Randomization", "MultiplierInterval", 0.25f,
    "Step size for rounding random multipliers. Used when RandomizeStatValues is true.");
BonusMultiplierChance = ValheimEnchantmentSystem.config("Randomization", "BonusMultiplierChance", 0.1f,
    "Probability (0.0-1.0) of an exceptional bonus roll. Used when RandomizeStatValues is true.");
BonusMultiplier = ValheimEnchantmentSystem.config("Randomization", "BonusMultiplier", 2.0f,
    "Multiplier factor applied on an exceptional roll. Used when RandomizeStatValues is true.");
```

**Step 3: Build to verify configs compile**

```bash
msbuild ValheimEnchantmentSystem.sln /p:Configuration=Debug
```

Expected: 0 errors.

---

### Task 12: Update `GetRandomizedMultiplier` to use configs

**Files:**
- Modify: `Configs/SyncedData.cs` (the `GetRandomizedMultiplier` method, lines ~317-373)

**Step 1: Read the current method**

Read `Configs/SyncedData.cs` around lines 317-373 to confirm the exact current implementation.

**Step 2: Replace the method body**

Replace the entire `GetRandomizedMultiplier` method with:

```csharp
public static List<EnchantmentEffect> GetRandomizedMultiplier(Enchantment_Core.Enchanted en, int bonusLineCount = 0)
{
    if (en?.enchantedItem?.level <= 0)
    {
        Debug.LogWarning("VES No floats because item null or lv0");
        return new List<EnchantmentEffect>();
    }
    var allStats = GetStatIncrease(en);
    if (allStats == null)
    {
        Debug.LogError("VES No possible stats found while randomizing, check your EnchantmentStats config yml");
        return new List<EnchantmentEffect>();
    }

    var multipliers = new List<EnchantmentEffect>();
    var possibleFields = typeof(Stat_Data).GetFields(BindingFlags.Public | BindingFlags.Instance)
                               .Where(f => f.FieldType == typeof(int) || f.FieldType == typeof(float))
                               .Where(f => Convert.ToDouble(f.GetValue(allStats)) != 0)
                               .ToList();

    bool randomizeSelection = RandomizeStatSelection.Value;
    bool randomizeValues = RandomizeStatValues.Value;

    List<FieldInfo> selectedFields;
    if (randomizeSelection)
    {
        var lineCount = BaseStatLines.Value + (en.level / ExtraLineLevelThreshold.Value) + bonusLineCount;
        while (UnityEngine.Random.value <= ExtraLineChance.Value && lineCount < possibleFields.Count)
            lineCount++;

        // Apply pity system
        int oldLineCount = en.enchantedItem.effects.Count;
        lineCount = Mathf.Clamp(lineCount, oldLineCount - PityMaxLineDrop.Value, possibleFields.Count);

        selectedFields = possibleFields.OrderBy(f => UnityEngine.Random.value).Take(lineCount).ToList();
    }
    else
    {
        selectedFields = possibleFields;
    }

    foreach (var field in selectedFields)
    {
        float floatMultiplier;
        if (randomizeValues)
        {
            float minMult = MultiplierMin.Value;
            float maxMult = MultiplierMax.Value;
            float interval = MultiplierInterval.Value;
            floatMultiplier = Mathf.Round(UnityEngine.Random.Range(minMult / interval, maxMult / interval)) * interval;
            if (UnityEngine.Random.value <= BonusMultiplierChance.Value)
                floatMultiplier *= BonusMultiplier.Value;
        }
        else
        {
            floatMultiplier = 1.0f;
        }

        multipliers.Add(new EnchantmentEffect(field.Name, floatMultiplier));
    }

    return multipliers;
}
```

Note: `en.level` is used instead of `en.enchantedItem.level` in the `randomizeSelection` block because `en.level` is the fork's property that mirrors `enchantedItem.level`. Verify which property name is correct by reading the `Enchanted` class — use whichever one compiles.

**Step 3: Build**

```bash
msbuild ValheimEnchantmentSystem.sln /p:Configuration=Debug
```

Expected: 0 errors.

**Step 4: Commit**

```bash
git add Configs/SyncedData.cs
git commit -m "$(cat <<'EOF'
feat: add randomization toggles and expose all hardcoded parameters as configs

New [Randomization] config section (all server-synced):
- RandomizeStatSelection: toggle random stat subset selection
- RandomizeStatValues: toggle random multipliers on each stat
- BaseStatLines, ExtraLineLevelThreshold, ExtraLineChance: line count params
- PityMaxLineDrop: pity system floor
- MultiplierMin, MultiplierMax, MultiplierInterval: multiplier range
- BonusMultiplierChance, BonusMultiplier: exceptional roll params

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
```

---

### Task 13: Bump version and final build

**Files:**
- Modify: `ValheimEnchantmentSystem.cs`
- Modify: `ThunderstorePackage/manifest.json`
- Modify: `ThunderstorePackage/CHANGELOG.md`

**Step 1: Update version**

In `ValheimEnchantmentSystem.cs`, change:
```csharp
private const string PLUGIN_VERSION = "0.1.7";
```
to:
```csharp
private const string PLUGIN_VERSION = "0.2.0";
```

In `ThunderstorePackage/manifest.json`, change `"version_number": "0.1.8"` to `"0.2.0"`.

**Step 2: Add changelog entry**

Prepend to `ThunderstorePackage/CHANGELOG.md`:
```markdown
#### Version 0.2.0
- Merged upstream: BlessedScrollsPreventBreak and BlessedScrollsAdditionalChance configs
- Merged upstream: Skill XP gain on enchant based on scroll tier
- Merged upstream: Fix regular scrolls incorrectly adding enchant chance bonus
- Merged upstream: SettingsUI save() fires Saved event
- Updated Valheim game assemblies for current patch
- New [Randomization] config section: toggle stat selection and stat value randomization
- Expose all randomization parameters as server-synced configs (BaseStatLines, ExtraLineLevelThreshold, ExtraLineChance, PityMaxLineDrop, MultiplierMin/Max/Interval, BonusMultiplierChance/Multiplier)
```

**Step 3: Final build**

```bash
msbuild ValheimEnchantmentSystem.sln /p:Configuration=Release
```

Expected: 0 errors, DLL produced in `bin/Release/`.

**Step 4: Commit**

```bash
git add ValheimEnchantmentSystem.cs ThunderstorePackage/manifest.json ThunderstorePackage/CHANGELOG.md
git commit -m "$(cat <<'EOF'
chore: bump version to 0.2.0

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
```

---

## Summary of commits produced

1. **Merge commit** — Phase 1 work (merge + conflict resolution)
2. **Assembly update commit** — Phase 2 work (new Valheim DLLs + API fixes)
3. **Toggles feature commit** — Phase 3, Task 11+12
4. **Version bump commit** — Phase 3, Task 13
