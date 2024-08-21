using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem;
using kg.ValheimEnchantmentSystem.Misc;
using UnityEngine;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects
{
    [HarmonyPatch(typeof(Character), nameof(Character.SetMaxHealth))]
    [ClientOnlyPatch]
    public static class Character_SetMaxHealth_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Character __instance, ref float health)
        {
            if (__instance is Player player)
            {
                health += player.GetTotalEnchantedValue("max_hp");
            }
        }
    }

    // RandyKnapp's method, much more efficient
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateFood))]
    public static class AddHealthRegen_Player_UpdateFood_Patch
    {
        public static void Postfix(Player __instance)
        {
            // This works as a postfix, because on the timer is exactly zero on the same frame, 
            // then the tick just happened and the timer was reset
            if (__instance.m_foodRegenTimer != 0.0f)
            {
                return;
            }

            var regenAmount = __instance.GetTotalEnchantedValue("hp_regen");
            if (regenAmount <= 0)
            {
                return;
            }

            var regenMultiplier = 1.0f;
            __instance.m_seman.ModifyHealthRegen(ref regenMultiplier);
            __instance.Heal(regenAmount * regenMultiplier);
        }
    }

    // Heal on independent timer
    //[HarmonyPatch(typeof(Player), nameof(Player.FixedUpdate))]
    //[ClientOnlyPatch]
    //public static class Player_FixedUpdate_Patch
    //{
    //    [UsedImplicitly]
    //    private static void Postfix(Player __instance)
    //    {
    //        if (__instance != null && !__instance.IsDead())
    //        {
    //            float fixedDeltaTime = Time.fixedDeltaTime;
    //            __instance.UpdateEnchantmentHPRegen(fixedDeltaTime);
    //        }
    //    }
    //}
}

// Heal on independent timer
//public static partial class PlayerExtension
//{
//    private static float enchantmentRegenTimer = 0f;

//    public static void UpdateEnchantmentHPRegen(this Player player, float dt)
//    {
//        if (player == null) return;

//        if (player is Character character)
//        {
//            enchantmentRegenTimer += dt;
//            if (enchantmentRegenTimer >= 10f)
//            {
//                enchantmentRegenTimer = 0f;
//                float regen = player.GetTotalEnchantedValue("hp_regen");
//                if (regen > 0)
//                {
//                    character.Heal(regen);
//                }
//            }
//        }
//    }
//}