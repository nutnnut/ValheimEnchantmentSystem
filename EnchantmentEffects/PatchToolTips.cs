using System.Reflection.Emit;
using System.Text.RegularExpressions;
using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;
using static kg.ValheimEnchantmentSystem.Enchantment_Core;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects;

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData),
    typeof(int), typeof(bool), typeof(float))]
[ClientOnlyPatch]
public class PatchToolTips
{
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

            string suffix = en.GenerateNameSuffix();
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

            string suffix = en.GenerateNameSuffix();
            __instance.m_itemData.m_shared.m_name += suffix;
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop __instance, string __state)
        {
            if (__state != null) __instance.m_itemData.m_shared.m_name = __state;
        }
    }

    [UsedImplicitly]
    public static void Postfix(ItemDrop.ItemData item, bool crafting, int qualityLevel, ref string __result)
    {
        bool blockShowEnchant = false;
        if (item.Data().Get<Enchantment_Core.Enchanted>() is { level: > 0 } en)
        {
            SyncedData.Stat_Data stats = en.Stats;
            string color = SyncedData.GetColor(en, out _, true).IncreaseColorLight();

            if (stats)
            {
                if (stats.durability > 0)
                    __result = new Regex("(\\$item_durability.*)").Replace(__result, $"$1 <color={color}>(+{stats.durability})</color>");
                if (stats.durability_percentage > 0)
                    __result = new Regex("(\\$item_durability.*)").Replace(__result, $"$1 <color={color}>(+{stats.durability_percentage}%)</color>");

                int damagePercent = stats.damage_percentage;
                HitData.DamageTypes damage = item.GetDamage(qualityLevel, item.m_worldLevel);
                float totalDamage = damage.GetTotalBlockableDamage();

                Player.m_localPlayer.GetSkills().GetRandomSkillRange(out float minFactor, out float maxFactor, item.m_shared.m_skillType);

                // AddDamageTooltip(ref __result, damage.m_damage, "$inventory_damage", 0, stats.damage_percentage, totalDamage, minFactor, maxFactor, color);
                updateDmgTooltip(ref __result, damage.m_damage, "$inventory_damage", damagePercent, stats.damage_true, stats.damage_true_percentage, totalDamage, minFactor, maxFactor, color);
                updateDmgTooltip(ref __result, damage.m_chop, "$inventory_chop", damagePercent, stats.damage_chop, stats.damage_chop_percentage, totalDamage, minFactor, maxFactor, color);
                updateDmgTooltip(ref __result, damage.m_pickaxe, "$inventory_pickaxe", damagePercent, stats.damage_pickaxe, stats.damage_pickaxe_percentage, totalDamage, minFactor, maxFactor, color);
                updateDmgTooltip(ref __result, damage.m_blunt, "$inventory_blunt", damagePercent, stats.damage_blunt, stats.damage_blunt_percentage, totalDamage, minFactor, maxFactor, color);
                updateDmgTooltip(ref __result, damage.m_slash, "$inventory_slash", damagePercent, stats.damage_slash, stats.damage_slash_percentage, totalDamage, minFactor, maxFactor, color);
                updateDmgTooltip(ref __result, damage.m_pierce, "$inventory_pierce", damagePercent, stats.damage_pierce, stats.damage_pierce_percentage, totalDamage, minFactor, maxFactor, color);
                updateDmgTooltip(ref __result, damage.m_fire, "$inventory_fire", damagePercent, stats.damage_fire, stats.damage_fire_percentage, totalDamage, minFactor, maxFactor, color);
                updateDmgTooltip(ref __result, damage.m_frost, "$inventory_frost", damagePercent, stats.damage_frost, stats.damage_frost_percentage, totalDamage, minFactor, maxFactor, color);
                updateDmgTooltip(ref __result, damage.m_lightning, "$inventory_lightning", damagePercent, stats.damage_lightning, stats.damage_lightning_percentage, totalDamage, minFactor, maxFactor, color);
                updateDmgTooltip(ref __result, damage.m_poison, "$inventory_poison", damagePercent, stats.damage_poison, stats.damage_poison_percentage, totalDamage, minFactor, maxFactor, color);
                updateDmgTooltip(ref __result, damage.m_spirit, "$inventory_spirit", damagePercent, stats.damage_spirit, stats.damage_spirit_percentage, totalDamage, minFactor, maxFactor, color);

                __result += "\n";

                int armorPercent = stats.armor_percentage;
                if (armorPercent > 0)
                {
                    __result = new Regex("(\\$item_blockarmor.*)").Replace(__result, $"$1 <color={color}>(+{(item.GetBaseBlockPower(qualityLevel) * armorPercent / 100f).RoundOne()}({armorPercent}%))</color>");
                    __result = new Regex("(\\$item_armor.*)").Replace(__result, $"$1 <color={color}>(+{(item.GetArmor(qualityLevel, item.m_worldLevel) * armorPercent / 100f).RoundOne()}({armorPercent}%))</color>");
                }
                if (stats.armor > 0)
                {
                    __result = new Regex("(\\$item_blockarmor.*)").Replace(__result, $"$1 <color={color}>(+{stats.armor})</color>");
                    __result = new Regex("(\\$item_armor.*)").Replace(__result, $"$1 <color={color}>(+{stats.armor})</color>");
                }
                if (stats.movement_speed > 0)
                {
                    var totalMovementModifier = Player.m_localPlayer.GetEquipmentMovementModifier() * 100f;
                    bool replaced = false;
                    __result = Regex.Replace(__result, @"(\$item_movement_modifier.*)", match =>
                    {
                        replaced = true;
                        return $"{match.Value} <color={color}>+{stats.movement_speed}% ($item_total:{totalMovementModifier:+0;-0}%)</color>";
                    });
                    if (!replaced)
                    {
                        __result += $"\n<color={color}>$item_movement_modifier: +{stats.movement_speed}% ($item_total:{totalMovementModifier:+0;-0}%)</color>";
                    }
                }

                __result += stats.BuildAdditionalStats(color);
            }

            int chance = en.GetEnchantmentChance();
            if (chance > 0)
            {
                //__result += $"\n<color={color}>•</color> $enchantment_chance (<color={color}>{chance}%</color>)";
                //float additionalChance = SyncedData.GetAdditionalEnchantmentChance();
                //if (additionalChance > 0)
                //{
                //    __result += $" (<color={color}>+{additionalChance.RoundOne()}%</color> $enchantment_additionalchance)";
                //}
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

            //if (reqs.blessed_enchant_prefab.IsValid())
            //{
            //    string blessName = ZNetScene.instance.GetPrefab(reqs.blessed_enchant_prefab.prefab)
            //        .GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
            //    int val2 = reqs.blessed_enchant_prefab.amount;
            //    canBe += $"\n<color=yellow>• {blessName} x{val2}</color>";
            //}

            if (reqs.required_skill > 0)
            {
                canBe += "\n<color=yellow>• $enchantment_requiresskilllevel</color>".Localize(reqs.required_skill.ToString());
            }

            __result += canBe;
        }


        void updateDmgTooltip(ref string result, float baseDamage, string regexToReplace, int damage_percentage, int elementFlat, int elementPercent, float totalDamage, float minFactor, float maxFactor, string color)
        {
            double percentageBonus = (baseDamage * damage_percentage / 100f).RoundOne();
            double elementPercentBonus = (totalDamage * elementPercent / 100f).RoundOne();

            double totalAfterBonus = baseDamage + percentageBonus + elementFlat + elementPercentBonus;
            double min = Math.Round(totalAfterBonus * minFactor, 1);
            double max = Math.Round(totalAfterBonus * maxFactor, 1);

            string percentTxt = percentageBonus != 0 ? $"+{percentageBonus}({damage_percentage}%)" : "";
            string eflatTxt = elementFlat != 0 ? $"+{elementFlat}" : "";
            string epercentTxt = elementPercent != 0 ? $"+{elementPercentBonus}({elementPercent}%)" : "";

            string bonusTxt = string.Join(" ", new[] { percentTxt, eflatTxt, epercentTxt }.Where(s => !string.IsNullOrEmpty(s)));
            string rangeTxt = string.IsNullOrEmpty(bonusTxt) ? "" : $"({min} - {max})";

            if (baseDamage > 0)
            {
                result = new Regex($"(\\{regexToReplace}.*)").Replace(result,
                    $"$1<color={color}>{bonusTxt} {rangeTxt}</color>");
            }
            else if (totalAfterBonus > 0)
            {
                result += $"\n<color={color}>{regexToReplace}: {bonusTxt} {rangeTxt}</color>";
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
}
