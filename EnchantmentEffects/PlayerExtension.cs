using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects;

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

[HarmonyPatch(typeof(Player), nameof(Player.FixedUpdate))]
[ClientOnlyPatch]
public static class Player_FixedUpdate_Patch
{
    [UsedImplicitly]
    private static void Postfix(Player __instance)
    {
        if (__instance != null && !__instance.IsDead())
        {
            float fixedDeltaTime = Time.fixedDeltaTime;
            __instance.UpdateEnchantmentHPRegen(fixedDeltaTime);
        }
    }
}

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
    private static float enchantmentRegenTimer = 0f;

    public static void UpdateEnchantmentHPRegen(this Player player, float dt)
    {
        if (player == null) return;
        
        enchantmentRegenTimer += dt;
        if (enchantmentRegenTimer >= 10f)
        {
            enchantmentRegenTimer = 0f;
            float regen = player.GetTotalEnchantedValue("hp_regen");
            if (regen > 0)
            {
                player.Heal(regen);
            }
        }
    }

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

        float additionalRegen =  player.GetTotalEnchantedValue("stamina_regen");

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