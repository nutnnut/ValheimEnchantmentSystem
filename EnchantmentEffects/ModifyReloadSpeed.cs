using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using static kg.ValheimEnchantmentSystem.Enchantment_Core;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects
{
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
}
