
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using UnityEngine;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects;

[HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentMovementModifier))]
[ClientOnlyPatch]
public static class Player_UpdateMovementModifier_Patch
{
    [UsedImplicitly]
    private static void Postfix(Player __instance, ref float __result)
    {
        __result += __instance.GetTotalEnchantedValue("movement_speed") / 100f;
    }
}