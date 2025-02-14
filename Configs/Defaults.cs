﻿using YamlDotNet.Serialization;

namespace kg.ValheimEnchantmentSystem.Configs;

public static class Defaults
{
    private static readonly Dictionary<int, SyncedData.Stat_Data> DefaultStats_Weapons =
        new Dictionary<int, SyncedData.Stat_Data>()
        {
            { 1,  new() { damage_percentage = 2  } },
            { 2,  new() { damage_percentage = 4  } },
            { 3,  new() { damage_percentage = 6  } },
            { 4,  new() { damage_percentage = 8  } },
            { 5,  new() { damage_percentage = 10 } },
            { 6,  new() { damage_percentage = 12 } },
            { 7,  new() { damage_percentage = 16 } },
            { 8,  new() { damage_percentage = 20 } },
            { 9,  new() { damage_percentage = 24 } },
            { 10, new() { damage_percentage = 28 } },
            { 11, new() { damage_percentage = 32 } },
            { 12, new() { damage_percentage = 36 } },
            { 13, new() { damage_percentage = 40 } },
            { 14, new() { damage_percentage = 45 } },
            { 15, new() { damage_percentage = 50 } },
            { 16, new() { damage_percentage = 55 } },
            { 17, new() { damage_percentage = 60 } },
            { 18, new() { damage_percentage = 70 } },
            { 19, new() { damage_percentage = 85 } },
            { 20, new() { damage_percentage = 100 } },
        };

    private static readonly Dictionary<int, SyncedData.Stat_Data> DefaultStats_Armor =
        new Dictionary<int, SyncedData.Stat_Data>()
        {
            { 1,  new() { armor_percentage = 2  } },
            { 2,  new() { armor_percentage = 3  } },
            { 3,  new() { armor_percentage = 4  } },
            { 4,  new() { armor_percentage = 5  } },
            { 5,  new() { armor_percentage = 6  } },
            { 6,  new() { armor_percentage = 7  } },
            { 7,  new() { armor_percentage = 9  } },
            { 8,  new() { armor_percentage = 11 } },
            { 9,  new() { armor_percentage = 13 } },
            { 10, new() { armor_percentage = 15 } },
            { 11, new() { armor_percentage = 17 } },
            { 12, new() { armor_percentage = 19 } },
            { 13, new() { armor_percentage = 22 } },
            { 14, new() { armor_percentage = 25 } },
            { 15, new() { armor_percentage = 28 } },
            { 16, new() { armor_percentage = 31 } },
            { 17, new() { armor_percentage = 35 } },
            { 18, new() { armor_percentage = 39 } },
            { 19, new() { armor_percentage = 44 } },
            { 20, new() { armor_percentage = 50 } },
        };

    private static readonly Dictionary<int, SyncedData.Chance_Data> DefaultChances = new()
    {
        { 1, new SyncedData.Chance_Data() {success = 80} }, { 2, new SyncedData.Chance_Data() {success = 75} }, { 3, new SyncedData.Chance_Data() {success = 70} }, 
        { 4, new SyncedData.Chance_Data() {success = 60} }, { 5, new SyncedData.Chance_Data() {success = 55} }, { 6, new SyncedData.Chance_Data() {success = 50} },
        { 7, new SyncedData.Chance_Data() {success = 40} }, { 8, new SyncedData.Chance_Data() {success = 35} }, { 9, new SyncedData.Chance_Data() {success = 30} }, 
        { 10, new SyncedData.Chance_Data() {success = 26} }, { 11, new SyncedData.Chance_Data() {success = 22} }, { 12, new SyncedData.Chance_Data() {success = 18} }, 
        { 13, new SyncedData.Chance_Data() {success = 14} }, { 14, new SyncedData.Chance_Data() {success = 10} }, { 15, new SyncedData.Chance_Data() {success = 8} }, 
        { 16, new SyncedData.Chance_Data() {success = 6} }, { 17, new SyncedData.Chance_Data() {success = 5} }, { 18, new SyncedData.Chance_Data() {success = 4} }, 
        { 19, new SyncedData.Chance_Data() {success = 3} }
    };

    private static readonly Dictionary<int, SyncedData.VFX_Data> DefaultColors =
        new Dictionary<int, SyncedData.VFX_Data>
        {
            { 1,  new() { color = "#1E151C01", variant = 0 } },
            { 2,  new() { color = "#1E181F02", variant = 0 } },
            { 3,  new() { color = "#1E1A2A03", variant = 0 } },
            { 4,  new() { color = "#1E1E3AA6", variant = 0 } },
            { 5,  new() { color = "#1E1E4AB0", variant = 0 } },
            { 6,  new() { color = "#23415A9B", variant = 0 } },
            { 7,  new() { color = "#28577EA2", variant = 0 } },
            { 8,  new() { color = "#1E508EA9", variant = 0 } },
            { 9,  new() { color = "#14469EB0", variant = 0 } },
            { 10, new() { color = "#0A3CAFB7", variant = 0 } },
            { 11, new() { color = "#0038BFC0", variant = 0 } },
            { 12, new() { color = "#0038BFC0", variant = 0 } },
            { 13, new() { color = "#001CDBC4", variant = 0 } },
            { 14, new() { color = "#001CDBDB", variant = 0 } },
            { 15, new() { color = "#001CDFE2", variant = 0 } },
            { 16, new() { color = "#A0140EE9", variant = 0 } },
            { 17, new() { color = "#B40A0EF0", variant = 0 } },
            { 18, new() { color = "#C8000EF7", variant = 0 } },
            { 19, new() { color = "#D2000EFE", variant = 0 } },
            { 20, new() { color = "#FF000EFF", variant = 0 } }
        };

    private static readonly List<SyncedData.EnchantmentReqs> DefaultReqs = new()
    {
        new SyncedData.EnchantmentReqs()
        {
            enchant_prefab = new("kg_EnchantScroll_Weapon_S", 1),
            blessed_enchant_prefab = new("kg_EnchantScroll_Weapon_Blessed_S", 1), Items = new()
            {
                "AtgeirHimminAfl", "AxeJotunBane", "BowSpineSnap", "PickaxeBlackMetal", "ShieldCarapace",
                "ShieldCarapaceBuckler", "SledgeDemolisher", "SpearCarapace", 
                "StaffFireball", "StaffIceShards", "StaffShield", "StaffSkeleton", "SwordMistwalker", "THSwordKrom",
                "CrossbowArbalest", "SwordCheat", "KnifeSkollAndHati"
            }
        },
        new SyncedData.EnchantmentReqs()
        {
            enchant_prefab = new("kg_EnchantScroll_Armor_S", 1),
            blessed_enchant_prefab = new("kg_EnchantScroll_Armor_Blessed_S", 1), Items = new()
            {
                "ArmorCarapaceChest", "ArmorCarapaceLegs", "ArmorMageChest", "ArmorMageLegs", "CapeFeather",
                "HelmetCarapace", "HelmetMage"
            }
        },
        new SyncedData.EnchantmentReqs()
        {
            enchant_prefab = new("kg_EnchantScroll_Weapon_A", 1),
            blessed_enchant_prefab = new("kg_EnchantScroll_Weapon_Blessed_A", 1), Items = new()
            {
                "AtgeirBlackmetal", "AxeBlackMetal", "KnifeBlackMetal", "ShieldBlackmetal",
                "ShieldBlackmetalTower", "SwordBlackmetal", "MaceNeedle"
            }
        },
        new SyncedData.EnchantmentReqs()
        {
            enchant_prefab = new("kg_EnchantScroll_Armor_A", 1),
            blessed_enchant_prefab = new("kg_EnchantScroll_Armor_Blessed_A", 1), Items = new()
            {
                "CapeLinen", "CapeLox", "ArmorPaddedGreaves", "HelmetPadded",  "ArmorPaddedCuirass"
            }
        },
        new SyncedData.EnchantmentReqs()
        {
            enchant_prefab = new("kg_EnchantScroll_Weapon_B", 1),
            blessed_enchant_prefab = new("kg_EnchantScroll_Weapon_Blessed_B", 1), Items = new()
            {
                "BattleaxeCrystal", "BowDraugrFang", "FistFenrirClaw","KnifeSilver", "MaceSilver", "ShieldSilver",
                "SpearWolfFang", "SwordSilver"
            }
        },
        new SyncedData.EnchantmentReqs()
        {
            enchant_prefab = new("kg_EnchantScroll_Armor_B", 1),
            blessed_enchant_prefab = new("kg_EnchantScroll_Armor_Blessed_B", 1), Items = new()
            {
                "ArmorWolfChest", "ArmorWolfLegs", "CapeWolf", "HelmetDrake",
                "ArmorFenringChest", "ArmorFenringLegs", "HelmetFenring"
            }
        },
        new SyncedData.EnchantmentReqs()
        {
            enchant_prefab = new("kg_EnchantScroll_Weapon_C", 1),
            blessed_enchant_prefab = new("kg_EnchantScroll_Weapon_Blessed_C", 1), Items = new()
            {
                "AtgeirIron", "AxeIron", "Battleaxe", "BowHuntsman", "Lantern", "MaceIron", "PickaxeIron",
                "ShieldBanded", "ShieldIronBuckler", "ShieldIronSquare", "ShieldIronTower", "ShieldSerpentscale",
                "SledgeIron", "SpearElderbark", "SwordIron", "TankardAnniversary", "TorchMist", "KnifeChitin",
                "SpearChitin",  "ArmorRootChest"
            }
        },
        new SyncedData.EnchantmentReqs()
        {
            enchant_prefab = new("kg_EnchantScroll_Armor_C", 1),
            blessed_enchant_prefab = new("kg_EnchantScroll_Armor_Blessed_C", 1), Items = new()
            {
                "ArmorIronChest", "ArmorIronLegs","ArmorRootLegs", "HelmetIron", "HelmetRoot"
            }
        },
        new SyncedData.EnchantmentReqs()
        {
            enchant_prefab = new("kg_EnchantScroll_Weapon_D", 1),
            blessed_enchant_prefab = new("kg_EnchantScroll_Weapon_Blessed_D", 1), Items = new()
            {
                "AtgeirBronze", "AxeBronze", "Cultivator", "MaceBronze", "PickaxeBronze", "ShieldBronzeBuckler",
                "SpearBronze", "SwordBronze", "BowFineWood"
            }
        },
        new SyncedData.EnchantmentReqs()
        {
            enchant_prefab = new("kg_EnchantScroll_Armor_D", 1),
            blessed_enchant_prefab = new("kg_EnchantScroll_Armor_Blessed_D", 1), Items = new()
            {
                "ArmorBronzeChest", "ArmorBronzeLegs", "ArmorTrollLeatherChest", "ArmorTrollLeatherLegs",
                "CapeTrollHide", "HelmetBronze", "HelmetTrollLeather"
            }
        },
        new SyncedData.EnchantmentReqs()
        {
            enchant_prefab = new("kg_EnchantScroll_Weapon_F", 1),
            blessed_enchant_prefab = new("kg_EnchantScroll_Weapon_Blessed_F", 1), Items = new()
            {
                "AxeFlint", "Bow","Hoe", "KnifeButcher", "KnifeCopper", "KnifeFlint", "PickaxeAntler", "PickaxeStone",
                "ShieldBoneTower", "ShieldWood", "ShieldWoodTower", "SpearFlint", "SledgeStagbreaker"
            }
        },
        new SyncedData.EnchantmentReqs()
        {
            enchant_prefab = new("kg_EnchantScroll_Armor_F", 1),
            blessed_enchant_prefab = new("kg_EnchantScroll_Armor_Blessed_F", 1), Items = new()
            {
                "ArmorLeatherChest", "ArmorLeatherLegs", "CapeDeerHide", "HelmetLeather", "ArmorRagsChest",
                "ArmorRagsLegs"
            }
        }
    };

    private static List<SyncedData.OverrideChances> DefaultOverrides_Chances = new()
    {
        {new() {
            Items = new() { "SwordCheat", "SledgeCheat" },
            Chances = new() { { 1, new() {success = 50} }, { 2, new() {success = 45} }, { 3, new() {success = 40} }, { 4, new() {success = 30} }, { 5, new() {success = 25} },
                { 6, new() {success = 20} }, { 7, new() {success = 10} }, { 8, new() {success = 5} }, { 9, new() {success = 3} }, { 10, new() {success = 0} } }
        }}
    };

    private static readonly List<SyncedData.OverrideColors> DefaultOverrides_Colors = new()
    {
        {new() {
            Items = new() { "SwordCheat", "SledgeCheat" },
            Colors = new()
            {
                { 1, new() { color = "#00190019", variant = 0 } },
                { 2, new() { color = "#00320032", variant = 0 } },
                { 3, new() { color = "#004B004B", variant = 0 } },
                { 4, new() { color = "#00640064", variant = 0 } },
                { 5, new() { color = "#007D007D", variant = 0 } },
                { 6, new() { color = "#00960096", variant = 0 } },
                { 7, new() { color = "#00AF00AF", variant = 0 } },
                { 8, new() { color = "#00C800C8", variant = 0 } },
                { 9, new() { color = "#00E100E1", variant = 0 } },
                { 10, new() { color = "#00FA00FA", variant = 0 } }
            }
        }}
    };

    private static readonly List<SyncedData.OverrideStats> DefaultOverrides_Stats = new()
    {
        {new() { 
                Items = new() { "SwordCheat", "SledgeCheat" } , 
                Stats = new() 
                {
                    { 1, new() { damage_percentage = 5, damage_fire = 10 } },
                    { 2, new() { damage_percentage = 10, damage_fire = 20 } },
                    { 3, new() { damage_percentage = 15, damage_fire = 30 } },
                    { 4, new() { damage_percentage = 20, damage_fire = 40 } },
                    { 5, new() { damage_percentage = 25, damage_fire = 50 } },
                    { 6, new() { damage_percentage = 30, damage_fire = 60 } },
                    { 7, new() { damage_percentage = 35, damage_fire = 70 } },
                    { 8, new() { damage_percentage = 40, damage_fire = 80 } },
                    { 9, new() { damage_percentage = 45, damage_fire = 90 } },
                    { 10, new() { damage_percentage = 50, damage_fire = 100 } },
                }
        }}
    };

    public static string YAML_Stats_Weapons => new SerializerBuilder().ConfigureDefaultValuesHandling(
        DefaultValuesHandling.OmitDefaults).Build().Serialize(DefaultStats_Weapons);

    public static string YAML_Stats_Armor => new SerializerBuilder().ConfigureDefaultValuesHandling(
        DefaultValuesHandling.OmitDefaults).Build().Serialize(DefaultStats_Armor);

    public static string YAML_Reqs => new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults).Build().Serialize(DefaultReqs);

    public static string YAML_Colors => new SerializerBuilder().ConfigureDefaultValuesHandling(DefaultValuesHandling
        .OmitDefaults).Build().Serialize(DefaultColors);

    public static string YAML_Chances => new SerializerBuilder().ConfigureDefaultValuesHandling(
        DefaultValuesHandling.OmitDefaults).Build().Serialize(DefaultChances);

    public static string YAML_Overrides_Chances => new SerializerBuilder().ConfigureDefaultValuesHandling(
        DefaultValuesHandling.OmitDefaults).Build().Serialize(DefaultOverrides_Chances);

    public static string YAML_Overrides_Colors => new SerializerBuilder().ConfigureDefaultValuesHandling(
        DefaultValuesHandling.OmitDefaults).Build().Serialize(DefaultOverrides_Colors);

    public static string YAML_Overrides_Stats => new SerializerBuilder().ConfigureDefaultValuesHandling(
        DefaultValuesHandling.OmitDefaults).Build().Serialize(DefaultOverrides_Stats);
}