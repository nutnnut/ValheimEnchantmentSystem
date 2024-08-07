using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using static kg.ValheimEnchantmentSystem.Enchantment_Core;
using static Skills;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects
{

    // Reload speed crossbow/shotgun staff
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeaponLoadingTime))]
    [ClientOnlyPatch]
    public static class ItemWeaponLoadingTime_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            __result /= 1.0f + Player.m_localPlayer.GetTotalEnchantedValue("attack_speed") / 100f;
        }
    }

    // projectile weapons,like stafficeshard
    [HarmonyPatch(typeof(Attack), nameof(Attack.UpdateProjectile))]
    public static class AttackUpdateProjectile_Patch
    {
        public static void Prefix(Attack __instance, ref float dt)
        {
            dt *= 1.0f + Player.m_localPlayer.GetTotalEnchantedValue("attack_speed") / 100f;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetSkillFactor))]
    public class QuickDrawBowSkillIncrease_Player_GetSkillFactor_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance, Skills.SkillType skill, ref float __result)
        {
            if (skill == Skills.SkillType.Bows)
            {
                float totalAttackSpeedBonus = Player.m_localPlayer.GetTotalEnchantedValue("attack_speed") / 100f;
                float drawTimeMultiplier = 1.0f / (1.0f + totalAttackSpeedBonus);

                float originalDrawTime = (1.0f - __result) * 0.8f + 0.2f;
                // drawTimeMultiplier * originalDrawTime = (1.0f - adjustedSkillFactor) * 0.8f + 0.2f
                float adjustedSkillFactor = 1.0f - (drawTimeMultiplier * originalDrawTime - 0.2f) / 0.8f;

                __result = adjustedSkillFactor; // may exceed 100 here but will get clamped by the game anyway
            }
        }
    }
}
