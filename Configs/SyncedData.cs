using System.Text;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using ServerSync;
using AutoISP;
using static PrivilegeManager;
using UnityEngine;
using ItemManager;
using fastJSON;
using static fastJSON.Reflection;

namespace kg.ValheimEnchantmentSystem.Configs;

[VES_Autoload(VES_Autoload.Priority.First)]
public static class SyncedData
{
    private static FileSystemWatcher FSW;
    private static string YAML_Chances;
    private static string YAML_Stats_Weapons;
    private static string YAML_Stats_Armor;
    private static string YAML_Colors;
    private static string YAML_Reqs;

    private static string Directory_Overrides_Chances;
    private static string Directory_Overrides_Stats;
    private static string Directory_Overrides_Colors;
    private static string Directory_Reqs;
    

    private static readonly Dictionary<string, Action> FSW_Mapper = new();
    
    [UsedImplicitly]
    private static void OnInit()
    {
        SafetyLevel = ValheimEnchantmentSystem.config("Enchantment", "SafetyLevel", 3,
            "The level until which enchantments won't destroy the item. Set to 0 to disable.");
        DropEnchantmentOnUpgrade = ValheimEnchantmentSystem.config("Enchantment", "DropEnchantmentOnUpgrade", false, "Drop enchantment on item upgrade.");
        ItemFailureType = ValheimEnchantmentSystem.config("Enchantment", "ItemFailureType", ItemDesctructionTypeEnum.LevelDecrease, "LevelDecrease will remove one level on fail, Destroy will destroy item on fail, Combined will use yaml destroy chance and success chance, CombinedEasy will keep or decrease level and never destroy");
        AllowJewelcraftingMirrorCopyEnchant = ValheimEnchantmentSystem.config("Enchantment", "AllowJewelcraftingMirrorCopyEnchant", false, "Allow jewelcrafting to copy enchantment from one item to another using mirror.");
        AdditionalEnchantmentChancePerLevel = ValheimEnchantmentSystem.config("Enchantment", "AdditionalEnchantmentChancePerLevel", 0.06f, "Additional enchantment chance per level of Enchantment skill.");
        AllowVFXArmor = ValheimEnchantmentSystem.config("Enchantment", "AllowVFXArmor", false, "Allow VFX on armor.");
        EnchantmentEnableNotifications = ValheimEnchantmentSystem.config("Notifications", "EnchantmentEnableNotifications", true, "Enable enchantment notifications.");
        EnchantmentNotificationMinLevel = ValheimEnchantmentSystem.config("Notifications", "EnchantmentNotificationMinLevel", 6, "The minimum level of enchantment to show notification.");

        YAML_Stats_Weapons = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "EnchantmentStats_Weapons.yml");
        YAML_Stats_Armor = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "EnchantmentStats_Armor.yml");
        YAML_Colors = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "EnchantmentColors.yml");
        YAML_Reqs = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "EnchantmentReqs.yml");
        YAML_Chances = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "EnchantmentChancesV2.yml");
        Directory_Reqs = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "AdditionalEnchantmentReqs");
        Directory_Overrides_Chances = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "AdditionalOverrides_EnchantmentChances");
        Directory_Overrides_Stats = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "AdditionalOverrides_EnchantmentStats");
        Directory_Overrides_Colors = Path.Combine(ValheimEnchantmentSystem.ConfigFolder, "AdditionalOverrides_EnchantmentColors");
        
        if (!Directory.Exists(Directory_Reqs))
            Directory.CreateDirectory(Directory_Reqs);
        if (!Directory.Exists(Directory_Overrides_Chances))
            Directory.CreateDirectory(Directory_Overrides_Chances);
        if (!Directory.Exists(Directory_Overrides_Stats))
            Directory.CreateDirectory(Directory_Overrides_Stats);
        if (!Directory.Exists(Directory_Overrides_Colors))
            Directory.CreateDirectory(Directory_Overrides_Colors);
        

        if (!File.Exists(YAML_Chances))
            YAML_Chances.WriteFile(Defaults.YAML_Chances);
        if (!File.Exists(YAML_Stats_Weapons))
            YAML_Stats_Weapons.WriteFile(Defaults.YAML_Stats_Weapons);
        if (!File.Exists(YAML_Stats_Armor))
            YAML_Stats_Armor.WriteFile(Defaults.YAML_Stats_Armor);
        if (!File.Exists(YAML_Colors))
            YAML_Colors.WriteFile(Defaults.YAML_Colors);
        if (!File.Exists(YAML_Reqs))
            YAML_Reqs.WriteFile(Defaults.YAML_Reqs);

        Synced_EnchantmentChances.ValueChanged += ResetInventory;
        Synced_EnchantmentStats_Weapons.ValueChanged += ResetInventory;
        Synced_EnchantmentStats_Armor.ValueChanged += ResetInventory;
        Synced_EnchantmentColors.ValueChanged += ResetInventory;
        Synced_EnchantmentReqs.ValueChanged += ResetInventory;
        Overrides_EnchantmentChances.ValueChanged += ResetInventory;
        Overrides_EnchantmentStats.ValueChanged += ResetInventory;
        Overrides_EnchantmentColors.ValueChanged += ResetInventory;
        
        Overrides_EnchantmentChances.ValueChanged += OptimizeChances;
        Overrides_EnchantmentStats.ValueChanged += OptimizeStats;
        Overrides_EnchantmentColors.ValueChanged += OptimizeColors;
        
        Synced_EnchantmentChances.Value = YAML_Chances.FromYAML<Dictionary<int, Chance_Data>>();
        Synced_EnchantmentStats_Weapons.Value = YAML_Stats_Weapons.FromYAML<Dictionary<int, Stat_Data>>();
        Synced_EnchantmentStats_Armor.Value = YAML_Stats_Armor.FromYAML<Dictionary<int, Stat_Data>>();
        Synced_EnchantmentColors.Value = YAML_Colors.FromYAML<Dictionary<int, VFX_Data>>();
        ReadReqs();
        ReadOverrideChances();
        ReadOverrideStats();
        ReadOverrideColors();
        OptimizeChances();
        OptimizeStats();
        OptimizeColors();
        
        FSW_Mapper.Add(YAML_Chances, () => Synced_EnchantmentChances.Value = YAML_Chances.FromYAML<Dictionary<int, Chance_Data>>());
        FSW_Mapper.Add(YAML_Stats_Weapons, () => Synced_EnchantmentStats_Weapons.Value = YAML_Stats_Weapons.FromYAML<Dictionary<int, Stat_Data>>());
        FSW_Mapper.Add(YAML_Stats_Armor, () => Synced_EnchantmentStats_Armor.Value = YAML_Stats_Armor.FromYAML<Dictionary<int, Stat_Data>>());
        FSW_Mapper.Add(YAML_Colors, () => Synced_EnchantmentColors.Value = YAML_Colors.FromYAML<Dictionary<int, VFX_Data>>());
        FSW_Mapper.Add(YAML_Reqs, ReadReqs);
        FSW_Mapper.Add(ValheimEnchantmentSystem.SyncedConfig.ConfigFilePath, () => ValheimEnchantmentSystem.SyncedConfig.Reload());
        FSW_Mapper.Add(ValheimEnchantmentSystem.ItemConfig.ConfigFilePath, () => ValheimEnchantmentSystem.ItemConfig.Reload());
        FSW_Mapper.Add(Directory_Reqs, ReadReqs);
        FSW_Mapper.Add(Directory_Overrides_Chances, ReadOverrideChances);
        FSW_Mapper.Add(Directory_Overrides_Stats, ReadOverrideStats);
        FSW_Mapper.Add(Directory_Overrides_Colors, ReadOverrideColors);
        FSW = new FileSystemWatcher(ValheimEnchantmentSystem.ConfigFolder)
        {
            EnableRaisingEvents = true,
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite,
            SynchronizingObject = ThreadingHelper.SynchronizingObject
        };
        FSW.Changed += ConfigChanged;
    }
    private static void OptimizeChances()
    {
        OPTIMIZED_Overrides_EnchantmentChances.Clear();
        foreach (OverrideChances chance in Overrides_EnchantmentChances.Value)
            foreach (string entry in chance.Items)
                 OPTIMIZED_Overrides_EnchantmentChances[entry] = chance.Chances;
    }
    private static void OptimizeColors()
    {
        OPTIMIZED_Overrides_EnchantmentColors.Clear();
        foreach (OverrideColors chance in Overrides_EnchantmentColors.Value)
            foreach (string entry in chance.Items)
                OPTIMIZED_Overrides_EnchantmentColors[entry] = chance.Colors;
    }
    private static void OptimizeStats()
    {
        OPTIMIZED_Overrides_EnchantmentStats.Clear();
        foreach (OverrideStats chance in Overrides_EnchantmentStats.Value)
            foreach (string entry in chance.Items)
                OPTIMIZED_Overrides_EnchantmentStats[entry] = chance.Stats;
    }
    private static void ReadReqs()
    {
        List<EnchantmentReqs> result = new();
        if(YAML_Reqs.FromYAML<List<EnchantmentReqs>>() is { } yamlData)
            result.AddRange(yamlData);
        
        foreach (string file in Directory.GetFiles(Directory_Reqs, "*.yml", SearchOption.TopDirectoryOnly))
            if (file.FromYAML<List<EnchantmentReqs>>() is { } data)
                result.AddRange(data);
        
        Synced_EnchantmentReqs.Value = result;
    }
    private static void ReadOverrideChances()
    {
        List<OverrideChances> result = new();

        foreach (string file in Directory.GetFiles(Directory_Overrides_Chances, "*.yml", SearchOption.TopDirectoryOnly))
            if (file.FromYAML<List<OverrideChances>>() is {} data)
                result.AddRange(data);

        Overrides_EnchantmentChances.Value = result;
    }
    private static void ReadOverrideStats()
    {
        List<OverrideStats> result = new();

        foreach (string file in Directory.GetFiles(Directory_Overrides_Stats, "*.yml", SearchOption.TopDirectoryOnly))
            if (file.FromYAML<List<OverrideStats>>() is {} data)
                result.AddRange(data);

        Overrides_EnchantmentStats.Value = result;
    }
    private static void ReadOverrideColors()
    {
        List<OverrideColors> result = new();

        foreach (string file in Directory.GetFiles(Directory_Overrides_Colors, "*.yml", SearchOption.TopDirectoryOnly))
            if (file.FromYAML<List<OverrideColors>>() is {} data)
                result.AddRange(data);

        Overrides_EnchantmentColors.Value = result;
    }
    
    private static void ResetInventory()
    {
        Enchantment_VFX.UpdateGrid();
    }

    private static DateTime LastConfigChange = DateTime.Now;
    private static void ConfigChanged(object sender, FileSystemEventArgs e)
    {
        if (!Game.instance || !ZNet.instance || !ZNet.instance.IsServer()) return;
        if (e.ChangeType != WatcherChangeTypes.Changed) return;
        string extention = Path.GetExtension(e.FullPath);
        if (extention != ".yml" && extention != ".cfg") return;
        if (FSW_Mapper.TryGetValue(e.FullPath, out Action action))
        {
            if (DateTime.Now - LastConfigChange < TimeSpan.FromSeconds(3)) return;
            LastConfigChange = DateTime.Now;
            try
            {
                Utils.print($"Reloading config {e.FullPath}");
                action.Invoke();
            } 
            catch (Exception ex)
            {
                Utils.print($"Error while reloading config {e.FullPath}: {ex}", ConsoleColor.Red); 
            }
            return;
        }
        string folder = Path.GetDirectoryName(e.FullPath);
        if (folder == null) return;
        if (FSW_Mapper.TryGetValue(folder, out action))
        {
            if (DateTime.Now - LastConfigChange < TimeSpan.FromSeconds(3)) return;
            LastConfigChange = DateTime.Now;
            try
            {
                Utils.print($"Reloading config {e.FullPath}");
                action.Invoke();
            }
            catch (Exception ex)
            {
                Utils.print($"Error while reloading config {e.FullPath}: {ex}", ConsoleColor.Red);
            }
        }
    }

    public static string GetColor(Enchantment_Core.Enchanted en, out int variant, bool trimApha) =>
        GetColor(en.Item.m_dropPrefab?.name, en.level, out variant, trimApha);

    public static string GetColor(string dropPrefab, int level, out int variant, bool trimApha, string defaultValue = "#00000000")
    {
        variant = 0;
        if (level == 0) return trimApha ? defaultValue.Substring(0,7) : defaultValue;
        if (dropPrefab != null && OPTIMIZED_Overrides_EnchantmentColors.TryGetValue(dropPrefab, out Dictionary<int, VFX_Data> overriden))
        {
            if (overriden.TryGetValue(level, out VFX_Data overrideVfxData))
            {
                string result = overrideVfxData.color;
                if (trimApha) result = result.Substring(0, 7);
                variant = Mathf.Clamp(overrideVfxData.variant, 0, Enchantment_VFX.VFXs.Count - 1);
                return result;
            }
        }
        
        if (Synced_EnchantmentColors.Value.TryGetValue(level, out VFX_Data vfxData))
        {
            string result = vfxData.color;
            if (trimApha) result = result.Substring(0, 7);
            variant = Mathf.Clamp(vfxData.variant - 1, 0, Enchantment_VFX.VFXs.Count - 1);
            return result;
        }

        return trimApha ? defaultValue.Substring(0,7) : defaultValue;
    }

    public static Chance_Data GetPrevEnchantmentChance(Enchantment_Core.Enchanted en)
        => GetEnchantmentChance(en.Item.m_dropPrefab?.name, en.level - 1);

    public static Chance_Data GetEnchantmentChance(Enchantment_Core.Enchanted en)
        => GetEnchantmentChance(en.Item.m_dropPrefab?.name, en.level);

    private static Chance_Data GetEnchantmentChance(string dropPrefab, int level)
    {
        Chance_Data chanceData = new Chance_Data() { success = 0, destroy = 0, reroll = 100 }; // Default values

        if (level == 0)
        {
            chanceData.success = 100;
            return chanceData;
        }

        if (dropPrefab != null && OPTIMIZED_Overrides_EnchantmentChances.TryGetValue(dropPrefab, out Dictionary<int, Chance_Data> overriden))
        {
            if (overriden.TryGetValue(level, out Chance_Data overrideChance))
            {
                chanceData.success = overrideChance.success;
                chanceData.destroy = overrideChance.destroy;
                chanceData.reroll = overrideChance.reroll != 0 ? overrideChance.reroll : 100;
                return chanceData;
            }
        }

        if (Synced_EnchantmentChances.Value.TryGetValue(level, out Chance_Data syncedChance))
        {
            chanceData.success = syncedChance.success;
            chanceData.destroy = syncedChance.destroy;
            chanceData.reroll = syncedChance.reroll != 0 ? syncedChance.reroll : 100;
            return chanceData;
        }

        return chanceData;
    }

    public static Stat_Data GetStatIncrease(Enchantment_Core.Enchanted en)
    {
        if (en == null || en.enchantedItem == null || en.enchantedItem.level == 0) return null;
        int level = en.enchantedItem.level;

        string dropPrefab = en.Item.m_dropPrefab?.name;
        if (dropPrefab != null && OPTIMIZED_Overrides_EnchantmentStats.TryGetValue(dropPrefab, out Dictionary<int, Stat_Data> overriden))
        {
            return overriden.TryGetValue(level, out Stat_Data overrideChance) ? overrideChance : null;
        }

        Dictionary<int, Stat_Data> target = en.Item.IsWeapon() ? Synced_EnchantmentStats_Weapons.Value : Synced_EnchantmentStats_Armor.Value;
        return target.TryGetValue(level, out Stat_Data increase) ? increase : null;
    }

    public static float GetStatIncrease(Enchantment_Core.Enchanted en, string effectType)
    {
        var field = typeof(Stat_Data).GetField(effectType, BindingFlags.Public | BindingFlags.Instance);
        var baseStats = SyncedData.GetStatIncrease(en);
        return field != null && baseStats != null ? Convert.ToSingle(field.GetValue(baseStats)) : 0f;
    }

    public static List<EnchantmentEffect> GetRandomizedMultiplier(Enchantment_Core.Enchanted en)
    {
        if (en?.enchantedItem?.level <= 0)
        {
            Debug.LogWarning("VES No floats because item null or lv0");
            return new List<EnchantmentEffect>();
        }
        var allStats = GetStatIncrease(en);
        if (allStats == null)
        {
            Debug.LogError("VES No possible stats found while randomizing, check your EnchantmentStats config yml");
            return new List<EnchantmentEffect>();
        }
        var selectedStats = new Stat_Data();
        var multipliers = new List<EnchantmentEffect>();
        var possibleFields = typeof(Stat_Data).GetFields(BindingFlags.Public | BindingFlags.Instance)
                                   .Where(f => f.FieldType == typeof(int) || f.FieldType == typeof(float))
                                   .Where(f => Convert.ToDouble(f.GetValue(allStats)) != 0)
                                   .ToList();

        var lineCount = 2 + (en.level / 4); // Base + Level
        while (UnityEngine.Random.value <= 0.5 && lineCount < possibleFields.Count) // Extra by chance
        {
            lineCount++;
        }

        // Apply pity system
        int oldLineCount = en.enchantedItem.effects.Count;
        lineCount = Mathf.Clamp(lineCount, oldLineCount - 1, possibleFields.Count);

        // 30% 0.5
        // 30% 0.75
        // 30% 1.0
        // 3.33% 1.0
        // 3.33% 1.5
        // 3.33% 2.0
        float minMult = 0.5f;
        float maxMult = 1.0f;
        float interval = 0.25f;
        float bonusMultiplierChance = 0.1f;
        float bonusMultiplier = 2.0f;

        var randomFields = possibleFields.OrderBy(f => UnityEngine.Random.value).Take(lineCount).ToList();
        foreach (var field in randomFields)
        {
            var originalValue = Convert.ToDouble(field.GetValue(allStats));
            float floatMultiplier = Mathf.Round(UnityEngine.Random.Range(minMult / interval, maxMult / interval)) * interval;
            if (UnityEngine.Random.value <= bonusMultiplierChance) // 10% chance to get double bonus
            {
                floatMultiplier *= bonusMultiplier;
            }

            multipliers.Add(new EnchantmentEffect(field.Name, floatMultiplier));
        }

        return multipliers;
    }

    public static EnchantmentReqs GetReqs(string prefab)
    {
        return prefab == null ? null : Synced_EnchantmentReqs.Value.Find(x => x.Items.Contains(prefab));
    }

    public static float GetAdditionalEnchantmentChance()
    {
        if (!Player.m_localPlayer) return 0;
        float enchantmentLevel = Player.m_localPlayer.GetSkillLevel(Enchantment_Skill.SkillType_Enchantment);
        return enchantmentLevel * AdditionalEnchantmentChancePerLevel.Value;
    }

    public enum ItemDesctructionTypeEnum{ LevelDecrease, Destroy, Combined, CombinedEasy }
    
    public static ConfigEntry<int> SafetyLevel;
    public static ConfigEntry<bool> DropEnchantmentOnUpgrade;
    public static ConfigEntry<ItemDesctructionTypeEnum> ItemFailureType;
    public static ConfigEntry<bool> AllowJewelcraftingMirrorCopyEnchant;
    public static ConfigEntry<float> AdditionalEnchantmentChancePerLevel;
    public static ConfigEntry<int> EnchantmentNotificationMinLevel;
    public static ConfigEntry<bool> EnchantmentEnableNotifications;
    public static ConfigEntry<bool> AllowVFXArmor;

    public static readonly CustomSyncedValue<Dictionary<int, Chance_Data>> Synced_EnchantmentChances =
        new(ValheimEnchantmentSystem.ConfigSync, "EnchantmentGlobalChances",
            new Dictionary<int, Chance_Data>());

    public static readonly CustomSyncedValue<Dictionary<int, VFX_Data>> Synced_EnchantmentColors =
        new(ValheimEnchantmentSystem.ConfigSync, "OverridenEnchantmentColors",
            new Dictionary<int, VFX_Data>());

    public static readonly CustomSyncedValue<Dictionary<int, Stat_Data>> Synced_EnchantmentStats_Weapons =
        new(ValheimEnchantmentSystem.ConfigSync, "EnchantmentStats_Weapons",
            new Dictionary<int, Stat_Data>());
    
    public static readonly CustomSyncedValue<Dictionary<int, Stat_Data>> Synced_EnchantmentStats_Armor =
        new(ValheimEnchantmentSystem.ConfigSync, "EnchantmentStats_Armor",
            new Dictionary<int, Stat_Data>());

    public static readonly CustomSyncedValue<List<OverrideChances>> Overrides_EnchantmentChances =
        new(ValheimEnchantmentSystem.ConfigSync, "Overrides_EnchantmentChances",
            new());

    public static readonly CustomSyncedValue<List<OverrideColors>> Overrides_EnchantmentColors =
            new(ValheimEnchantmentSystem.ConfigSync, "Overrides_EnchantmentColors",
                new());

    public static readonly CustomSyncedValue<List<OverrideStats>> Overrides_EnchantmentStats =
            new(ValheimEnchantmentSystem.ConfigSync, "Overrides_EnchantmentStats",
                new());

    public static readonly CustomSyncedValue<List<EnchantmentReqs>> Synced_EnchantmentReqs =
        new(ValheimEnchantmentSystem.ConfigSync, "EnchantmentReqs",
            new List<EnchantmentReqs>());

    private static readonly Dictionary<string, Dictionary<int, Chance_Data>> OPTIMIZED_Overrides_EnchantmentChances = new();
    private static readonly Dictionary<string, Dictionary<int, VFX_Data>> OPTIMIZED_Overrides_EnchantmentColors = new();
    private static readonly Dictionary<string, Dictionary<int, Stat_Data>> OPTIMIZED_Overrides_EnchantmentStats = new();

    private static List<FieldInfo> _stat_Data_Cached_Fields = AccessTools.GetDeclaredFields(typeof(Stat_Data)).Where(x => x.FieldType.IsValueType).ToList();
    public partial class Stat_Data
    {
        private bool ShouldShow() => _stat_Data_Cached_Fields.Any(x => !x.GetValue(this).Equals(Activator.CreateInstance(x.FieldType)));
        private List<HitData.DamageModPair> cached_resistance_pairs;
        public List<HitData.DamageModPair> GetResistancePairs()
        {
            if (cached_resistance_pairs != null) return cached_resistance_pairs;
            cached_resistance_pairs = new()
            {
                new() { m_type = HitData.DamageType.Blunt, m_modifier = resistance_blunt },
                new() { m_type = HitData.DamageType.Slash, m_modifier = resistance_slash },
                new() { m_type = HitData.DamageType.Pierce, m_modifier = resistance_pierce },
                new() { m_type = HitData.DamageType.Chop, m_modifier = resistance_chop },
                new() { m_type = HitData.DamageType.Pickaxe, m_modifier = resistance_pickaxe },
                new() { m_type = HitData.DamageType.Fire, m_modifier = resistance_fire },
                new() { m_type = HitData.DamageType.Frost, m_modifier = resistance_frost },
                new() { m_type = HitData.DamageType.Lightning, m_modifier = resistance_lightning },
                new() { m_type = HitData.DamageType.Poison, m_modifier = resistance_poison },
                new() { m_type = HitData.DamageType.Spirit, m_modifier = resistance_spirit },
            };
            cached_resistance_pairs.RemoveAll(x => x.m_modifier == HitData.DamageModifier.Normal);
            return cached_resistance_pairs;
        }

        private string cached_tooltip;
        public string BuildAdditionalStats(string color)
        {
            if (cached_tooltip != null) return cached_tooltip;
            if (!ShouldShow())
            {
                cached_tooltip = "\n";
                return cached_tooltip;
            }
            StringBuilder builder = new StringBuilder();
            if (damage_percentage > 0) builder.Append($"\n<color={color}>•</color> $enchantment_bonusespercentdamage: +{damage_percentage}%");
            if (damage_true > 0) builder.Append($"\n<color={color}>•</color> $enchantment_truedamage: +{damage_true}");
            if (damage_true_percentage > 0) builder.Append($"\n<color={color}>•</color> $enchantment_truedamage: +{damage_true_percentage}%");
            if (damage_fire > 0) builder.Append($"\n<color={color}>•</color> $inventory_fire: <color=#FFA500>+{damage_fire}</color>");
            if (damage_fire_percentage > 0) builder.Append($"\n<color={color}>•</color> $inventory_fire: <color=#FFA500>+{damage_fire_percentage}%</color>");
            if (damage_blunt > 0) builder.Append($"\n<color={color}>•</color> $inventory_blunt: <color=#FFFF00>+{damage_blunt}</color>");
            if (damage_blunt_percentage > 0) builder.Append($"\n<color={color}>•</color> $inventory_blunt: <color=#FFFF00>+{damage_blunt_percentage}%</color>");
            if (damage_slash > 0) builder.Append($"\n<color={color}>•</color> $inventory_slash: <color=#7F00FF>+{damage_slash}</color>");
            if (damage_slash_percentage > 0) builder.Append($"\n<color={color}>•</color> $inventory_slash: <color=#7F00FF>+{damage_slash_percentage}%</color>");
            if (damage_pierce > 0) builder.Append($"\n<color={color}>•</color> $inventory_pierce: <color=#D499B9>+{damage_pierce}</color>");
            if (damage_pierce_percentage > 0) builder.Append($"\n<color={color}>•</color> $inventory_pierce: <color=#D499B9>+{damage_pierce_percentage}%</color>");
            if (damage_chop > 0) builder.Append($"\n<color={color}>•</color> $enchantment_chopdamage: <color=#FFAF00>+{damage_chop}</color>");
            if (damage_chop_percentage > 0) builder.Append($"\n<color={color}>•</color> $enchantment_chopdamage: <color=#FFAF00>+{damage_chop_percentage}%</color>");
            if (damage_pickaxe > 0) builder.Append($"\n<color={color}>•</color> $enchantment_pickaxedamage: <color=#FF00FF>+{damage_pickaxe}</color>");
            if (damage_pickaxe_percentage > 0) builder.Append($"\n<color={color}>•</color> $enchantment_pickaxedamage: <color=#FF00FF>+{damage_pickaxe_percentage}%</color>");
            if (damage_frost > 0) builder.Append($"\n<color={color}>•</color> $inventory_frost: <color=#00FFFF>+{damage_frost}</color>");
            if (damage_frost_percentage > 0) builder.Append($"\n<color={color}>•</color> $inventory_frost: <color=#00FFFF>+{damage_frost_percentage}%</color>");
            if (damage_lightning > 0) builder.Append($"\n<color={color}>•</color> $inventory_lightning: <color=#0000FF>+{damage_lightning}</color>");
            if (damage_lightning_percentage > 0) builder.Append($"\n<color={color}>•</color> $inventory_lightning: <color=#0000FF>+{damage_lightning_percentage}%</color>");
            if (damage_poison > 0) builder.Append($"\n<color={color}>•</color> $inventory_poison: <color=#00FF00>+{damage_poison}</color>");
            if (damage_poison_percentage > 0) builder.Append($"\n<color={color}>•</color> $inventory_poison: <color=#00FF00>+{damage_poison_percentage}%</color>");
            if (damage_spirit > 0) builder.Append($"\n<color={color}>•</color> $inventory_spirit: <color=#FFFFA0>+{damage_spirit}</color>");
            if (damage_spirit_percentage > 0) builder.Append($"\n<color={color}>•</color> $inventory_spirit: <color=#FFFFA0>+{damage_spirit_percentage}%</color>");
            if (attack_speed > 0) builder.Append($"\n<color={color}>•</color> $enchantment_attackspeed: <color=#DF745D>+{attack_speed}%</color>");
            if (movement_speed > 0) builder.Append($"\n<color={color}>•</color> $enchantment_movementspeed: <color=#DF745D>+{movement_speed}%</color>");
            if (weapon_skill > 0) builder.Append($"\n<color={color}>•</color> $enchantment_matching_weapon_skill: <color=#FFA500>+{weapon_skill}</color>");
            if (armor > 0) builder.Append($"\n<color={color}>•</color> $item_armor: <color=#808080>+{armor}</color>");
            if (armor_percentage > 0) builder.Append($"\n<color={color}>•</color> $enchantment_bonusespercentarmor: <color=#808080>+{armor_percentage}%</color>");
            if (durability > 0) builder.Append($"\n<color={color}>•</color> $item_durability: <color=#7393B3>+{durability}</color>");
            if (durability_percentage > 0) builder.Append($"\n<color={color}>•</color> $item_durability: <color=#7393B3>+{durability_percentage}%</color>");
            if (max_hp > 0) builder.Append($"\n<color={color}>•</color> $se_health: <color=#ff8080ff>+{max_hp}</color>");
            if (hp_regen > 0) builder.Append($"\n<color={color}>•</color> $se_healthregen: <color=#ff8080ff>+{hp_regen}/10s</color>");
            if (max_stamina > 0) builder.Append($"\n<color={color}>•</color> $se_stamina: <color=#ffff80ff>+{max_stamina}</color>");
            if (stamina_regen > 0) builder.Append($"\n<color={color}>•</color> $se_staminaregen: <color=#ffff80ff>+{stamina_regen}/s</color>");
            if (stamina_regen_percentage > 0) builder.Append($"\n<color={color}>•</color> $se_staminaregen: <color=#ffff80ff>+{stamina_regen_percentage}%</color>");
            if (stamina_use_reduction_percent > 0) builder.Append($"\n<color={color}>•</color> $item_staminause: <color=#ffff80ff>-{stamina_use_reduction_percent}%</color>");
            if (max_eitr > 0) builder.Append($"\n<color={color}>•</color> $item_food_eitr: <color=#9090ffff>+{max_eitr}</color>");
            if (eitr_regen_percentage > 0) builder.Append($"\n<color={color}>•</color> $item_eitrregen_modifier: <color=#9090ffff>+{eitr_regen_percentage}%</color>");
            if (API_backpacks_additionalrow_x > 0) builder.Append($"\n<color={color}>•</color> $enchantment_backpacks_additionalrow_x: <color=#7393B3>{API_backpacks_additionalrow_x}</color>");
            if (API_backpacks_additionalrow_y > 0) builder.Append($"\n<color={color}>•</color> $enchantment_backpacks_additionalrow_x: <color=#7393B3>{API_backpacks_additionalrow_y}</color>");
            
            builder.Append(SE_Stats.GetDamageModifiersTooltipString(GetResistancePairs()).Replace("\n", $"\n<color={color}>•</color> "));
            
            builder.Append("\n");
            cached_tooltip = builder.ToString();
            return cached_tooltip;
        }

        public string Info_Description()
        {
            string result = "";
            if (damage_percentage > 0)
            {
                result += $"\n• $enchantment_bonusespercentdamage: <color=#AF009F>+{damage_percentage}%</color>";
            }
            if (armor_percentage > 0)
            {
                result += $"\n• $enchantment_bonusespercentarmor: <color=#009FAF>+{armor_percentage}%</color>";
            }
            result += BuildAdditionalStats("#FFFFFF");
            return result;
        }
    }
    
    [AutoSerialize]
    public partial class Stat_Data : ImplicitBool, ISerializableParameter
    {
        [SerializeField] public int durability;
        [SerializeField] public int durability_percentage;
        [SerializeField] public int armor_percentage;
        [SerializeField] public int armor;
        [SerializeField] public int damage_percentage;
        [SerializeField] public int damage_true;
        [SerializeField] public int damage_blunt;
        [SerializeField] public int damage_slash;
        [SerializeField] public int damage_pierce;
        [SerializeField] public int damage_chop;
        [SerializeField] public int damage_pickaxe;
        [SerializeField] public int damage_fire;
        [SerializeField] public int damage_frost;
        [SerializeField] public int damage_lightning;
        [SerializeField] public int damage_poison;
        [SerializeField] public int damage_spirit;
        [SerializeField] public int damage_true_percentage;
        [SerializeField] public int damage_blunt_percentage;
        [SerializeField] public int damage_slash_percentage;
        [SerializeField] public int damage_pierce_percentage;
        [SerializeField] public int damage_chop_percentage;
        [SerializeField] public int damage_pickaxe_percentage;
        [SerializeField] public int damage_fire_percentage;
        [SerializeField] public int damage_frost_percentage;
        [SerializeField] public int damage_lightning_percentage;
        [SerializeField] public int damage_poison_percentage;
        [SerializeField] public int damage_spirit_percentage;
        [SerializeField] public HitData.DamageModifier resistance_blunt;
        [SerializeField] public HitData.DamageModifier resistance_slash;
        [SerializeField] public HitData.DamageModifier resistance_pierce;
        [SerializeField] public HitData.DamageModifier resistance_chop;
        [SerializeField] public HitData.DamageModifier resistance_pickaxe;
        [SerializeField] public HitData.DamageModifier resistance_fire;
        [SerializeField] public HitData.DamageModifier resistance_frost;
        [SerializeField] public HitData.DamageModifier resistance_lightning;
        [SerializeField] public HitData.DamageModifier resistance_poison;
        [SerializeField] public HitData.DamageModifier resistance_spirit;
        [SerializeField] public int attack_speed;
        [SerializeField] public int movement_speed;
        [SerializeField] public int max_hp;
        [SerializeField] public int max_stamina;
        [SerializeField] public int max_eitr;
        [SerializeField] public int weapon_skill;
        [SerializeField] public int movement_skill;
        [SerializeField] public float hp_regen;
        [SerializeField] public float stamina_regen;
        [SerializeField] public int stamina_regen_percentage;
        [SerializeField] public int eitr_regen_percentage;
        [SerializeField] public int stamina_use_reduction_percent;
        //api stats
        [SerializeField] public int API_backpacks_additionalrow_x;
        [SerializeField] public int API_backpacks_additionalrow_y;
        
        public void Serialize  (ref ZPackage pkg) => throw new NotImplementedException();
        public void Deserialize(ref ZPackage pkg) => throw new NotImplementedException();

        public string SerializeJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static Stat_Data DeserializeJson(string json)
        {
            return JsonUtility.FromJson<Stat_Data>(json);
        }

        public Stat_Data ApplyMultiplier(EnchantedItem item)
        {
            var multipliedStats = new Stat_Data();
            if (item == null || item.effects == null) return multipliedStats;
            foreach (var field in typeof(Stat_Data).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var baseValue = Convert.ToDouble(field.GetValue(this));
                var multiplierField = typeof(Stat_Data).GetField(field.Name);
                if (multiplierField != null)
                {
                    var multiplier = item.GetTotalFloat(field.Name);
                    var newValue = baseValue * multiplier;
                    if (field.FieldType == typeof(int))
                    {
                        field.SetValue(multipliedStats, (int)Math.Round(newValue, MidpointRounding.AwayFromZero));
                    }
                    else if (field.FieldType == typeof(float))
                    {
                        field.SetValue(multipliedStats, (float)newValue);
                    }
                }
            }
            return multipliedStats;
        }
    }

    [AutoSerialize]
    public class Chance_Data : ImplicitBool, ISerializableParameter
    {
        [SerializeField] public int success;
        [SerializeField] public int destroy;
        [SerializeField] public int reroll;
        public void Serialize  (ref ZPackage pkg) => throw new NotImplementedException();
        public void Deserialize(ref ZPackage pkg) => throw new NotImplementedException();
    }
    
    [AutoSerialize]
    public class SingleReq : ImplicitBool, ISerializableParameter
    {
        [SerializeField] public string prefab;
        [SerializeField] public int amount;
        public SingleReq() { }
        public SingleReq (string prefab, int amount) { this.prefab = prefab; this.amount = amount; }
        public bool IsValid() => !string.IsNullOrEmpty(prefab) && amount > 0 && ZNetScene.instance.GetPrefab(prefab);
        public void Serialize  (ref ZPackage pkg) => throw new NotImplementedException();
        public void Deserialize(ref ZPackage pkg) => throw new NotImplementedException();
    }
    
    [AutoSerialize]
    public class EnchantmentReqs : ImplicitBool, ISerializableParameter
    {
        [SerializeField] public int required_skill = 0;
        [SerializeField] public SingleReq enchant_prefab = new();
        [SerializeField] public SingleReq blessed_enchant_prefab = new();
        [SerializeField] public List<string> Items = new();
        public void Serialize  (ref ZPackage pkg) => throw new NotImplementedException();
        public void Deserialize(ref ZPackage pkg) => throw new NotImplementedException();
    }

    [AutoSerialize]
    public class VFX_Data : ImplicitBool, ISerializableParameter
    {
        [SerializeField] public string color = "#00000000";
        [SerializeField] public int variant;
        public void Serialize  (ref ZPackage pkg) => throw new NotImplementedException();
        public void Deserialize(ref ZPackage pkg) => throw new NotImplementedException();
    }
    
    [AutoSerialize]
    public class OverrideChances : ImplicitBool, ISerializableParameter
    {
        [SerializeField] public List<string> Items = new();
        [SerializeField] public Dictionary<int, Chance_Data> Chances = new();
        public void Serialize  (ref ZPackage pkg) => throw new NotImplementedException();
        public void Deserialize(ref ZPackage pkg) => throw new NotImplementedException();
    }

    [AutoSerialize]
    public class OverrideColors : ImplicitBool, ISerializableParameter
    {
        [SerializeField] public List<string> Items = new();
        [SerializeField] public Dictionary<int, VFX_Data> Colors = new();
        public void Serialize  (ref ZPackage pkg) => throw new NotImplementedException();
        public void Deserialize(ref ZPackage pkg) => throw new NotImplementedException();
    }

    [AutoSerialize]
    public class OverrideStats : ImplicitBool, ISerializableParameter
    {
        [SerializeField] public List<string> Items = new();
        [SerializeField] public Dictionary<int, Stat_Data> Stats = new();
        public void Serialize  (ref ZPackage pkg) => throw new NotImplementedException();
        public void Deserialize(ref ZPackage pkg) => throw new NotImplementedException();
    }
    
}