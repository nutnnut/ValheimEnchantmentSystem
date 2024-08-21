
using System.Text.RegularExpressions;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects;

[HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
public static class IncreaseEitr_Player_GetTotalFoodValue_Patch
{
    public static void Postfix(Player __instance, ref float eitr)
    {

        int total = 0;
        foreach (var en in __instance.EquippedEnchantments())
        {
            if (en.Stats is { } stats)
            {
                total += stats.max_eitr;
            }
        }

        eitr += total;
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

            int total = 0;
            foreach (var en in player.EquippedEnchantments())
            {
                if (en.Stats is { } stats)
                {
                    total += stats.eitr_regen_percentage;
                }
            }

            eitrMultiplier += total / 100f;
        }
    }
}