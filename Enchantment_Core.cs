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
using fastJSON;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;
using static kg.ValheimEnchantmentSystem.Enchantment_Core;

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

    public static class EquipmentEffectCache
    {
        public static ConditionalWeakTable<Player, Dictionary<string, float?>> EquippedValues = new ConditionalWeakTable<Player, Dictionary<string, float?>>();

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        public static class EquipmentEffectCache_Humanoid_UnequipItem_Patch
        {
            [UsedImplicitly]
            public static void Prefix(Humanoid __instance)
            {
                if (__instance is Player player)
                {
                    Reset(player);
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        public static class EquipmentEffectCache_Humanoid_EquipItem_Patch
        {
            [UsedImplicitly]
            public static void Prefix(Humanoid __instance)
            {
                if (__instance is Player player)
                {
                    Reset(player);
                }
            }
        }

        public static void Reset(Player player)
        {
            EquippedValues.Remove(player);
        }

        public static float? Get(Player player, string effect, Func<float?> calculate)
        {
            var values = EquippedValues.GetOrCreateValue(player);
            if (values.TryGetValue(effect, out float? value))
            {
                return value;
            }

            return values[effect] = calculate();
        }
    }

    public class Enchanted : ItemData
    {
        private Stat_Data cachedMultipliedStats;

        public int level {
            get
            {
                return enchantedItem.level;
            }
        }
        private int level_legacy; // for converting older version items
        public EnchantedItem enchantedItem = new EnchantedItem();
        public SyncedData.Stat_Data Stats
        {
            get
            {
                if (cachedMultipliedStats != null)
                {
                    return cachedMultipliedStats;
                }
                if (enchantedItem.GetTotalFloat() <= 0)
                {
                    if (level_legacy > enchantedItem.level)
                    {
                        enchantedItem.level = level_legacy;
                        level_legacy = 0;
                    }
                    Debug.LogWarning("effects empty, randomizing level: " + level);
                    if (enchantedItem.level <= 0)
                    {
                        enchantedItem.effects = new List<EnchantmentEffect>();
                    }
                    else
                    {
                        RandomizeAndSaveEnchantedItem();
                    }
                }
                Stat_Data baseStats = SyncedData.GetStatIncrease(this);
                if (baseStats == null)
                {
                    Debug.LogError("Failed to get base stats, check config");
                    cachedMultipliedStats = new Stat_Data();
                    return new Stat_Data();
                }
                cachedMultipliedStats = baseStats.ApplyMultiplier(enchantedItem);
                return cachedMultipliedStats;
            }
        }

        private void RandomizeAndSaveEnchantedItem(int bonusLineCount = 0)
        {
            cachedMultipliedStats = null;
            List<EnchantmentEffect> effects = SyncedData.GetRandomizedMultiplier(this, bonusLineCount);
            if (effects == null || effects.Count == 0)
            {
                effects = new List<EnchantmentEffect>();
                //cachedMultipliedStats = new Stat_Data();
                Debug.LogWarning("VES Failed to randomize, giving 0 empty floats");
            }
            enchantedItem.effects = effects;
            Save();
        }

        public override void Save()
        {
            Value = JsonConvert.SerializeObject(enchantedItem, Formatting.None);
            Enchantment_VFX.UpdateGrid();
        }

        public override void Load()
        {
            if (string.IsNullOrEmpty(Value)) return;
            try
            {
                enchantedItem = JsonConvert.DeserializeObject<EnchantedItem>(Value);
                Debug.Log("Loaded Item Level: " + enchantedItem?.level);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to load, trying deserialize legacy");
                Debug.LogWarning("Loading Legacy: " + Value);
                deserializeLegacy();
            }
        }

        private void deserializeLegacy()
        {
            if (string.IsNullOrEmpty(Value)) return;
            var parts = Value.Split('|');
            int.TryParse(parts[0], out level_legacy);
            enchantedItem = new EnchantedItem(level_legacy, new List<EnchantmentEffect>());
        }

        public int GetDestroyChance()
        {
            return SyncedData.GetEnchantmentChance(this).destroy;
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

        public bool CanReroll()
        {
            if (GetRerollChance() <= 0) return false;
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
            if (!CanReroll())
            {
                msg = "$enchantment_cannotbe".Localize();
                return false;
            }

            if (!HaveReqs(safeEnchant))
            {
                msg = "$enchantment_nomaterials".Localize();
                return false;
            }

            if (Random.Range(0f, 100f) <= GetRerollChance())
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

        public void EnchantReroll(int bonusLineCount = 0)
        {
            RandomizeAndSaveEnchantedItem(bonusLineCount);
            Other_Mods_APIs.ApplyAPIs(this);
            ValheimEnchantmentSystem._thistype.StartCoroutine(FrameSkipEquip(Item));
        }

        public void EnchantLevelUp()
        {
            enchantedItem.level++;
            EnchantReroll();
        }

        public void EnchantLevelDown()
        {
            enchantedItem.level = Mathf.Max(0, level - 1);
            EnchantReroll();
        }

        public static implicit operator bool(Enchanted en) => en != null;

        public string GenerateNameSuffix()
        {
            string suffix = "";
            string color = SyncedData.GetColor(this, out _, true)
                .IncreaseColorLight();

            if (!enchantedItem.effects.Any())
            {
                return $" (<color={color}>+{level}</color>)";
            }

            string asteriskText = GenerateAsteriskSuffix();
            suffix += $" (<color={color}>+{level}</color>{asteriskText})";

            return suffix;
        }

        public string GenerateAsteriskSuffix()
        {
            float sumOfFloats = enchantedItem.GetTotalFloat();

            int score = (int)Math.Round(Math.Max(sumOfFloats, 0), MidpointRounding.AwayFromZero);
            string symbols;
            if (score >= 10)
            {
                symbols = new string('◆', score - 9);
            }
            else
            {
                symbols = new string('*', score);
            }

            string color;
            if (score >= 10)
            {
                color = "#FF0000"; // Red
            }
            else if (score >= 8)
            {
                color = "#FFA500"; // Orange
            }
            else if (score >= 6)
            {
                color = "#CC00CC"; // Purple
            }
            else if (score >= 4)
            {
                color = "#4444FF"; // Blue
            }
            else if (score >= 2)
            {
                color = "#00FF00"; // Green
            }
            else
            {
                color = "#777777"; // Grey
            }

            string suffixTxt = $"<color={color}>{symbols}</color>";
            return suffixTxt;
        }
    }

    public static double ModifyAttackSpeed(Character c, double speed)
    {
        if (c != Player.m_localPlayer || !c.InAttack()) return speed;
        return speed * (1.0f + Player.m_localPlayer.GetTotalEnchantedValue("attack_speed") / 100f);
    }
}

public static partial class PlayerExtension
{
    public static List<EnchantmentEffect> GetEnchantedEffects(this Player player, string effectType = null)
    {
        var equipEffects = player.EquippedEnchantments()
            .Where(en => en.enchantedItem.level > 0)
            .SelectMany(en =>
            {
                float baseValue = SyncedData.GetStatIncrease(en, effectType);
                return en.enchantedItem.effects
                    .Where(effect => effectType == null || effect.name == effectType)
                    .Select(effect => new EnchantmentEffect(effect.name, effect.value * baseValue));
            });

        return equipEffects.ToList();
    }

    public static float GetTotalEnchantedValue(this Player player, string effectType)
    {
        var totalValue = EquipmentEffectCache.Get(player, effectType, () =>
        {
            var allEffects = player.GetEnchantedEffects(effectType);
            return allEffects.Count > 0 ? allEffects.Select(effect => effect.value).Sum() : null;
        }) ?? 0;
        return totalValue;
    }

    // 20% = 1.2x
    // 40% = 1.4x
    // 20% + 20% = 1.44x
    // 20% + 20% + 20% = 1.728x
    // Extremely powerful if stacked
    public static float GetTotalEnchantedMultiplierIncreaseMultiplicative(this Player player, string effectType)
    {
        var totalValue = EquipmentEffectCache.Get(player, effectType, () =>
        {
            var allEffects = player.GetEnchantedEffects(effectType);
            return allEffects.Count > 0 ? allEffects.Aggregate(1f, (total, effect) => total * (1 + effect.value / 100)) : (float?)null;
        }) ?? 1f;
        return totalValue;
    }

    // 20% = 0.8x
    // 40% = 0.6x
    // 20% + 20% = 0.64x
    // 20% + 20% + 20% = 0.512x
    // Good for preventing stackable bonuses from going negative
    public static float GetTotalEnchantedMultiplierDecreaseMultiplicative(this Player player, string effectType)
    {
        var totalValue = EquipmentEffectCache.Get(player, effectType, () =>
        {
            var allEffects = player.GetEnchantedEffects(effectType);
            return allEffects.Count > 0 ? allEffects.Aggregate(1f, (total, effect) => total * (1 - effect.value / 100)) : (float?)null;
        }) ?? 1f;
        return totalValue;
    }
}