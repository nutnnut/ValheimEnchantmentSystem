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
            Player player = Player.m_localPlayer;

            float totalAttackSpeedBonus = 0;
            foreach (var en in player.EquippedEnchantments())
            {
                if (en.Stats is { } stats) totalAttackSpeedBonus += stats.attack_speed / 100f;
            }

            __result /= 1.0f + totalAttackSpeedBonus;
        }
    }

    // projectile weapons,like stafficeshard
    [HarmonyPatch(typeof(Attack), nameof(Attack.UpdateProjectile))]
    public static class AttackUpdateProjectile_Patch
    {
        public static void Prefix(Attack __instance, ref float dt)
        {
            Player player = Player.m_localPlayer;

            float totalAttackSpeedBonus = 0;
            foreach (var en in player.EquippedEnchantments())
            {
                if (en.Stats is { } stats) totalAttackSpeedBonus += stats.attack_speed / 100f;
            }

            dt *= 1 + totalAttackSpeedBonus;
        }
    }
}
