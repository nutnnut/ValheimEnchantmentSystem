using YamlDotNet.Serialization;

namespace kg.ValheimEnchantmentSystem.Configs;

public static class Defaults
{
    private static readonly Dictionary<int, SyncedData.Stat_Data> DefaultStats_Weapons =
        new Dictionary<int, SyncedData.Stat_Data>()
        {
            { 1,  new() { damage_percentage = 4, attack_speed = 2, max_stamina = 3, armor = 3 } },
            { 2,  new() { damage_percentage = 5, attack_speed = 3, max_stamina = 4, stamina_use_reduction_percent = 2, armor = 4 } },
            { 3,  new() { damage_percentage = 7, attack_speed = 3, weapon_skill = 1, max_stamina = 6, stamina_use_reduction_percent = 3, armor = 8 } },
            { 4,  new() { damage_percentage = 8, damage_spirit_percentage = 4, attack_speed = 4, weapon_skill = 2, movement_speed = 2, armor = 10, max_hp = 5, max_stamina = 8, stamina_use_reduction_percent = 3 } },
            { 5,  new() { damage_percentage = 10, damage_fire_percentage = 5, damage_spirit_percentage = 5, attack_speed = 5, weapon_skill = 3, movement_speed = 2, armor = 10, max_hp = 5, max_stamina = 10, stamina_use_reduction_percent = 3, max_eitr = 10 } },
            { 6,  new() { damage_percentage = 12, damage_fire_percentage = 6, damage_frost_percentage = 0, damage_lightning_percentage = 6, damage_poison_percentage = 0, damage_spirit_percentage = 6, attack_speed = 6, weapon_skill = 3, movement_speed = 3, durability_percentage = 25, armor = 11, armor_percentage = 15, max_hp = 6, hp_regen = 0.6f, max_stamina = 12, stamina_regen_percentage = 6, stamina_use_reduction_percent = 4, max_eitr = 11, eitr_regen_percentage = 6 } },
            { 7,  new() { damage_percentage = 14, damage_fire_percentage = 7, damage_frost_percentage = 7, damage_lightning_percentage = 7, damage_poison_percentage = 0, damage_spirit_percentage = 7, attack_speed = 7, weapon_skill = 3, movement_speed = 3, durability_percentage = 30, armor = 12, armor_percentage = 18, max_hp = 7, hp_regen = 0.7f, max_stamina = 14, stamina_regen_percentage = 7, stamina_use_reduction_percent = 4, max_eitr = 12, eitr_regen_percentage = 7 } },
            { 8,  new() { damage_percentage = 16, damage_fire_percentage = 8, damage_frost_percentage = 8, damage_lightning_percentage = 8, damage_poison_percentage = 0, damage_spirit_percentage = 8, attack_speed = 8, weapon_skill = 4, movement_speed = 3, durability_percentage = 35, armor = 13, armor_percentage = 20, max_hp = 8, hp_regen = 0.8f, max_stamina = 16, stamina_regen_percentage = 8, stamina_use_reduction_percent = 4, max_eitr = 13, eitr_regen_percentage = 8 } },
            { 9,  new() { damage_percentage = 18, damage_fire_percentage = 9, damage_frost_percentage = 9, damage_lightning_percentage = 9, damage_poison_percentage = 9, damage_spirit_percentage = 9, attack_speed = 9, weapon_skill = 4, movement_speed = 3, durability_percentage = 40, armor = 14, armor_percentage = 22, max_hp = 9, hp_regen = 0.9f, max_stamina = 18, stamina_regen_percentage = 9, stamina_use_reduction_percent = 4, max_eitr = 14, eitr_regen_percentage = 9 } },
            { 10, new() { damage_percentage = 20, damage_fire_percentage = 10, damage_frost_percentage = 10, damage_lightning_percentage = 10, damage_poison_percentage = 10, damage_spirit_percentage = 10, attack_speed = 10, weapon_skill = 5, movement_speed = 3, durability_percentage = 45, armor = 15, armor_percentage = 25, max_hp = 10, hp_regen = 1f, max_stamina = 20, stamina_regen_percentage = 10, stamina_use_reduction_percent = 5, max_eitr = 15, eitr_regen_percentage = 10 } },
            { 11, new() { damage_percentage = 22, damage_fire_percentage = 11, damage_frost_percentage = 11, damage_lightning_percentage = 11, damage_poison_percentage = 11, damage_spirit_percentage = 11, attack_speed = 11, weapon_skill = 5, movement_speed = 3, durability_percentage = 50, armor = 16, armor_percentage = 28, max_hp = 11, hp_regen = 1.1f, max_stamina = 21, stamina_regen_percentage = 11, stamina_use_reduction_percent = 5, max_eitr = 16, eitr_regen_percentage = 11 } },
            { 12, new() { damage_percentage = 24, damage_fire_percentage = 12, damage_frost_percentage = 12, damage_lightning_percentage = 12, damage_poison_percentage = 12, damage_spirit_percentage = 12, attack_speed = 12, weapon_skill = 6, movement_speed = 4, durability_percentage = 55, armor = 17, armor_percentage = 30, max_hp = 12, hp_regen = 1.2f, max_stamina = 22, stamina_regen_percentage = 12, stamina_use_reduction_percent = 6, max_eitr = 17, eitr_regen_percentage = 12 } },
            { 13, new() { damage_percentage = 26, damage_fire_percentage = 13, damage_frost_percentage = 13, damage_lightning_percentage = 13, damage_poison_percentage = 13, damage_spirit_percentage = 13, attack_speed = 13, weapon_skill = 6, movement_speed = 4, durability_percentage = 60, armor = 18, armor_percentage = 32, max_hp = 13, hp_regen = 1.3f, max_stamina = 23, stamina_regen_percentage = 13, stamina_use_reduction_percent = 6, max_eitr = 18, eitr_regen_percentage = 13 } },
            { 14, new() { damage_percentage = 28, damage_fire_percentage = 14, damage_frost_percentage = 14, damage_lightning_percentage = 14, damage_poison_percentage = 14, damage_spirit_percentage = 14, attack_speed = 14, weapon_skill = 7, movement_speed = 4, durability_percentage = 65, armor = 19, armor_percentage = 35, max_hp = 14, hp_regen = 1.4f, max_stamina = 24, stamina_regen_percentage = 14, stamina_use_reduction_percent = 7, max_eitr = 19, eitr_regen_percentage = 14 } },
            { 15, new() { damage_percentage = 30, damage_fire_percentage = 15, damage_frost_percentage = 15, damage_lightning_percentage = 15, damage_poison_percentage = 15, damage_spirit_percentage = 15, attack_speed = 15, weapon_skill = 7, movement_speed = 4, durability_percentage = 70, armor = 20, armor_percentage = 38, max_hp = 15, hp_regen = 1.5f, max_stamina = 25, stamina_regen_percentage = 15, stamina_use_reduction_percent = 7, max_eitr = 20, eitr_regen_percentage = 15 } },
            { 16, new() { damage_percentage = 32, damage_fire_percentage = 16, damage_frost_percentage = 16, damage_lightning_percentage = 16, damage_poison_percentage = 16, damage_spirit_percentage = 16, attack_speed = 16, weapon_skill = 8, movement_speed = 5, durability_percentage = 75, armor = 21, armor_percentage = 40, max_hp = 16, hp_regen = 1.6f, max_stamina = 26, stamina_regen_percentage = 16, stamina_use_reduction_percent = 8, max_eitr = 21, eitr_regen_percentage = 16 } },
            { 17, new() { damage_percentage = 34, damage_fire_percentage = 17, damage_frost_percentage = 17, damage_lightning_percentage = 17, damage_poison_percentage = 17, damage_spirit_percentage = 17, attack_speed = 17, weapon_skill = 8, movement_speed = 5, durability_percentage = 80, armor = 22, armor_percentage = 42, max_hp = 17, hp_regen = 1.7f, max_stamina = 27, stamina_regen_percentage = 17, stamina_use_reduction_percent = 8, max_eitr = 22, eitr_regen_percentage = 17 } },
            { 18, new() { damage_percentage = 36, damage_fire_percentage = 18, damage_frost_percentage = 18, damage_lightning_percentage = 18, damage_poison_percentage = 18, damage_spirit_percentage = 18, attack_speed = 18, weapon_skill = 9, movement_speed = 5, durability_percentage = 85, armor = 23, armor_percentage = 45, max_hp = 18, hp_regen = 1.8f, max_stamina = 28, stamina_regen_percentage = 18, stamina_use_reduction_percent = 9, max_eitr = 23, eitr_regen_percentage = 18 } },
            { 19, new() { damage_percentage = 38, damage_fire_percentage = 19, damage_frost_percentage = 19, damage_lightning_percentage = 19, damage_poison_percentage = 19, damage_spirit_percentage = 19, attack_speed = 19, weapon_skill = 9, movement_speed = 5, durability_percentage = 90, armor = 24, armor_percentage = 48, max_hp = 19, hp_regen = 1.9f, max_stamina = 29, stamina_regen_percentage = 19, stamina_use_reduction_percent = 9, max_eitr = 24, eitr_regen_percentage = 19 } },
            { 20, new() { damage_percentage = 40, damage_fire_percentage = 20, damage_frost_percentage = 20, damage_lightning_percentage = 20, damage_poison_percentage = 20, damage_spirit_percentage = 20, attack_speed = 20, weapon_skill = 10, movement_speed = 5, durability_percentage = 100, armor = 25, armor_percentage = 50, max_hp = 20, hp_regen = 2.0f, max_stamina = 30, stamina_regen_percentage = 20, stamina_use_reduction_percent = 10, max_eitr = 25, eitr_regen_percentage = 20 } },
        };

    private static readonly Dictionary<int, SyncedData.Stat_Data> DefaultStats_Armor =
        new Dictionary<int, SyncedData.Stat_Data>()
        {
            { 1,  new() { armor = 1, armor_percentage = 2, max_hp = 1, stagger_limit_percentage = 2 } },
            { 2,  new() { armor = 2, armor_percentage = 4, max_hp = 1, stagger_limit_percentage = 4, durability_percentage = 5 } },
            { 3,  new() { armor = 2, armor_percentage = 6, max_hp = 2, stagger_limit_percentage = 6, stagger_recovery_percentage = 3, durability_percentage = 10 } },
            { 4,  new() { armor = 3, armor_percentage = 8, max_hp = 4, stagger_limit_percentage = 8, stagger_recovery_percentage = 4, movement_speed = 1, movement_skill = 1, durability_percentage = 10, hp_regen = 0.5f } },
            { 5,  new() { armor = 4, armor_percentage = 10, max_hp = 5, stagger_limit_percentage = 10, stagger_recovery_percentage = 5, movement_speed = 2, movement_skill = 2, durability_percentage = 15, hp_regen = 0.5f, stamina_regen_percentage = 2 } },
            { 6,  new() { attack_speed = 2, weapon_skill = 2, armor = 4, armor_percentage = 12, max_hp = 6, stagger_limit_percentage = 12, stagger_recovery_percentage = 6, movement_speed = 2, movement_skill = 2, durability_percentage = 20, hp_regen = 0.75f, stamina_regen_percentage = 3 } },
            { 7,  new() { attack_speed = 2, weapon_skill = 2, armor = 5, armor_percentage = 15, max_hp = 7, stagger_limit_percentage = 14, stagger_recovery_percentage = 7, max_stamina = 7, movement_speed = 2, movement_skill = 2, durability_percentage = 25, hp_regen = 0.75f, stamina_regen_percentage = 3 } },
            { 8,  new() { attack_speed = 2, weapon_skill = 2, armor = 6, armor_percentage = 17, max_hp = 8, stagger_limit_percentage = 16, stagger_recovery_percentage = 8, max_stamina = 8, movement_speed = 2, movement_skill = 2, durability_percentage = 30, hp_regen = 1f, stamina_regen_percentage = 4, stamina_use_reduction_percent = 2, max_eitr = 8 } },
            { 9,  new() { attack_speed = 2, weapon_skill = 2, armor = 7, armor_percentage = 19, max_hp = 9, stagger_limit_percentage = 18, stagger_recovery_percentage = 9, max_stamina = 9, movement_speed = 2, movement_skill = 2, durability_percentage = 35, hp_regen = 1f, stamina_regen_percentage = 4, stamina_use_reduction_percent = 2, max_eitr = 9, eitr_regen_percentage = 5 } },
            { 10, new() { attack_speed = 2, weapon_skill = 2, armor = 8, armor_percentage = 20, max_hp = 10, stagger_limit_percentage = 20, stagger_recovery_percentage = 10, max_stamina = 10, movement_speed = 3, movement_skill = 3, durability_percentage = 40, hp_regen = 1.25f, stamina_regen_percentage = 5, stamina_use_reduction_percent = 3, max_eitr = 10, eitr_regen_percentage = 5 } },
            { 11, new() { attack_speed = 2, weapon_skill = 2, armor = 9, armor_percentage = 21, max_hp = 11, stagger_limit_percentage = 22, stagger_recovery_percentage = 11, max_stamina = 11, movement_speed = 3, movement_skill = 3, durability_percentage = 45, hp_regen = 1.25f, stamina_regen_percentage = 5, stamina_use_reduction_percent = 3, max_eitr = 11, eitr_regen_percentage = 6 } },
            { 12, new() { attack_speed = 3, weapon_skill = 3, armor = 10, armor_percentage = 22, max_hp = 12, stagger_limit_percentage = 24, stagger_recovery_percentage = 12, max_stamina = 12, movement_speed = 3, movement_skill = 3, durability_percentage = 50, hp_regen = 1.5f, stamina_regen_percentage = 6, stamina_use_reduction_percent = 3, max_eitr = 12, eitr_regen_percentage = 6 } },
            { 13, new() { attack_speed = 3, weapon_skill = 3, armor = 11, armor_percentage = 23, max_hp = 13, stagger_limit_percentage = 26, stagger_recovery_percentage = 13, max_stamina = 13, movement_speed = 3, movement_skill = 3, durability_percentage = 55, hp_regen = 1.75f, stamina_regen_percentage = 6, stamina_use_reduction_percent = 3, max_eitr = 13, eitr_regen_percentage = 7 } },
            { 14, new() { attack_speed = 3, weapon_skill = 3, armor = 11, armor_percentage = 24, max_hp = 14, stagger_limit_percentage = 28, stagger_recovery_percentage = 14, max_stamina = 14, movement_speed = 3, movement_skill = 3, durability_percentage = 60, hp_regen = 1.75f, stamina_regen_percentage = 7, stamina_use_reduction_percent = 3, max_eitr = 14, eitr_regen_percentage = 7 } },
            { 15, new() { attack_speed = 3, weapon_skill = 3, armor = 12, armor_percentage = 25, max_hp = 15, stagger_limit_percentage = 30, stagger_recovery_percentage = 15, max_stamina = 15, movement_speed = 4, movement_skill = 4, durability_percentage = 65, hp_regen = 2f, stamina_regen_percentage = 7, stamina_use_reduction_percent = 4, max_eitr = 15, eitr_regen_percentage = 8 } },
            { 16, new() { attack_speed = 4, weapon_skill = 4, armor = 12, armor_percentage = 26, max_hp = 16, stagger_limit_percentage = 32, stagger_recovery_percentage = 16, max_stamina = 16, movement_speed = 4, movement_skill = 4, durability_percentage = 70, hp_regen = 2f, stamina_regen_percentage = 8, stamina_use_reduction_percent = 4, max_eitr = 16, eitr_regen_percentage = 8 } },
            { 17, new() { attack_speed = 4, weapon_skill = 4, armor = 13, armor_percentage = 27, max_hp = 17, stagger_limit_percentage = 34, stagger_recovery_percentage = 17, max_stamina = 17, movement_speed = 4, movement_skill = 4, durability_percentage = 75, hp_regen = 2.25f, stamina_regen_percentage = 8, stamina_use_reduction_percent = 4, max_eitr = 17, eitr_regen_percentage = 9 } },
            { 18, new() { attack_speed = 4, weapon_skill = 4, armor = 13, armor_percentage = 28, max_hp = 18, stagger_limit_percentage = 36, stagger_recovery_percentage = 18, max_stamina = 18, movement_speed = 4, movement_skill = 4, durability_percentage = 80, hp_regen = 2.25f, stamina_regen_percentage = 9, stamina_use_reduction_percent = 4, max_eitr = 18, eitr_regen_percentage = 9 } },
            { 19, new() { attack_speed = 4, weapon_skill = 4, armor = 14, armor_percentage = 29, max_hp = 19, stagger_limit_percentage = 38, stagger_recovery_percentage = 19, max_stamina = 19, movement_speed = 4, movement_skill = 4, durability_percentage = 90, hp_regen = 2.5f, stamina_regen_percentage = 9, stamina_use_reduction_percent = 4, max_eitr = 19, eitr_regen_percentage = 10 } },
            { 20, new() { attack_speed = 5, weapon_skill = 5, armor = 15, armor_percentage = 30, max_hp = 20, stagger_limit_percentage = 40, stagger_recovery_percentage = 20, max_stamina = 20, movement_speed = 5, movement_skill = 5, durability_percentage = 100, hp_regen = 2.5f, stamina_regen_percentage = 10, stamina_use_reduction_percent = 5, max_eitr = 20, eitr_regen_percentage = 10 } },
        };

    private static readonly Dictionary<int, SyncedData.Chance_Data> DefaultChances = new()
    {
        { 1, new SyncedData.Chance_Data() { success = 70, reroll = 100 } },
        { 2, new SyncedData.Chance_Data() { success = 60, reroll = 100 } },
        { 3, new SyncedData.Chance_Data() { success = 50, destroy = 50, reroll = 100 } },
        { 4, new SyncedData.Chance_Data() { success = 45, destroy = 50, reroll = 100 } },
        { 5, new SyncedData.Chance_Data() { success = 40, destroy = 45, reroll = 90 } },
        { 6, new SyncedData.Chance_Data() { success = 38, destroy = 45, reroll = 90 } },
        { 7, new SyncedData.Chance_Data() { success = 36, destroy = 45, reroll = 90 } },
        { 8, new SyncedData.Chance_Data() { success = 34, destroy = 45, reroll = 90 } },
        { 9, new SyncedData.Chance_Data() { success = 32, destroy = 45, reroll = 90 } },
        { 10, new SyncedData.Chance_Data() { success = 30, destroy = 40, reroll = 85 } },
        { 11, new SyncedData.Chance_Data() { success = 28, destroy = 40, reroll = 85 } },
        { 12, new SyncedData.Chance_Data() { success = 25, destroy = 40, reroll = 85 } },
        { 13, new SyncedData.Chance_Data() { success = 22, destroy = 40, reroll = 85 } },
        { 14, new SyncedData.Chance_Data() { success = 20, destroy = 40, reroll = 85 } },
        { 15, new SyncedData.Chance_Data() { success = 18, destroy = 35, reroll = 80 } },
        { 16, new SyncedData.Chance_Data() { success = 16, destroy = 35, reroll = 80 } },
        { 17, new SyncedData.Chance_Data() { success = 14, destroy = 35, reroll = 80 } },
        { 18, new SyncedData.Chance_Data() { success = 12, destroy = 35, reroll = 80 } },
        { 19, new SyncedData.Chance_Data() { success = 10, destroy = 35, reroll = 75 } },
        { 20, new SyncedData.Chance_Data() { reroll = 70, destroy = 30 } },
    };

    private static readonly Dictionary<int, SyncedData.VFX_Data> DefaultColors =
        new Dictionary<int, SyncedData.VFX_Data>
        {
            { 1,  new SyncedData.VFX_Data() { color = "#00110015", variant = 2 } }, // green glints
            { 2,  new SyncedData.VFX_Data() { color = "#00223315", variant = 2 } }, // blue glints
            { 3,  new SyncedData.VFX_Data() { color = "#33113315", variant = 2 } }, // magenta glints
            { 4,  new SyncedData.VFX_Data() { color = "#33110015", variant = 2 } }, // orange glints
            { 5,  new SyncedData.VFX_Data() { color = "#55000015", variant = 2 } }, // red glints
            { 6,  new SyncedData.VFX_Data() { color = "#22CC0088", variant = 3 } }, // dim green
            { 7,  new SyncedData.VFX_Data() { color = "#0077FF88", variant = 3 } }, // dim blue
            { 8,  new SyncedData.VFX_Data() { color = "#EE11EE88", variant = 3 } }, // dim magenta
            { 9,  new SyncedData.VFX_Data() { color = "#CC330088", variant = 3 } }, // dim orange
            { 10, new SyncedData.VFX_Data() { color = "#FF0000AA", variant = 3 } }, // dim red
            { 11, new SyncedData.VFX_Data() { color = "#00FF0066", variant = 1 } }, // green waves
            { 12, new SyncedData.VFX_Data() { color = "#0077FF88", variant = 1 } }, // blue waves
            { 13, new SyncedData.VFX_Data() { color = "#EE11EEAA", variant = 1 } }, // magenta waves
            { 14, new SyncedData.VFX_Data() { color = "#CC3300AA", variant = 1 } }, // orange waves
            { 15, new SyncedData.VFX_Data() { color = "#FF0000CC", variant = 1 } }, // red waves
            { 16, new SyncedData.VFX_Data() { color = "#00FF00CC", variant = 4 } }, // green ripples
            { 17, new SyncedData.VFX_Data() { color = "#0077FFDD", variant = 4 } }, // blue ripples
            { 18, new SyncedData.VFX_Data() { color = "#FF22FFEE", variant = 4 } }, // magenta ripples
            { 19, new SyncedData.VFX_Data() { color = "#CC3300FF", variant = 4 } }, // orange ripples
            { 20, new SyncedData.VFX_Data() { color = "#CC0000FF", variant = 4 } }  // red ripples
        };

    private static readonly List<SyncedData.EnchantmentReqs> DefaultReqs = new()
    {
        new SyncedData.EnchantmentReqs()
        {
            enchant_prefab = new("kg_EnchantScroll_Weapon_S", 1),
            blessed_enchant_prefab = new("kg_EnchantScroll_Weapon_Blessed_S", 1),
            Items = new()
            {
                "AtgeirHimminAfl",
                "AxeJotunBane",
                "BowSpineSnap",
                "PickaxeBlackMetal",
                "ShieldCarapace",
                "ShieldCarapaceBuckler",
                "SledgeDemolisher",
                "SpearCarapace",
                "StaffFireball",
                "StaffIceShards",
                "StaffShield",
                "StaffSkeleton",
                "SwordMistwalker",
                "THSwordKrom",
                "CrossbowArbalest",
                "SwordCheat",
                "KnifeSkollAndHati",
                // Ash
                "AxeBerzerkr",
                "AxeBerzerkrBlood",
                "AxeBerzerkrLightning",
                "AxeBerzerkrNature",
                "MaceEldner",
                "MaceEldnerBlood",
                "MaceEldnerLightning",
                "MaceEldnerNature",
                "THSwordSlayer",
                "THSwordSlayerBlood",
                "THSwordSlayerLightning",
                "THSwordSlayerNature",
                "SwordNiedhogg",
                "SwordNiedhoggBlood",
                "SwordNiedhoggLightning",
                "SwordNiedhoggNature",
                "SwordDyrnwyn",
                "SpearSplitner",
                "SpearSplitner_Blood",
                "SpearSplitner_Lightning",
                "SpearSplitner_Nature",
                "ShieldFlametal",
                "ShieldFlametalTower",
                "StaffClusterbomb",
                "StaffGreenRoots",
                "StaffLightning",
                "StaffRedTroll",
                "CrossbowRipper",
                "CrossbowRipperNature",
                "CrossbowRipperLightning",
                "CrossbowRipperBlood",
                "BowAshlands",
                "BowAshlandsBlood",
                "BowAshlandsLightning",
                "BowAshlandsNature"
            }
        },
        new SyncedData.EnchantmentReqs()
        {
            enchant_prefab = new("kg_EnchantScroll_Armor_S", 1),
            blessed_enchant_prefab = new("kg_EnchantScroll_Armor_Blessed_S", 1), Items = new()
            {
                "ArmorCarapaceChest",
                "ArmorCarapaceLegs",
                "ArmorMageChest",
                "ArmorMageLegs",
                "CapeFeather",
                "HelmetCarapace",
                "HelmetMage",

                // Ashlands
                "HelmetFlametal",
                "ArmorFlametalChest",
                "ArmorFlametalLegs",
                "HelmetAshlandsMediumHood",
                "ArmorAshlandsMediumChest",
                "ArmorAshlandsMediumlegs",
                "HelmetMage_Ashlands",
                "ArmorMageChest_Ashlands",
                "ArmorMageLegs_Ashlands",
                "CapeAsh",
                "CapeAsksvin"
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