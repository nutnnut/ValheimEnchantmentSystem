using System.Reflection.Emit;
using ItemDataManager;
using ItemManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;
using kg.ValheimEnchantmentSystem.UI;
using static kg.ValheimEnchantmentSystem.Configs.SyncedData;
using static Skills;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;
using TMPro;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;
using System.Reflection;

namespace kg.ValheimEnchantmentSystem;

[VES_Autoload]
public static class Enchantment_Core
{
    [UsedImplicitly]
    private static void OnInit()
    {
        if (ValheimEnchantmentSystem.NoGraphics) return;
        AnimationSpeedManager.Add(ModifyAttackSpeed);
    }

    public static IEnumerator FrameSkipEquip(ItemDrop.ItemData weapon)
    {
        if (!Player.m_localPlayer.IsItemEquiped(weapon) || !weapon.IsWeapon()) yield break;
        Player.m_localPlayer.UnequipItem(weapon);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        if (Player.m_localPlayer && Player.m_localPlayer.m_inventory.ContainsItem(weapon))
            Player.m_localPlayer?.EquipItem(weapon);
    }

    public class Enchanted : ItemData
    {
        public int level;
        private SyncedData.Stat_Data cachedMultipliedStats;
        public SyncedData.Stat_Data_Float randomizedFloat;

        public SyncedData.Stat_Data Stats
        {
            get
            {
                if (level == 0) return null;
                if (cachedMultipliedStats != null)
                {
                    return cachedMultipliedStats;
                }
                if (randomizedFloat == null)
                {
                    Debug.LogWarning("VES Item Floats Not Found");
                    RandomizeAndSaveFloats();
                }
                var baseStats = SyncedData.GetStatIncrease(this);
                if (baseStats == null)
                {
                    Debug.LogError("Failed to get base stats, check config");
                    return null;
                }
                cachedMultipliedStats = baseStats.ApplyMultiplier(randomizedFloat);
                return cachedMultipliedStats;
            }
        }

        private void RandomizeAndSaveFloats()
        {
            cachedMultipliedStats = null;
            randomizedFloat = SyncedData.GetRandomizedMultiplier(this);
            if (randomizedFloat == null)
            {
                Debug.LogWarning("VES Failed to randomize, giving 0 empty floats");
                randomizedFloat = new Stat_Data_Float();
            }
            Save();
        }

        public override void Save()
        {
            if (randomizedFloat != null)
            {
                Value = $"{level}|{randomizedFloat.SerializeJson()}";
            }
            else
            {
                Value = $"{level}|";
            }
            Enchantment_VFX.UpdateGrid();
        }

        public override void Load()
        {
            if (string.IsNullOrEmpty(Value)) return;
            var parts = Value.Split('|');
            if (parts.Length == 2 && int.TryParse(parts[0], out level))
            {
                if (!string.IsNullOrEmpty(parts[1]))
                {
                    randomizedFloat = SyncedData.Stat_Data_Float.DeserializeJson(parts[1]);
                }
            }
            else
            {
                Debug.LogWarning("VES Failed to deserialize floats...");
            }
        }

        public int GetRerollChance()
        {
            return SyncedData.GetEnchantmentChance(this).reroll;
        }

        public int GetEnchantmentChance()
        {
            return SyncedData.GetEnchantmentChance(this).success;
        }

        private SyncedData.Chance_Data GetEnchantmentChanceData()
        {
            return SyncedData.GetEnchantmentChance(this);
        }

        private bool HaveReqs(bool bless)
        {
            SyncedData.SingleReq singleReq = bless
                ? SyncedData.GetReqs(Item.m_dropPrefab.name).blessed_enchant_prefab
                : SyncedData.GetReqs(Item.m_dropPrefab.name).enchant_prefab;
            if (singleReq == null || !singleReq.IsValid()) return false;
            GameObject prefab = ZNetScene.instance.GetPrefab(singleReq.prefab);
            if (prefab == null) return false;
            int count = Utils.CustomCountItemsNoLevel(prefab.name);
            if (count >= singleReq.amount)
            {
                Utils.CustomRemoveItemsNoLevel(prefab.name, singleReq.amount);
                return true;
            }

            return false;
        }

        public bool CanEnchant()
        {
            if (GetEnchantmentChance() <= 0) return false;
            return IsEnchantablePrefab();
        }

        public bool IsEnchantablePrefab()
        {
            SyncedData.EnchantmentReqs reqs = SyncedData.GetReqs(Item.m_dropPrefab.name);
            return reqs != null;
        }

        private bool CheckRandom(out bool destroy)
        {
            float random = Random.Range(0f, 100f);
            SyncedData.Chance_Data chanceData = GetEnchantmentChanceData();
            float additionalChance = SyncedData.GetAdditionalEnchantmentChance();
            destroy = chanceData.destroy > 0 && Random.Range(0f, 100f) <= chanceData.destroy;
            return random <= chanceData.success + additionalChance;
        }

        public bool Reroll(bool safeEnchant, out string msg)
        {
            msg = "";
            if (!CanEnchant())
            {
                msg = "$enchantment_cannotbe".Localize();
                return false;
            }

            if (!HaveReqs(safeEnchant))
            {
                msg = "$enchantment_nomaterials".Localize();
                return false;
            }

            if (Random.Range(0f, 100f) <= 80f) // TODO: config chance
            {
                string oldSuffix = GenerateAsteriskSuffix();
                EnchantReroll();
                msg = "$enchantment_success_reroll".Localize(Item.m_shared.m_name.Localize(), level.ToString(), oldSuffix, GenerateAsteriskSuffix());
                return true;
            }

            bool destroy = Random.Range(0f, 100f) <= GetPrevEnchantmentChance(this).destroy;
            msg = HandleFailedEnchant(safeEnchant, level, destroy);

            return false;
        }

        public bool Enchant(bool safeEnchant, out string msg)
        {
            msg = "";
            if (!CanEnchant())
            {
                msg = "$enchantment_cannotbe".Localize();
                return false;
            }

            if (!HaveReqs(safeEnchant))
            {
                msg = "$enchantment_nomaterials".Localize();
                return false;
            }

            int prevLevel = level;
            if (CheckRandom(out bool destroy))
            {
                string oldSuffix = GenerateAsteriskSuffix();
                EnchantLevelUp();
                msg = "$enchantment_success".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString() + oldSuffix, level.ToString() + GenerateAsteriskSuffix());
                if (SyncedData.EnchantmentEnableNotifications.Value && SyncedData.EnchantmentNotificationMinLevel.Value <= level)
                    Notifications_UI.AddNotification(Player.m_localPlayer.GetPlayerName(), Item.m_dropPrefab.name, (int)Notifications_UI.NotificationItemResult.Success, prevLevel, level);
                return true;
            }

            msg = HandleFailedEnchant(safeEnchant, level, destroy);

            return false;
        }

        private string HandleFailedEnchant(bool safeEnchant, int prevLevel, bool destroy)
        {
            string msg;
            string oldSuffix = GenerateAsteriskSuffix();
            if (SyncedData.SafetyLevel.Value <= level && !safeEnchant)
            {
                Notifications_UI.NotificationItemResult notification;
                switch (SyncedData.ItemFailureType.Value)
                {
                    case SyncedData.ItemDesctructionTypeEnum.LevelDecrease:
                    default:
                        EnchantLevelDown();
                        msg = "$enchantment_fail_leveldown".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString() + oldSuffix, level.ToString() + GenerateAsteriskSuffix());
                        notification = Notifications_UI.NotificationItemResult.LevelDecrease;
                        break;
                    case SyncedData.ItemDesctructionTypeEnum.Destroy:
                        Player.m_localPlayer.UnequipItem(Item);
                        Player.m_localPlayer.m_inventory.RemoveItem(Item);
                        msg = "$enchantment_fail_destroyed".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString() + oldSuffix);
                        notification = Notifications_UI.NotificationItemResult.Destroyed;
                        break;
                    case SyncedData.ItemDesctructionTypeEnum.Combined:
                        notification = destroy ? Notifications_UI.NotificationItemResult.Destroyed : Notifications_UI.NotificationItemResult.LevelDecrease;
                        if (destroy)
                        {
                            Player.m_localPlayer.UnequipItem(Item);
                            Player.m_localPlayer.m_inventory.RemoveItem(Item);
                            msg = "$enchantment_fail_destroyed".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString() + oldSuffix);
                        }
                        else
                        {
                            EnchantLevelDown();
                            msg = "$enchantment_fail_leveldown".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString() + oldSuffix, level.ToString() + GenerateAsteriskSuffix());
                        }
                        break;
                    case SyncedData.ItemDesctructionTypeEnum.CombinedEasy:
                        notification = destroy ? Notifications_UI.NotificationItemResult.LevelDecrease : Notifications_UI.NotificationItemResult.LevelDecrease;
                        if (destroy)
                        {
                            EnchantLevelDown();
                            msg = "$enchantment_fail_leveldown".Localize(Item.m_shared.m_name.Localize(), prevLevel.ToString() + oldSuffix, level.ToString() + GenerateAsteriskSuffix());
                        }
                        else
                        {
                            msg = "$enchantment_fail_nochange".Localize(Item.m_shared.m_name.Localize(), level.ToString() + oldSuffix);
                        }
                        break;
                }

                if (SyncedData.EnchantmentEnableNotifications.Value && SyncedData.EnchantmentNotificationMinLevel.Value <= level)
                    Notifications_UI.AddNotification(Player.m_localPlayer.GetPlayerName(), Item.m_dropPrefab.name, (int)notification, prevLevel, level);
            }
            else
            {
                msg = "$enchantment_fail_nochange".Localize(Item.m_shared.m_name.Localize(), level.ToString() + oldSuffix);
                if (SyncedData.EnchantmentEnableNotifications.Value && SyncedData.EnchantmentNotificationMinLevel.Value <= level)
                    Notifications_UI.AddNotification(Player.m_localPlayer.GetPlayerName(), Item.m_dropPrefab.name, (int)Notifications_UI.NotificationItemResult.LevelDecrease, prevLevel, level);
            }

            return msg;
        }

        public void EnchantReroll()
        {
            RandomizeAndSaveFloats();
            Other_Mods_APIs.ApplyAPIs(this);
            ValheimEnchantmentSystem._thistype.StartCoroutine(FrameSkipEquip(Item));
        }

        public void EnchantLevelUp()
        {
            level++;
            EnchantReroll();
        }

        public void EnchantLevelDown()
        {
            level = Mathf.Max(0, level - 1);
            EnchantReroll();
        }

        public static implicit operator bool(Enchanted en) => en != null;

        public string GenerateNameSuffix()
        {
            string suffix = "";
            string color = SyncedData.GetColor(this, out _, true)
                .IncreaseColorLight();

            if (randomizedFloat == null)
            {
                Debug.LogError("VES Failed to get float for suffix");
                return $" (<color={color}>+{level}</color>)";
            }

            string asteriskText = GenerateAsteriskSuffix();
            suffix += $" (<color={color}>+{level}</color>{asteriskText})";

            return suffix;
        }

        public string GenerateAsteriskSuffix()
        {
            if (randomizedFloat == null) return "";
            float sumOfFloats = typeof(Stat_Data_Float).GetFields(BindingFlags.Public | BindingFlags.Instance)
                        .Where(f => f.FieldType == typeof(float))
                        .Sum(f => (float)f.GetValue(randomizedFloat));

            int numberOfAsterisks = (int)Math.Round(Math.Max(sumOfFloats, 0), MidpointRounding.AwayFromZero);
            string asterisks = new string('*', numberOfAsterisks);

            string asteriskColor;
            if (numberOfAsterisks >= 10)
            {
                asteriskColor = "#FF0000"; // Red
            }
            else if (numberOfAsterisks >= 8)
            {
                asteriskColor = "#FFA500"; // Orange
            }
            else if (numberOfAsterisks >= 6)
            {
                asteriskColor = "#CC00CC"; // Purple
            }
            else if (numberOfAsterisks >= 4)
            {
                asteriskColor = "#4444FF"; // Blue
            }
            else if (numberOfAsterisks >= 2)
            {
                asteriskColor = "#00FF00"; // Green
            }
            else
            {
                asteriskColor = "#777777"; // Grey
            }

            string asteriskText = $"<color={asteriskColor}>{asterisks}</color>";
            return asteriskText;
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBlockPower), typeof(float))]
    [ClientOnlyPatch]
    private static class ModifyBlockPower
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.Data().Get<Enchanted>() is { level: > 0 } data && data.Stats is { } stats)
            {
                __result *= 1 + stats.armor_percentage / 100f;
                __result += stats.armor;
            }
        }
    }

    [HarmonyPatch]
    [ClientOnlyPatch]
    private static class ModifyArmor
    {
        [UsedImplicitly]
        private static MethodInfo TargetMethod()
        {
            return AccessTools.Method(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor));
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.Data().Get<Enchanted>() is { level: > 0 } data && data.Stats is { } stats)
            {
                __result *= 1 + stats.armor_percentage / 100f;
                __result += stats.armor;
            }
        }
    }

    [HarmonyPatch]
    [ClientOnlyPatch]
    private static class ModifyDamage
    {
        [UsedImplicitly]
        private static MethodInfo TargetMethod()
        {
            return AccessTools.Method(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage));
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            if (__instance.Data().Get<Enchanted>() is { level: > 0 } data && data.Stats is { } stats)
            {
                float rawDmg = __result.GetTotalBlockableDamage();
                __result.Modify(1 + stats.damage_percentage / 100f);
                __result.m_blunt += rawDmg * stats.damage_blunt_percentage / 100f + stats.damage_blunt;
                __result.m_slash += rawDmg * stats.damage_slash_percentage / 100f + stats.damage_slash;
                __result.m_pierce += rawDmg * stats.damage_pierce_percentage / 100f + stats.damage_pierce;
                __result.m_fire += rawDmg * stats.damage_fire_percentage / 100f + stats.damage_fire;
                __result.m_frost += rawDmg * stats.damage_frost_percentage / 100f + stats.damage_frost;
                __result.m_lightning += rawDmg * stats.damage_lightning_percentage / 100f + stats.damage_lightning;
                __result.m_poison += rawDmg * stats.damage_poison_percentage / 100f + stats.damage_poison;
                __result.m_spirit += rawDmg * stats.damage_spirit_percentage / 100f + stats.damage_spirit;
                __result.m_damage += rawDmg * stats.damage_true_percentage / 100f + stats.damage_true;
                __result.m_chop += rawDmg * stats.damage_chop_percentage / 100f + stats.damage_chop;
                __result.m_pickaxe += rawDmg * stats.damage_pickaxe_percentage / 100f + stats.damage_pickaxe;
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ApplyArmorDamageMods))]
    [ClientOnlyPatch]
    private static class Player_ApplyArmorDamageMods_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance, ref HitData.DamageModifiers mods)
        {
            foreach (var en in __instance.EquippedEnchantments())
            {
                if (en.Stats is { } stats) mods.Apply(stats.GetResistancePairs());
            }
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.SetMaxHealth))]
    [ClientOnlyPatch]
    private static class Character_SetMaxHealth_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Character __instance, ref float health)
        {
            if (__instance is Player player)
            {
                foreach (var en in player.EquippedEnchantments())
                {
                    if (en.Stats is { } stats)
                    {
                        health += stats.max_hp;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetMaxStamina))]
    [ClientOnlyPatch]
    private static class Player_SetMaxStamina_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Player __instance, ref float stamina)
        {
            foreach (var en in __instance.EquippedEnchantments())
            {
                if (en.Stats is { } stats)
                {
                    stamina += stats.max_stamina;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentMovementModifier))]
    [ClientOnlyPatch]
    private static class Player_UpdateMovementModifier_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance, ref float __result)
        {
            foreach (var en in __instance.EquippedEnchantments())
            {
                if (en.Stats is { } stats) __result += stats.movement_speed / 100f;
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetMaxDurability), typeof(int))]
    [ClientOnlyPatch]
    public class ApplySkillToDurability
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            try
            {
                if (__instance?.Data().Get<Enchanted>() is not { level: > 0 } en) return;
                if (en.level > 0 && en.Stats != null)
                {
                    var stats = en.Stats;
                    __result *= 1 + stats.durability_percentage / 100f;
                    __result += stats.durability;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in ApplySkillToDurability.Postfix: {ex}");
            }
        }
    }

    private static double ModifyAttackSpeed(Character c, double speed)
    {
        if (c != Player.m_localPlayer || !c.InAttack()) return speed;

        ItemDrop.ItemData weapon = Player.m_localPlayer.GetCurrentWeapon();
        if (weapon == null) return speed;

        if (weapon.Data().Get<Enchanted>() is { level: > 0 } data && data.Stats is { attack_speed: > 0 } stats)
            return speed * (1 + stats.attack_speed / 100f);

        return speed;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetSkillFactor))]
    public class QuickDrawBowSkillIncrease_Player_GetSkillFactor_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance, Skills.SkillType skill, ref float __result)
        {
            if (skill == Skills.SkillType.Bows)
            {
                float totalAttackSpeedBonus = 0;
                foreach (var en in __instance.EquippedEnchantments())
                {
                    if (en.Stats is { } stats) totalAttackSpeedBonus += stats.attack_speed / 100f;
                }
                float drawTimeMultiplier = 1.0f / (1.0f + totalAttackSpeedBonus);

                float originalDrawTime = (1.0f - __result) * 0.8f + 0.2f;
                // drawTimeMultiplier * originalDrawTime = (1.0f - adjustedSkillFactor) * 0.8f + 0.2f
                // Solve
                float adjustedSkillFactor = 1.0f - (drawTimeMultiplier * originalDrawTime - 0.2f) / 0.8f;

                __result = adjustedSkillFactor; // may exceed 100 but got clamped by the game anyway
            }
        }
    }

    [HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillFactor))]
    public static class AddSkillLevel_Skills_GetSkillFactor_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Skills __instance, SkillType skillType, ref float __result)
        {
            __result += SkillIncrease(__instance.m_player, skillType) / 100f;
        }

        public static int SkillIncrease(Player player, SkillType skillType)
        {
            int increase = 0;

            int getSkillIncrease(SkillType[] types)
            {
                int result = 0;
                if (types.Contains(skillType))
                {
                    foreach (var en in player.EquippedEnchantments())
                    {
                        if (en.Stats is { } stats) result += stats.weapon_skill;
                    }
                    // Debug.LogWarning(skillType + ": +" + result);
                }
                return result;
            }

            increase += getSkillIncrease(new[] { player.GetCurrentWeapon().m_shared.m_skillType });
            // increase += check(new[] { SkillType.Run, SkillType.Jump, SkillType.Swim, SkillType.Sneak });

            return increase;
        }
    }

    // These fix a bug in vanilla where skill factor cannot go over 100
    [HarmonyPatch(typeof(Skills), nameof(Skills.GetRandomSkillRange))]
    public static class Skills_GetRandomSkillRange_Patch
    {
        public static bool Prefix(Skills __instance, out float min, out float max, SkillType skillType)
        {
            var skillValue = Mathf.Lerp(0.4f, 1.0f, __instance.GetSkillFactor(skillType));
            min = Mathf.Max(0, skillValue - 0.15f);
            max = skillValue + 0.15f;
            return false;
        }
    }

    [HarmonyPatch(typeof(Skills), nameof(Skills.GetRandomSkillFactor))]
    public static class Skills_GetRandomSkillFactor_Patch
    {
        // ReSharper disable once RedundantAssignment
        public static bool Prefix(Skills __instance, ref float __result, SkillType skillType)
        {
            __instance.GetRandomSkillRange(out var low, out var high, skillType);
            __result = Mathf.Lerp(low, high, Random.value);
            return false;
        }
    }

    [HarmonyPatch(typeof(SkillsDialog), nameof(SkillsDialog.Setup))]
    public static class DisplayExtraSkillLevels_SkillsDialog_Setup_Patch
    {
        [UsedImplicitly]
        private static void Postfix(SkillsDialog __instance, Player player)
        {
            var allSkills = player.m_skills.GetSkillList();

            // Remove existing extra level bars
            foreach (var element in __instance.m_elements)
            {
                var extraLevelBars = element.GetComponentsInChildren<Transform>(true);
                foreach (var bar in extraLevelBars)
                {
                    if (bar.gameObject.name == "ExtraLevelBar")
                    {
                        Object.Destroy(bar.gameObject);
                    }
                }
            }

            foreach (var element in __instance.m_elements)
            {
                var skill = allSkills.Find(s => s.m_info.m_description == element.GetComponentInChildren<UITooltip>().m_text);
                var extraSkillFromVES = AddSkillLevel_Skills_GetSkillFactor_Patch.SkillIncrease(player, skill.m_info.m_skill);
                if (extraSkillFromVES > 0)
                {
                    var levelbar = Utils.FindChild(element.transform, "bar");

                    var extraLevelbar = Object.Instantiate(levelbar.gameObject, levelbar.parent);
                    extraLevelbar.name = "ExtraLevelBar"; // Tag the extra level bar for removal
                    var rect = extraLevelbar.GetComponent<RectTransform>();
                    float skillLevel = player.GetSkills().GetSkillLevel(skill.m_info.m_skill);
                    rect.sizeDelta = new Vector2((skillLevel + extraSkillFromVES) * 1.6f, rect.sizeDelta.y);
                    var image = extraLevelbar.GetComponent<Image>();
                    image.color = Color.magenta;
                    extraLevelbar.transform.SetSiblingIndex(levelbar.GetSiblingIndex());

                    var bonustext = Utils.FindChild(element.transform, "bonustext");
                    var text = bonustext.GetComponent<TextMeshProUGUI>();
                    bool hasExistingSetBonus = skillLevel != Mathf.Floor(skill.m_level);
                    var extraSkillText = $"<color=#CC00CC>+{extraSkillFromVES}</color>";
                    text.text = hasExistingSetBonus ? text.text + extraSkillText : extraSkillText;
                    bonustext.gameObject.SetActive(true);
                }
            }
        }
    }
}
