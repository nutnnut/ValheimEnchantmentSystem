
using System.Text.RegularExpressions;
using kg.ValheimEnchantmentSystem;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects;

[HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackStamina))]
public static class ModifyAttackStaminaUse_Patch
{
    public static void Postfix(Attack __instance, ref float __result)
    {
        if (__instance.m_character is Player player)
        {
            __result *= player.GetTotalEnchantedMultiplierDecreaseMultiplicative("stamina_use_reduction_percent");
        }
    }
}

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDrawStaminaDrain))]
public static class ModifyBowDrawStaminaUse_Patch
{
    public static void Postfix(Attack __instance, ref float __result)
    {
        __result *= Player.m_localPlayer.GetTotalEnchantedMultiplierDecreaseMultiplicative("stamina_use_reduction_percent");
    }
}