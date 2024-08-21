
using System.Text.RegularExpressions;
using UnityEngine;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects;

[HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
public static class IncreaseEitr_Player_GetTotalFoodValue_Patch
{
    public static void Postfix(Player __instance, ref float eitr)
    {
        eitr += __instance.GetTotalEnchantedValue("max_eitr");
    }
}

[HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyEitrRegen))]
public static class ModifyEitrRegen_SEMan_ModifyEitrRegen_Patch
{
    public static void Postfix(SEMan __instance, ref float eitrMultiplier)
    {
        if (__instance.m_character.IsPlayer())
        {
            var player = __instance.m_character as Player;
            eitrMultiplier += player.GetTotalEnchantedValue("eitr_regen_percentage") / 100f;
        }
    }
}