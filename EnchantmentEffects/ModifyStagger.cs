using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects;

[HarmonyPatch(typeof(Character), nameof(Character.GetStaggerTreshold))]
public static class IncreaseStaggerLimit_GetStaggerThreshold_Patch
{
    [UsedImplicitly]
    public static void Postfix(Character __instance, ref float __result)
    {
        if (__instance is Player player)
        {
            __result *= 1.0f + player.GetTotalEnchantedValue("stagger_limit_percentage") / 100f;
        }
    }
}

[HarmonyPatch(typeof(Character), nameof(Character.UpdateStagger))]
public static class IncreaseStaggerRecovery_UpdateStagger_Patch
{
    [UsedImplicitly]
    public static void Postfix(Character __instance, ref float dt)
    {
        if (__instance is Player player)
        {
            float multiplier = 1.0f + player.GetTotalEnchantedValue("stagger_recovery_percentage") / 100f;
            float num = player.GetMaxHealth() * player.m_staggerDamageFactor * multiplier;
            player.m_staggerDamage -= num / 5f * dt;
            if (player.m_staggerDamage < 0f)
            {
                player.m_staggerDamage = 0f;
            }
        }
    }
}
