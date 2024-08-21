using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Configs;
using kg.ValheimEnchantmentSystem.Misc;

namespace kg.ValheimEnchantmentSystem;

public static class TerminalCommands
{
    [HarmonyPatch(typeof(Terminal),nameof(Terminal.InitTerminal))]
    [ClientOnlyPatch]
    private static class Terminal_InitTerminal_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Terminal __instance)
        {
            new Terminal.ConsoleCommand("checkenchant", "", (args) =>
            {
                if (!Utils.IsDebug_Strict) return;
                if (args.Length < 2)
                {
                    args.Context.AddString("Usage: checkenchant <slot(1-8)>");
                    return;
                }

                if (!int.TryParse(args[1], out int index))
                {
                    args.Context.AddString("Invalid arguments. Usage: checkenchant <slot(1-8)>");
                    return;
                }

                ItemDrop.ItemData item = Player.m_localPlayer.GetInventory().GetItemAt(index - 1, 0);
                if (item == null || !item.m_dropPrefab)
                {
                    args.Context.AddString($"No item found at index {index}.");
                    return;
                }

                Enchantment_Core.Enchanted en = item.Data().GetOrCreate<Enchantment_Core.Enchanted>();
                if (!en.IsEnchantablePrefab())
                {
                    args.Context.AddString($"{item.m_dropPrefab.name} is not Enchantable.");
                    return;
                }
                args.Context.AddString("Floats: " + en.enchantedItem.ToString());
            });

            new Terminal.ConsoleCommand("setenchant", "", (args) =>
            {
                if (!Utils.IsDebug_Strict) return;
                if (args.Length < 3 || !int.TryParse(args[1], out int index) || !int.TryParse(args[2], out int level))
                {
                    args.Context.AddString("Usage: setenchant <slot(1-8)> <level> [bonusLineCount]");
                    return;
                }

                int bonusLineCount = 0;
                if (args.Length > 3 && !int.TryParse(args[3], out bonusLineCount))
                {
                    args.Context.AddString("Invalid bonusLineCount. It must be an integer.");
                    return;
                }

                ItemDrop.ItemData item = Player.m_localPlayer.GetInventory().GetItemAt(index - 1, 0);
                if (item == null || !item.m_dropPrefab)
                {
                    args.Context.AddString($"No item found at index {index}.");
                    return;
                }

                args.Context.AddString($"Enchanting {item.m_dropPrefab.name}.");

                Enchantment_Core.Enchanted en = item.Data().GetOrCreate<Enchantment_Core.Enchanted>();
                if (!en.IsEnchantablePrefab())
                {
                    args.Context.AddString($"{item.m_dropPrefab.name} is not Enchantable.");
                    return;
                }
                en.enchantedItem.level = level;
                en.EnchantReroll(bonusLineCount);
                args.Context.AddString("Enchantment level set to " + level);
                args.Context.AddString("Floats: " + en.enchantedItem.ToString());
                ValheimEnchantmentSystem._thistype.StartCoroutine(Enchantment_Core.FrameSkipEquip(item));
            });
            
            new Terminal.ConsoleCommand("setenchantall", "", (args) =>
            {
                if(!Utils.IsDebug_Strict) return;
                if (args.Length < 2 || !int.TryParse(args[1], out int level))
                {
                    args.Context.AddString("Usage: setenchantall <level> [bonusLineCount]");
                    return;
                }

                int bonusLineCount = 0;
                if (args.Length > 2 && !int.TryParse(args[2], out bonusLineCount))
                {
                    args.Context.AddString("Invalid bonusLineCount. It must be an integer.");
                    return;
                }

                foreach (ItemDrop.ItemData item in Player.m_localPlayer.m_inventory.m_inventory.Where(x => SyncedData.GetReqs(x.m_dropPrefab?.name) != null))
                {
                    Enchantment_Core.Enchanted en = item.Data().GetOrCreate<Enchantment_Core.Enchanted>();
                    en.enchantedItem.level = level;
                    en.EnchantReroll(bonusLineCount);
                }
            });
        }
    }
}