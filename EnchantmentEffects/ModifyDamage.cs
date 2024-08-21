
using System.Text.RegularExpressions;
using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using UnityEngine;
using static kg.ValheimEnchantmentSystem.Enchantment_Core;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects;

[HarmonyPatch]
[ClientOnlyPatch]
public static class ModifyDamage
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