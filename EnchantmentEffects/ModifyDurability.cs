
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using UnityEngine;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects;

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetMaxDurability), typeof(int))]
[ClientOnlyPatch]
public class ApplySkillToDurability
{
    [UsedImplicitly]
    private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
    {
        __result *= 1 + Player.m_localPlayer.GetTotalEnchantedValue("durability_percentage") / 100f;
        __result += Player.m_localPlayer.GetTotalEnchantedValue("durability");
    }
}