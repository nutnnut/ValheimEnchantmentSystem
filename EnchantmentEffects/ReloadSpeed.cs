using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using kg.ValheimEnchantmentSystem;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects;

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeaponLoadingTime))]
[ClientOnlyPatch]
public static class ItemWeaponLoadingTime_Patch
{
    [UsedImplicitly]
    private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
    {
        //try
        //{
        //    if (__instance?.Data().Get<Enchanted>() is not { level: > 0 } en) return;
        //    if (en.level > 0 && en.Stats != null)
        //    {
        //        var stats = en.Stats;
        //        __result *= 1 + stats.durability_percentage / 100f;
        //        __result += stats.durability;
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Debug.LogError($"Error in ApplySkillToDurability.Postfix: {ex}");
        //}
    }
}