using System.Reflection.Emit;
using System.Text.RegularExpressions;
using ItemDataManager;
using ItemManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;
using kg.ValheimEnchantmentSystem.UI;
using static kg.ValheimEnchantmentSystem.Configs.SyncedData;
using static Skills;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;
using TMPro;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace kg.ValheimEnchantmentSystem;

[VES_Autoload]
public static class Enchantment_Core
{
    [UsedImplicitly]
    private static void OnInit()
    {
        if (ValheimEnchantmentSystem.NoGraphics) return;
        AnimationSpeedManager.Add(ModifyAttackSpeed);
    }

    public static IEnumerator FrameSkipEquip(ItemDrop.ItemData weapon)
    {
        if (!Player.m_localPlayer.IsItemEquiped(weapon) || !weapon.IsWeapon()) yield break;
        Player.m_localPlayer.UnequipItem(weapon);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        if (Player.m_localPlayer && Player.m_localPlayer.m_inventory.ContainsItem(weapon))
            Player.m_localPlayer?.EquipItem(weapon);
    }

    public class Enchanted : ItemData
    {
        public int level;
        private SyncedData.Stat_Data cachedMultipliedStats;
        public SyncedData.Stat_Data_Float randomizedFloat;

        public SyncedData.Stat_Data Stats
        {
            get
            {
                if (cachedMultipliedStats != null)
                {
                    return cachedMultipliedStats;
                }
                if (randomizedFloat == null)
                {
                    Debug.Log("VES Floats Not Found");
                    RandomizeAndSaveFloats();
                }
                var baseStats = SyncedData.GetStatIncrease(this);
                if (baseStats == null)
                {
                    Debug.LogError("Failed to get base stats, check config");
                    return null;
                }
                cachedMultipliedStats = baseStats.ApplyMultiplier(randomizedFloat);
                return cachedMultipliedStats;
            }
        }

        private void RandomizeAndSaveFloats()
        {
            cachedMultipliedStats = null;
            randomizedFloat = SyncedData.GetRandomizedMultiplier(this);
            Save();
        }

        public override void Save()
        {
            if (randomizedFloat != null)
            {
                Value = $"{level}|{randomizedFloat.SerializeJson()}";
            }
            else
            {
                Value = $"{level}|";
            }
            Enchantment_VFX.UpdateGrid();
        }

        public override void Load()
        {
            if (string.IsNullOrEmpty(Value)) return;
            var parts = Value.Split('|');
            if (parts.Length == 2 && int.TryParse(parts[0], out level))
            {
                if (!string.IsNullOrEmpty(parts[1]))
                {
                    randomizedFloat = SyncedData.Stat_Data_Float.DeserializeJson(parts[1]);
                }
            }
            else
            {
                Debug.LogWarning("VES Failed to deserialize floats...");
            }
        }

        public int GetRerollChance()
        {
            return SyncedData.GetEnchantmentChance(this).reroll;
        }

        public int GetEnchantmentChance()
        {
            return SyncedData.GetEnchantmentChance(this).success;
        }

        private SyncedData.Chance_Data GetEnchantmentChanceData()
        {
            return SyncedData.GetEnchantmentChance(this);
        }

        private bool HaveReqs(bool bless)
        {
            SyncedData.SingleReq singleReq = bless
                ? SyncedData.GetReqs(Item.m_dropPrefab.name).blessed_enchant_prefab
                : SyncedData.GetReqs(Item.m_dropPrefab.name).enchant_prefab;
            if (singleReq == null || !singleReq.IsValid()) return false;
            GameObject prefab = ZNetScene.instance.GetPrefab(singleReq.prefab);
            if (prefab == null) return false;
            int count = Utils.CustomCountItemsNoLevel(prefab.name);
            if (count >= singleReq.amount)
            {
                Utils.CustomRemoveItemsNoLevel(prefab.name, singleReq.amount);
                return true;
            }

            return false;
        }

        public bool CanEnchant()
        {
            if (GetEnchantmentChance() <= 0) return false;
            return IsEnchantablePrefab();
        }

        public bool IsEnchantablePrefab()
        {
            SyncedData.EnchantmentReqs reqs = SyncedData.GetReqs(Item.m_dropPrefab.name);
            return reqs != null;
        }

        private bool CheckRandom(out bool destroy)
        {
            float random = Random.Range(0f, 100f);
            SyncedData.Chance_Data chanceData = GetEnchantmentChanceData();
            float additionalChance = SyncedData.GetAdditionalEnchantmentChance();
            destroy = chanceData.destroy > 0 && Random.Range(0f, 100f) <= chanceData.destroy;
            return random <= chanceData.success + additionalChance;
        }

        public bool Reroll(bool safeEnchant, out string msg)
        {
            msg = "";
            if (!CanEnchant())
            {
                msg = "$enchantment_cannotbe".Localize();
                return false;
            }

            if (!HaveReqs(safeEnchant))
            {
                msg = "$enchantment_nomaterials".Localize();
                return false;
            }

            if (Random.Range(0f, 100f) <= 80f) // TODO: config chance
            {
                string oldSuffix = GenerateAsteriskSuffix(this);
                EnchantReroll();
                msg = "$enchantment_success_reroll".Localize(Item.m_shared.m_name.Localize(), level.ToString(), oldSuffix, GenerateAsteriskSuffix(this));
                return true;
            }

            bool destroy = Random.Range(0f, 100f) <= GetPrevEnchantmentChance(this).destroy;
            msg = HandleFailedEnchant(safeEnchant, level, destroy);

            return false;
        }

        public bool Enchant(bool safeEnchant, out string msg)
        {
            msg = "";
            if (!CanEnchant())
            {
                msg = "$enchantment_cannotbe".Localize();
                return false;
            }

            if (!HaveReqs(safeEnchant))
            {
                msg = "$enchantment_nomaterials".Localize();
                return false;
            }

            int prevLevel = level;
            if (CheckRandom(out bool destroy))
            {
                string oldSuffix = GenerateAsteriskSuffix(this);
                EnchantLevelUp();
                msg = "$enchantment_success".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString() + oldSuffix, level.ToString() + GenerateAsteriskSuffix(this));
                if (SyncedData.EnchantmentEnableNotifications.Value && SyncedData.EnchantmentNotificationMinLevel.Value <= level)
                    Notifications_UI.AddNotification(Player.m_localPlayer.GetPlayerName(), Item.m_dropPrefab.name, (int)Notifications_UI.NotificationItemResult.Success, prevLevel, level);
                return true;
            }

            msg = HandleFailedEnchant(safeEnchant, level, destroy);

            return false;
        }

        private string HandleFailedEnchant(bool safeEnchant, int prevLevel, bool destroy)
        {
            string msg;
            string oldSuffix = GenerateAsteriskSuffix(this);
            if (SyncedData.SafetyLevel.Value <= level && !safeEnchant)
            {
                Notifications_UI.NotificationItemResult notification;
                switch (SyncedData.ItemFailureType.Value)
                {
                    case SyncedData.ItemDesctructionTypeEnum.LevelDecrease:
                    default:
                        EnchantLevelDown();
                        msg = "$enchantment_fail_leveldown".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString() + oldSuffix, level.ToString() + GenerateAsteriskSuffix(this));
                        notification = Notifications_UI.NotificationItemResult.LevelDecrease;
                        break;
                    case SyncedData.ItemDesctructionTypeEnum.Destroy:
                        Player.m_localPlayer.UnequipItem(Item);
                        Player.m_localPlayer.m_inventory.RemoveItem(Item);
                        msg = "$enchantment_fail_destroyed".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString() + oldSuffix);
                        notification = Notifications_UI.NotificationItemResult.Destroyed;
                        break;
                    case SyncedData.ItemDesctructionTypeEnum.Combined:
                        notification = destroy ? Notifications_UI.NotificationItemResult.Destroyed : Notifications_UI.NotificationItemResult.LevelDecrease;
                        if (destroy)
                        {
                            Player.m_localPlayer.UnequipItem(Item);
                            Player.m_localPlayer.m_inventory.RemoveItem(Item);
                            msg = "$enchantment_fail_destroyed".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString() + oldSuffix);
                        }
                        else
                        {
                            EnchantLevelDown();
                            msg = "$enchantment_fail_leveldown".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString() + oldSuffix, level.ToString() + GenerateAsteriskSuffix(this));
                        }
                        break;
                    case SyncedData.ItemDesctructionTypeEnum.CombinedEasy:
                        notification = destroy ? Notifications_UI.NotificationItemResult.LevelDecrease : Notifications_UI.NotificationItemResult.LevelDecrease;
                        if (destroy)
                        {
                            EnchantLevelDown();
                            msg = "$enchantment_fail_leveldown".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString() + oldSuffix, level.ToString() + GenerateAsteriskSuffix(this));
                        }
                        else
                        {
                            msg = "$enchantment_fail_nochange".Localize(Item.m_shared.m_name.Localize(), level.ToString() + oldSuffix);
                        }
                        break;
                }

                if (SyncedData.EnchantmentEnableNotifications.Value && SyncedData.EnchantmentNotificationMinLevel.Value <= level)
                    Notifications_UI.AddNotification(Player.m_localPlayer.GetPlayerName(), Item.m_dropPrefab.name, (int)notification, prevLevel, level);
            }
            else
            {
                msg = "$enchantment_fail_nochange".Localize(Item.m_shared.m_name.Localize(), level.ToString() + oldSuffix);
                if (SyncedData.EnchantmentEnableNotifications.Value && SyncedData.EnchantmentNotificationMinLevel.Value <= level)
                    Notifications_UI.AddNotification(Player.m_localPlayer.GetPlayerName(), Item.m_dropPrefab.name, (int)Notifications_UI.NotificationItemResult.LevelDecrease, prevLevel, level);
            }

            return msg;
        }

        public void EnchantReroll()
        {
            RandomizeAndSaveFloats();
            Other_Mods_APIs.ApplyAPIs(this);
            ValheimEnchantmentSystem._thistype.StartCoroutine(FrameSkipEquip(Item));
        }

        public void EnchantLevelUp()
        {
            level++;
            EnchantReroll();
        }

        public void EnchantLevelDown()
        {
            level = Mathf.Max(0, level - 1);
            EnchantReroll();
        }

        public static implicit operator bool(Enchanted en) => en != null;
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip))]
    [ClientOnlyPatch]
    private static class InventoryGrid_CreateItemTooltip_Patch
    {
        [UsedImplicitly]
        private static void Prefix(InventoryGrid __instance, ItemDrop.ItemData item, out string __state)
        {
            __state = null;
            if (item?.Data().Get<Enchanted>() is not { level: > 0 } en) return;
            __state = item.m_shared.m_name;

            string suffix = GenerateNameSuffix(en);
            item.m_shared.m_name += suffix;
        }

        [UsedImplicitly]
        private static void Postfix(InventoryGrid __instance, ItemDrop.ItemData item, string __state)
        {
            if (__state != null) item.m_shared.m_name = __state;
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.GetHoverText))]
    [ClientOnlyPatch]
    private static class ItemDrop_GetHoverText_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ItemDrop __instance, out string __state)
        {
            __state = null;
            if (__instance.m_itemData?.Data().Get<Enchanted>() is not { level: > 0 } en) return;
            __state = __instance.m_itemData.m_shared.m_name;

            string suffix = GenerateNameSuffix(en);
            __instance.m_itemData.m_shared.m_name += suffix;
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop __instance, string __state)
        {
            if (__state != null) __instance.m_itemData.m_shared.m_name = __state;
        }
    }

    private static string GenerateNameSuffix(Enchanted en)
    {
        string suffix = "";
        string color = SyncedData.GetColor(en, out _, true)
            .IncreaseColorLight();

        if (en.randomizedFloat == null)
        {
            Debug.LogError("VES Failed to get float for suffix");
            return $" (<color={color}>+{en.level}</color>)";
        }

        string asteriskText = GenerateAsteriskSuffix(en);
        suffix += $" (<color={color}>+{en.level}</color>{asteriskText})";

        return suffix;
    }

    public static string GenerateAsteriskSuffix(Enchanted en)
    {
        if (en.randomizedFloat == null) return "";
        float sumOfFloats = typeof(Stat_Data_Float).GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(f => f.FieldType == typeof(float))
                    .Sum(f => (float)f.GetValue(en.randomizedFloat));

        int numberOfAsterisks = (int)Math.Round(Math.Max(sumOfFloats, 0), MidpointRounding.AwayFromZero);
        string asterisks = new string('*', numberOfAsterisks);

        string asteriskColor;
        if (numberOfAsterisks >= 10)
        {
            asteriskColor = "#FF0000"; // Red
        }
        else if (numberOfAsterisks >= 8)
        {
            asteriskColor = "#FFA500"; // Orange
        }
        else if (numberOfAsterisks >= 6)
        {
            asteriskColor = "#CC00CC"; // Purple
        }
        else if (numberOfAsterisks >= 4)
        {
            asteriskColor = "#4444FF"; // Blue
        }
        else if (numberOfAsterisks >= 2)
        {
            asteriskColor = "#00FF00"; // Green
        }
        else
        {
            asteriskColor = "#777777"; // Grey
        }

        string asteriskText = $"<color={asteriskColor}>{asterisks}</color>";
        return asteriskText;
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData),
        typeof(int), typeof(bool), typeof(float))]
    [ClientOnlyPatch]
    public class TooltipPatch
    {
        [UsedImplicitly]
        public static void Postfix(ItemDrop.ItemData item, bool crafting, int qualityLevel, ref string __result)
        {
            bool blockShowEnchant = false;
            if (item.Data().Get<Enchanted>() is { level: > 0 } en)
            {
                SyncedData.Stat_Data stats = en.Stats;
                string color = SyncedData.GetColor(en, out _, true).IncreaseColorLight();

                if (stats)
                {
                    int damagePercent = stats.damage_percentage;
                    if (stats.durability > 0)
                        __result = new Regex("(\\$item_durability.*)").Replace(__result, $"$1 (<color={color}>+{stats.durability}</color>)");
                    if (stats.durability_percentage > 0)
                        __result = new Regex("(\\$item_durability.*)").Replace(__result, $"$1 (<color={color}>+{stats.durability_percentage}%</color>)");

                    __result += "\n";

                    if (damagePercent > 0)
                    {
                        Player.m_localPlayer.GetSkills().GetRandomSkillRange(out float minFactor, out float maxFactor, item.m_shared.m_skillType);
                        HitData.DamageTypes damage = item.GetDamage(qualityLevel, item.m_worldLevel);
                        __result = new Regex("(\\$inventory_damage.*)").Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_damage * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_damage * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result = new Regex("(\\$inventory_blunt.*)").Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_blunt * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_blunt * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result = new Regex("(\\$inventory_slash.*)").Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_slash * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_slash * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result = new Regex("(\\$inventory_pierce.*)").Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_pierce * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_pierce * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result = new Regex("(\\$inventory_fire.*)").Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_fire * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_fire * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result = new Regex("(\\$inventory_frost.*)").Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_frost * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_frost * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result = new Regex("(\\$inventory_lightning.*)").Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_lightning * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_lightning * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result = new Regex("(\\$inventory_poison.*)").Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_poison * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_poison * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result = new Regex("(\\$inventory_spirit.*)").Replace(__result,
                            $"$1 (<color={color}>+{(damage.m_spirit * damagePercent / 100f * minFactor).RoundOne()} - {(damage.m_spirit * damagePercent / 100f * maxFactor).RoundOne()}</color>)");
                        __result += $"\n<color={color}>•</color> $enchantment_bonusespercentdamage: <color={color}>+{damagePercent}%</color>";
                    }
                    int armorPercent = stats.armor_percentage;
                    if (armorPercent > 0)
                    {
                        __result = new Regex("(\\$item_blockarmor.*)").Replace(__result, $"$1 (<color={color}>+{(item.GetBaseBlockPower(qualityLevel) * armorPercent / 100f).RoundOne()}({armorPercent}%)</color>)");
                        __result = new Regex("(\\$item_armor.*)").Replace(__result, $"$1 (<color={color}>+{(item.GetArmor(qualityLevel, item.m_worldLevel) * armorPercent / 100f).RoundOne()}({armorPercent}%)</color>)");
                        //__result += $"\n<color={color}>•</color> $enchantment_bonusespercentarmor: <color={color}>+{armorPercent}%</color>";
                    }
                    int armor = stats.armor;
                    if (armor > 0)
                    {
                        __result = new Regex("(\\$item_blockarmor.*)").Replace(__result, $"$1 (<color={color}>+{stats.armor}</color>)");
                        __result = new Regex("(\\$item_armor.*)").Replace(__result, $"$1 (<color={color}>+{stats.armor}</color>)");
                    }

                    __result += stats.BuildAdditionalStats(color);
                }

                int chance = en.GetEnchantmentChance();
                if (chance > 0)
                {
                    __result += $"\n<color={color}>•</color> $enchantment_chance (<color={color}>{chance}%</color>)";
                    float additionalChance = SyncedData.GetAdditionalEnchantmentChance();
                    if (additionalChance > 0)
                    {
                        __result += $" (<color={color}>+{additionalChance.RoundOne()}%</color> $enchantment_additionalchance)";
                    }
                }
                if (chance <= 0)
                {
                    blockShowEnchant = true;
                    __result += $"\n<color={color}>•</color> $enchantment_maxedout".Localize();
                }
            }


            if (blockShowEnchant) return;
            string dropName = item.m_dropPrefab
                ? item.m_dropPrefab.name
                : Utils.GetPrefabNameByItemName(item.m_shared.m_name);
            if (SyncedData.GetReqs(dropName) is { } reqs)
            {
                string canBe = $"\n• $enchantment_canbeenchantedwith:";
                if (reqs.enchant_prefab.IsValid())
                {
                    string mainName = ZNetScene.instance.GetPrefab(reqs.enchant_prefab.prefab).GetComponent<ItemDrop>()
                        .m_itemData.m_shared.m_name;
                    int val1 = reqs.enchant_prefab.amount;
                    canBe += $"\n<color=yellow>• {mainName} x{val1}</color>";
                }

                if (reqs.blessed_enchant_prefab.IsValid())
                {
                    string blessName = ZNetScene.instance.GetPrefab(reqs.blessed_enchant_prefab.prefab)
                        .GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                    int val2 = reqs.blessed_enchant_prefab.amount;
                    canBe += $"\n<color=yellow>• {blessName} x{val2}</color>";
                }

                if (reqs.required_skill > 0)
                {
                    canBe += "\n<color=yellow>• $enchantment_requiresskilllevel</color>".Localize(reqs.required_skill.ToString());
                }

                __result += canBe;
            }

        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
    [ClientOnlyPatch]
    private static class InventoryGui_UpdateRecipe_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance)
        {
            Enchanted en = __instance.m_selectedRecipe.Value?.Data().Get<Enchanted>();
            if (!en) return;
            string color = SyncedData.GetColor(en, out _, true).IncreaseColorLight();
            __instance.m_recipeName.text += $" (<color={color}>+{en!.level}</color>)";
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.AddRecipeToList))]
    [ClientOnlyPatch]
    private static class InventoryGui_AddRecipeToList_Patch
    {
        private static void Modify(ref string text, ItemDrop.ItemData item)
        {
            Enchanted en = item?.Data().Get<Enchanted>();
            if (!en) return;
            string color = SyncedData.GetColor(en, out _, true).IncreaseColorLight();
            text += $" (<color={color}>+{en!.level}</color>)";
        }

        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            CodeMatcher matcher = new(code);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Stloc_2));
            if (matcher.IsInvalid) return matcher.InstructionEnumeration();
            MethodInfo method = AccessTools.Method(typeof(InventoryGui_AddRecipeToList_Patch), nameof(Modify));
            matcher.Advance(1).InsertAndAdvance(new CodeInstruction(OpCodes.Ldloca_S, 2))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_3))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, method));
            return matcher.InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBlockPower), typeof(float))]
    [ClientOnlyPatch]
    private static class ModifyBlockPower
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.Data().Get<Enchanted>() is { level: > 0 } data && data.Stats is { } stats)
            {
                __result *= 1 + stats.armor_percentage / 100f;
                __result += stats.armor;
            }
        }
    }

    [HarmonyPatch]
    [ClientOnlyPatch]
    private static class ModifyArmor
    {
        [UsedImplicitly]
        private static MethodInfo TargetMethod()
        {
            return AccessTools.Method(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor));
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.Data().Get<Enchanted>() is { level: > 0 } data && data.Stats is { } stats)
            {
                __result *= 1 + stats.armor_percentage / 100f;
                __result += stats.armor;
            }
        }
    }

    [HarmonyPatch]
    [ClientOnlyPatch]
    private static class ModifyDamage
    {
        [UsedImplicitly]
        private static MethodInfo TargetMethod()
        {
            return AccessTools.Method(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage));
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            if (__instance.Data().Get<Enchanted>() is { level: > 0 } data && data.Stats is { } stats)
            {
                float rawDmg = __result.GetTotalBlockableDamage();
                __result.Modify(1 + stats.damage_percentage / 100f);
                __result.m_blunt += rawDmg * stats.damage_blunt_percentage / 100f + stats.damage_blunt;
                __result.m_slash += rawDmg * stats.damage_slash_percentage / 100f + stats.damage_slash;
                __result.m_pierce += rawDmg * stats.damage_pierce_percentage / 100f + stats.damage_pierce;
                __result.m_fire += rawDmg * stats.damage_fire_percentage / 100f + stats.damage_fire;
                __result.m_frost += rawDmg * stats.damage_frost_percentage / 100f + stats.damage_frost;
                __result.m_lightning += rawDmg * stats.damage_lightning_percentage / 100f + stats.damage_lightning;
                __result.m_poison += rawDmg * stats.damage_poison_percentage / 100f + stats.damage_poison;
                __result.m_spirit += rawDmg * stats.damage_spirit_percentage / 100f + stats.damage_spirit;
                __result.m_damage += rawDmg * stats.damage_true_percentage / 100f + stats.damage_true;
                __result.m_chop += rawDmg * stats.damage_chop_percentage / 100f + stats.damage_chop;
                __result.m_pickaxe += rawDmg * stats.damage_pickaxe_percentage / 100f + stats.damage_pickaxe;
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ApplyArmorDamageMods))]
    [ClientOnlyPatch]
    private static class Player_ApplyArmorDamageMods_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance, ref HitData.DamageModifiers mods)
        {
            foreach (var en in __instance.EquippedEnchantments())
            {
                if (en.Stats is { } stats) mods.Apply(stats.GetResistancePairs());
            }
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.SetMaxHealth))]
    [ClientOnlyPatch]
    private static class Character_SetMaxHealth_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Character __instance, ref float health)
        {
            if (__instance is Player player)
            {
                foreach (var en in player.EquippedEnchantments())
                {
                    if (en.Stats is { } stats)
                    {
                        health += stats.max_hp;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetMaxStamina))]
    [ClientOnlyPatch]
    private static class Player_SetMaxStamina_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Player __instance, ref float stamina)
        {
            foreach (var en in __instance.EquippedEnchantments())
            {
                if (en.Stats is { } stats)
                {
                    stamina += stats.max_stamina;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentMovementModifier))]
    [ClientOnlyPatch]
    private static class Player_UpdateMovementModifier_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance, ref float __result)
        {
            foreach (var en in __instance.EquippedEnchantments())
            {
                if (en.Stats is { } stats) __result += stats.movement_speed / 100f;
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetMaxDurability), typeof(int))]
    [ClientOnlyPatch]
    public class ApplySkillToDurability
    {
        //private static readonly object lockObject = new object();
        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            try
            {
                if (__instance?.Data().Get<Enchanted>() is not { level: > 0 } en) return;
                if (en.Stats != null)
                {
                    var stats = en.Stats;
                    __result *= 1 + stats.durability_percentage / 100f;
                    __result += stats.durability;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in ApplySkillToDurability.Postfix: {ex}");
            }
        }
    }

    private static double ModifyAttackSpeed(Character c, double speed)
    {
        if (c != Player.m_localPlayer || !c.InAttack()) return speed;

        ItemDrop.ItemData weapon = Player.m_localPlayer.GetCurrentWeapon();
        if (weapon == null) return speed;

        if (weapon.Data().Get<Enchanted>() is { level: > 0 } data && data.Stats is { attack_speed: > 0 } stats)
            return speed * (1 + stats.attack_speed / 100f);

        return speed;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetSkillFactor))]
    public class QuickDrawBowSkillIncrease_Player_GetSkillFactor_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance, Skills.SkillType skill, ref float __result)
        {
            if (skill == Skills.SkillType.Bows)
            {
                float totalAttackSpeedBonus = 0;
                foreach (var en in __instance.EquippedEnchantments())
                {
                    if (en.Stats is { } stats) totalAttackSpeedBonus += stats.attack_speed / 100f;
                }
                float drawTimeMultiplier = 1.0f / (1.0f + totalAttackSpeedBonus);

                float originalDrawTime = (1.0f - __result) * 0.8f + 0.2f;
                // drawTimeMultiplier * originalDrawTime = (1.0f - adjustedSkillFactor) * 0.8f + 0.2f
                // Solve
                float adjustedSkillFactor = 1.0f - (drawTimeMultiplier * originalDrawTime - 0.2f) / 0.8f;

                __result = adjustedSkillFactor; // may exceed 100 but got clamped by the game anyway
            }
        }
    }

    [HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillFactor))]
    public static class AddSkillLevel_Skills_GetSkillFactor_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Skills __instance, SkillType skillType, ref float __result)
        {
            __result += SkillIncrease(__instance.m_player, skillType) / 100f;
        }

        public static int SkillIncrease(Player player, SkillType skillType)
        {
            int increase = 0;

            int getSkillIncrease(SkillType[] types)
            {
                int result = 0;
                if (types.Contains(skillType))
                {
                    foreach (var en in player.EquippedEnchantments())
                    {
                        if (en.Stats is { } stats) result += stats.weapon_skill;
                    }
                    // Debug.LogWarning(skillType + ": +" + result);
                }
                return result;
            }

            increase += getSkillIncrease(new[] { player.GetCurrentWeapon().m_shared.m_skillType });
            // increase += check(new[] { SkillType.Run, SkillType.Jump, SkillType.Swim, SkillType.Sneak });

            return increase;
        }
    }

    // These fix a bug in vanilla where skill factor cannot go over 100
    [HarmonyPatch(typeof(Skills), nameof(Skills.GetRandomSkillRange))]
    public static class Skills_GetRandomSkillRange_Patch
    {
        public static bool Prefix(Skills __instance, out float min, out float max, SkillType skillType)
        {
            var skillValue = Mathf.Lerp(0.4f, 1.0f, __instance.GetSkillFactor(skillType));
            min = Mathf.Max(0, skillValue - 0.15f);
            max = skillValue + 0.15f;
            return false;
        }
    }

    [HarmonyPatch(typeof(Skills), nameof(Skills.GetRandomSkillFactor))]
    public static class Skills_GetRandomSkillFactor_Patch
    {
        // ReSharper disable once RedundantAssignment
        public static bool Prefix(Skills __instance, ref float __result, SkillType skillType)
        {
            __instance.GetRandomSkillRange(out var low, out var high, skillType);
            __result = Mathf.Lerp(low, high, Random.value);
            return false;
        }
    }

    [HarmonyPatch(typeof(SkillsDialog), nameof(SkillsDialog.Setup))]
    public static class DisplayExtraSkillLevels_SkillsDialog_Setup_Patch
    {
        [UsedImplicitly]
        private static void Postfix(SkillsDialog __instance, Player player)
        {
            var allSkills = player.m_skills.GetSkillList();

            // Remove existing extra level bars
            foreach (var element in __instance.m_elements)
            {
                var extraLevelBars = element.GetComponentsInChildren<Transform>(true);
                foreach (var bar in extraLevelBars)
                {
                    if (bar.gameObject.name == "ExtraLevelBar")
                    {
                        Object.Destroy(bar.gameObject);
                    }
                }
            }

            foreach (var element in __instance.m_elements)
            {
                var skill = allSkills.Find(s => s.m_info.m_description == element.GetComponentInChildren<UITooltip>().m_text);
                var extraSkillFromVES = AddSkillLevel_Skills_GetSkillFactor_Patch.SkillIncrease(player, skill.m_info.m_skill);
                if (extraSkillFromVES > 0)
                {
                    var levelbar = Utils.FindChild(element.transform, "bar");

                    var extraLevelbar = Object.Instantiate(levelbar.gameObject, levelbar.parent);
                    extraLevelbar.name = "ExtraLevelBar"; // Tag the extra level bar for removal
                    var rect = extraLevelbar.GetComponent<RectTransform>();
                    float skillLevel = player.GetSkills().GetSkillLevel(skill.m_info.m_skill);
                    rect.sizeDelta = new Vector2((skillLevel + extraSkillFromVES) * 1.6f, rect.sizeDelta.y);
                    var image = extraLevelbar.GetComponent<Image>();
                    image.color = Color.magenta;
                    extraLevelbar.transform.SetSiblingIndex(levelbar.GetSiblingIndex());

                    var bonustext = Utils.FindChild(element.transform, "bonustext");
                    var text = bonustext.GetComponent<TextMeshProUGUI>();
                    bool hasExistingSetBonus = skillLevel != Mathf.Floor(skill.m_level);
                    var extraSkillText = $"<color=#CC00CC>+{extraSkillFromVES}</color>";
                    text.text = hasExistingSetBonus ? text.text + extraSkillText : extraSkillText;
                    bonustext.gameObject.SetActive(true);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.FixedUpdate))]
    [ClientOnlyPatch]
    public static class Player_FixedUpdate_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance)
        {
            if (!__instance.IsDead())
            {
                float fixedDeltaTime = Time.fixedDeltaTime;
                __instance.UpdateEnchantmentRegen(fixedDeltaTime);
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UpdateStats), typeof(float))]
    [ClientOnlyPatch]
    public static class Player_UpdateStats_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance, float dt)
        {
            __instance.UpdateEnchantmentStaminaRegen(dt);
        }
    }
}

public static class PlayerExtensions
{
    private static float enchantmentRegenTimer = 0f;

    public static void UpdateEnchantmentRegen(this Player player, float dt)
    {
        enchantmentRegenTimer += dt;
        if (enchantmentRegenTimer >= 10f)
        {
            enchantmentRegenTimer = 0f;
            float regen = 0f;
            foreach (var en in player.EquippedEnchantments())
            {
                if (en.Stats is { } stats)
                {
                    regen += stats.hp_regen;
                }
            }
            if (regen > 0)
            {
                player.Heal(regen);
            }
        }
    }

    public static void UpdateEnchantmentStaminaRegen(this Player player, float dt)
    {
        if (player.IsDead() || player.InIntro() || player.IsTeleporting())
        {
            return;
        }

        bool flag = player.IsEncumbered();
        float maxStamina = player.GetMaxStamina();
        float num = 1f;
        if (player.IsBlocking())
        {
            num *= 0.8f;
        }
        if ((player.IsSwimming() && !player.IsOnGround()) || player.InAttack() || player.InDodge() || player.m_wallRunning || flag)
        {
            num = 0f;
        }

        float additionalRegen = 0f;
        foreach (var en in player.EquippedEnchantments())
        {
            if (en.Stats is { } stats)
            {
                additionalRegen += stats.stamina_regen;
            }
        }

        if (additionalRegen > 0f)
        {
            float staminaMultiplier = 1f;
            player.m_seman.ModifyStaminaRegen(ref staminaMultiplier);
            float regenAmount = additionalRegen * staminaMultiplier * num * dt;

            player.m_stamina = Mathf.Min(maxStamina, player.m_stamina + regenAmount * Game.m_staminaRegenRate);
            player.m_nview.GetZDO().Set(ZDOVars.s_stamina, player.m_stamina);
        }
    }
}