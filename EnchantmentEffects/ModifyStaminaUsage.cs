
using System.Text.RegularExpressions;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects;

[HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackStamina))]
public class ModifyAttackStamina_Attack_GetStaminaUsage_Patch
{
    public static void Postfix(Attack __instance, ref float __result)
    {
        if (__instance.m_character is Player player)
        {
            float multiplier = 1f;
            foreach (var en in player.EquippedEnchantments())
            {
                if (en.Stats is { } stats)
                {
                    multiplier *= 1 - stats.stamina_use_reduction_percent / 100f;
                }
            }

            __result *= multiplier;
        }
    }
}