﻿using System.Reflection.Emit;
using System.Text.RegularExpressions;
using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;
using kg.ValheimEnchantmentSystem.UI;
using Random = UnityEngine.Random;

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

        public SyncedData.Stat_Data Stats => SyncedData.GetStatIncrease(this);

        public override void Save()
        {
            Value = level.ToString();
            Enchantment_VFX.UpdateGrid();
        }

        public override void Load()
        {
            if (string.IsNullOrEmpty(Value)) return;
            level = int.TryParse(Value, out int lvl) ? lvl : 0;
        }

        public override void Upgraded()
        {
            if (SyncedData.DropEnchantmentOnUpgrade.Value)
            {
                ValheimEnchantmentSystem._thistype.DelayedInvoke(() =>
                {
                    Item.Data().Remove<Enchanted>();
                    Enchantment_VFX.UpdateGrid();
                }, 1);
            }
            else
            {
                ValheimEnchantmentSystem._thistype.DelayedInvoke(() =>
                {
                    Other_Mods_APIs.ApplyAPIs_Upgraded(this);
                    Enchantment_VFX.UpdateGrid();
                }, 1);
            }
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

        private bool CanEnchant()
        {
            if (GetEnchantmentChance() <= 0) return false;
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
                level++;
                Save();
                Other_Mods_APIs.ApplyAPIs(this);
                ValheimEnchantmentSystem._thistype.StartCoroutine(FrameSkipEquip(Item));
                msg = "$enchantment_success".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString(), level.ToString());
                if (SyncedData.EnchantmentEnableNotifications.Value && SyncedData.EnchantmentNotificationMinLevel.Value <= level)
                    Notifications_UI.AddNotification(Player.m_localPlayer.GetPlayerName(), Item.m_dropPrefab.name, (int)Notifications_UI.NotificationItemResult.Success, prevLevel, level);
                return true;
            }
            
            if (SyncedData.SafetyLevel.Value <= level && !safeEnchant)
            {
                Notifications_UI.NotificationItemResult notification;
                switch (SyncedData.ItemFailureType.Value)
                {
                    case SyncedData.ItemDesctructionTypeEnum.LevelDecrease:
                    default:
                        level = Mathf.Max(0, level - 1);
                        Save();
                        Other_Mods_APIs.ApplyAPIs(this);
                        ValheimEnchantmentSystem._thistype.StartCoroutine(FrameSkipEquip(Item));
                        msg = "$enchantment_fail_leveldown".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString(), level.ToString());
                        notification = Notifications_UI.NotificationItemResult.LevelDecrease;
                        break;
                    case SyncedData.ItemDesctructionTypeEnum.Destroy:
                        Player.m_localPlayer.UnequipItem(Item);
                        Player.m_localPlayer.m_inventory.RemoveItem(Item);
                        msg = "$enchantment_fail_destroyed".Localize(Item.m_shared.m_name.Localize());
                        notification = Notifications_UI.NotificationItemResult.Destroyed;
                        break;
                    case SyncedData.ItemDesctructionTypeEnum.Combined:
                        notification = destroy ? Notifications_UI.NotificationItemResult.Destroyed : Notifications_UI.NotificationItemResult.LevelDecrease;
                        if (destroy)
                        {
                            Player.m_localPlayer.UnequipItem(Item);
                            Player.m_localPlayer.m_inventory.RemoveItem(Item);
                            msg = "$enchantment_fail_destroyed".Localize(Item.m_shared.m_name.Localize());
                        }
                        else
                        {
                            level = Mathf.Max(0, level - 1);
                            Save();
                            Other_Mods_APIs.ApplyAPIs(this);
                            ValheimEnchantmentSystem._thistype.StartCoroutine(FrameSkipEquip(Item));
                            msg = "$enchantment_fail_leveldown".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString(), level.ToString());
                        }
                        break;
                }
                
                if (SyncedData.EnchantmentEnableNotifications.Value && SyncedData.EnchantmentNotificationMinLevel.Value <= level)
                    Notifications_UI.AddNotification(Player.m_localPlayer.GetPlayerName(), Item.m_dropPrefab.name, (int)notification, prevLevel, level);
            }
            else
            {
                msg = "$enchantment_fail_nochange".Localize(Item.m_shared.m_name.Localize(), level.ToString());
                Save();
                if (SyncedData.EnchantmentEnableNotifications.Value && SyncedData.EnchantmentNotificationMinLevel.Value <= level)
                    Notifications_UI.AddNotification(Player.m_localPlayer.GetPlayerName(), Item.m_dropPrefab.name, (int)Notifications_UI.NotificationItemResult.LevelDecrease, prevLevel, level);
            }
            
            return false;
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
            if (item?.Data().Get<Enchanted>() is not { level: > 0 } idm) return;
            __state = item.m_shared.m_name;
            string color = SyncedData.GetColor(idm, out _, true).IncreaseColorLight();
            item.m_shared.m_name += $" (<color={color}>+{idm.level}</color>)";
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
            if (__instance.m_itemData?.Data().Get<Enchanted>() is not { level: > 0 } idm) return;
            __state = __instance.m_itemData.m_shared.m_name;
            string color = SyncedData.GetColor(idm, out _, true)
                .IncreaseColorLight();
            __instance.m_itemData.m_shared.m_name += $" (<color={color}>+{idm.level}</color>)";
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop __instance, string __state)
        {
            if (__state != null) __instance.m_itemData.m_shared.m_name = __state;
        }
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
                SyncedData.Stat_Data stats = SyncedData.GetStatIncrease(en);
                string color = SyncedData.GetColor(en, out _, true).IncreaseColorLight();
                
                if (stats)
                {
                    int damagePercent = stats.damage_percentage;
                    if (stats.durability_percentage > 0 || stats.durability > 0)
                        __result = new Regex("(\\$item_durability.*)").Replace(__result, $"$1 (<color={color}>$enchantment_increasedwithenchantment</color>)");
                    
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
                        __result += $"\n<color={color}>•</color> $enchantment_bonusespercentdamage (<color={color}>+{damagePercent}%</color>)";
                    }
                    int armorPercent = stats.armor_percentage;
                    if (armorPercent > 0)
                    {
                        __result = new Regex("(\\$item_blockarmor.*)").Replace(__result, $"$1 (<color={color}>+{(item.GetBaseBlockPower(qualityLevel) * armorPercent / 100f).RoundOne()}</color>)");
                        __result = new Regex("(\\$item_armor.*)").Replace(__result, $"$1 (<color={color}>+{(item.GetArmor(qualityLevel, item.m_worldLevel) * armorPercent / 100f).RoundOne()}</color>)");
                        __result += $"\n<color={color}>•</color> $enchantment_bonusespercentarmor (<color={color}>+{armorPercent}%</color>)";
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
            if (__instance.Data().Get<Enchanted>() is { level: > 0 } data && SyncedData.GetStatIncrease(data) is {} stats)
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
            if (__instance.Data().Get<Enchanted>() is { level: > 0 } data && SyncedData.GetStatIncrease(data) is {} stats)
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
            if (__instance.Data().Get<Enchanted>() is { level: > 0 } data && SyncedData.GetStatIncrease(data) is {} stats)
            {
                __result.Modify(1 + stats.damage_percentage / 100f);
                __result.m_blunt += stats.damage_blunt;
                __result.m_slash += stats.damage_slash;
                __result.m_pierce += stats.damage_pierce;
                __result.m_fire += stats.damage_fire;
                __result.m_frost += stats.damage_frost;
                __result.m_lightning += stats.damage_lightning;
                __result.m_poison += stats.damage_poison;
                __result.m_spirit += stats.damage_spirit;
                __result.m_damage += stats.damage_true;
                __result.m_chop += stats.damage_chop;
                __result.m_pickaxe += stats.damage_pickaxe;
            }
        }
    }
    
    [HarmonyPatch(typeof(Player),nameof(Player.ApplyArmorDamageMods))]
    [ClientOnlyPatch]
    private static class Player_ApplyArmorDamageMods_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance, ref HitData.DamageModifiers mods)
        {
            foreach (var en in __instance.EquippedEnchantments())
            {
                if (en.Stats is {} stats) mods.Apply(stats.GetResistancePairs());
            }
        }
    }
    
    [HarmonyPatch(typeof(Player),nameof(Player.GetEquipmentMovementModifier))]
    [ClientOnlyPatch] 
    private static class Player_UpdateMovementModifier_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance, ref float __result)
        {
            foreach (var en in __instance.EquippedEnchantments())
            {
                if (en.Stats is {} stats) __result += stats.movement_speed / 100f;
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetMaxDurability), typeof(int))]
    [ClientOnlyPatch]
    public class ApplySkillToDurability
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.Data().Get<Enchanted>() is { level: > 0 } data && SyncedData.GetStatIncrease(data) is {} stats)
            {
                __result *= 1 + stats.durability_percentage / 100f;
                __result += stats.durability;
            }
        }
    }

    private static double ModifyAttackSpeed(Character c, double speed)
    {
        if (c != Player.m_localPlayer || !c.InAttack()) return speed;
    
        ItemDrop.ItemData weapon = Player.m_localPlayer.GetCurrentWeapon();
        if (weapon == null) return speed;
        
        if (weapon.Data().Get<Enchanted>() is { level: > 0 } data && SyncedData.GetStatIncrease(data) is { attack_speed: > 0} stats)
            return speed * (1 + stats.attack_speed / 100f);
        
        return speed;
    }
}