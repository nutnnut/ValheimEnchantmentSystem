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

[Serializable]
public class EnchantedItem
{
    public int level = 0;
    public List<EnchantmentEffect> effects = new List<EnchantmentEffect>();

    public EnchantedItem()
    {
    }

    public EnchantedItem(int level, List<EnchantmentEffect> effects)
    {
        this.level = level;
        this.effects = effects;
    }

    public float GetTotalFloat(string effectType = null)
    {
        return GetFloats(effectType).Sum(x => x.value);
    }

    public List<EnchantmentEffect> GetFloats(string effectType = null)
    {
        return effectType == null ? effects.ToList() : effects.Where(x => x.name == effectType).ToList();
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this, Formatting.None);
    }
}

[Serializable]
public class EnchantmentEffect
{
    public string name;
    public float value; // Values in items are actually stored as float multiplier of actual stats

    public EnchantmentEffect(string name, float value)
    {
        this.name = name;
        this.value = value;
    }
}