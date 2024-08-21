
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using UnityEngine;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects;

[HarmonyPatch(typeof(Player), nameof(Player.ApplyArmorDamageMods))]
[ClientOnlyPatch]
public static class Player_ApplyArmorDamageMods_Patch
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