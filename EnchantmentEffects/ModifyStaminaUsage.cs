
using System.Text.RegularExpressions;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects;

[HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackStamina))]
public class ModifyAttackStamina_Attack_GetStaminaUsage_Patch
{
    public static void Postfix(Attack __instance, ref float __result)
    {
        if (__instance.m_character is Player player)
        {
            __result *= player.GetTotalEnchantedMultiplierDecreaseMultiplicative("stamina_use_reduction_percent");
        }
    }
}