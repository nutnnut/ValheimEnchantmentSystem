using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects;

[HarmonyPatch(typeof(Player), nameof(Player.SetMaxStamina))]
[ClientOnlyPatch]
public static class Player_SetMaxStamina_Patch
{
    [UsedImplicitly]
    private static void Prefix(Player __instance, ref float stamina)
    {
        stamina += __instance.GetTotalEnchantedValue("max_stamina");
    }
}

[HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyStaminaRegen))]
public static class SEMan_ModifyStaminaRegen_Patch
{
    public static void Postfix(SEMan __instance, ref float staminaMultiplier)
    {
        if (__instance.m_character.IsPlayer())
        {
            var player = __instance.m_character as Player;
            staminaMultiplier += player.GetTotalEnchantedValue("stamina_regen_percentage") / 100f;
        }
    }
}

// Regen on independent timer
[HarmonyPatch(typeof(Player), nameof(Player.UpdateStats), typeof(float))]
[ClientOnlyPatch]
public static class Player_UpdateStats_Patch
{
    [UsedImplicitly]
    private static void Postfix(Player __instance, float dt)
    {
        __instance.UpdateEnchantmentStaminaRegen(dt);
    }
}

public static partial class PlayerExtension
{
    // Regen on independent timer, may get unintended result if base game changes this
    public static void UpdateEnchantmentStaminaRegen(this Player player, float dt)
    {
        if (player.IsDead() || player.InIntro() || player.IsTeleporting())
        {
            return;
        }

        bool flag = player.IsEncumbered();
        float maxStamina = player.GetMaxStamina();
        float num = 1f;
        if (player.IsBlocking())
        {
            num *= 0.8f;
        }
        if ((player.IsSwimming() && !player.IsOnGround()) || player.InAttack() || player.InDodge() || player.m_wallRunning || flag)
        {
            num = 0f;
        }

        float additionalRegen = player.GetTotalEnchantedValue("stamina_regen");

        if (additionalRegen > 0f)
        {
            float staminaMultiplier = 1f;
            player.m_seman.ModifyStaminaRegen(ref staminaMultiplier);
            float regenAmount = additionalRegen * staminaMultiplier * num * dt;

            player.m_stamina = Mathf.Min(maxStamina, player.m_stamina + regenAmount * Game.m_staminaRegenRate);
            player.m_nview.GetZDO().Set(ZDOVars.s_stamina, player.m_stamina);
        }
    }
}