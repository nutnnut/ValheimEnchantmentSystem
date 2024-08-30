
using System.Text.RegularExpressions;
using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using UnityEngine;
using static kg.ValheimEnchantmentSystem.Enchantment_Core;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects;

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetMaxDurability), typeof(int))]
[ClientOnlyPatch]
public class ApplySkillToDurability
{
    [UsedImplicitly]
    private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
    {
        if (__instance.Data()?.Get<Enchanted>() is { level: > 0 } data && data.Stats is { } stats)
        {
            __result *= 1 + stats.durability_percentage / 100f;
            __result += stats.durability;
        }
            
    }
}