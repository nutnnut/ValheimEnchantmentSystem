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
                    Chat.instance.AddString("Usage: checkenchant <slot(1-8)>");
                    return;
                }

                if (!int.TryParse(args[1], out int index))
                {
                    Chat.instance.AddString("Invalid arguments. Usage: checkenchant <slot(1-8)>");
                    return;
                }

                ItemDrop.ItemData item = Player.m_localPlayer.GetInventory().GetItemAt(index - 1, 0);
                if (item == null || !item.m_dropPrefab)
                {
                    Chat.instance.AddString($"No item found at index {index}.");
                    return;
                }

                Enchantment_Core.Enchanted en = item.Data().GetOrCreate<Enchantment_Core.Enchanted>();
                if (!en.IsEnchantablePrefab())
                {
                    Chat.instance.AddString($"{item.m_dropPrefab.name} is not Enchantable.");
                    return;
                }
                Chat.instance.m_hideTimer = 0f;
                Chat.instance.AddString("Floats: " + en.randomizedFloat.SerializeJson());
                ValheimEnchantmentSystem._thistype.StartCoroutine(Enchantment_Core.FrameSkipEquip(item));
            });

            new Terminal.ConsoleCommand("setenchant", "", (args) =>
            {
                if (!Utils.IsDebug_Strict) return;
                if (args.Length < 3)
                {
                    Chat.instance.AddString("Usage: setenchant <slot(1-8)> <level>");
                    return;
                }

                if (!int.TryParse(args[1], out int index) || !int.TryParse(args[2], out int level))
                {
                    Chat.instance.AddString("Invalid arguments. Usage: setenchant <slot(1-8)> <level>");
                    return;
                }

                ItemDrop.ItemData item = Player.m_localPlayer.GetInventory().GetItemAt(index - 1, 0);
                if (item == null || !item.m_dropPrefab)
                {
                    Chat.instance.AddString($"No item found at index {index}.");
                    return;
                }

                Chat.instance.AddString($"Enchanting {item.m_dropPrefab.name}.");

                Enchantment_Core.Enchanted en = item.Data().GetOrCreate<Enchantment_Core.Enchanted>();
                if (!en.IsEnchantablePrefab())
                {
                    Chat.instance.AddString($"{item.m_dropPrefab.name} is not Enchantable.");
                    return;
                }
                en.level = level;
                en.EnchantReroll();
                Chat.instance.m_hideTimer = 0f;
                Chat.instance.AddString("Enchantment level set to " + level);
                Chat.instance.AddString("Floats: " + en.randomizedFloat.SerializeJson());
                ValheimEnchantmentSystem._thistype.StartCoroutine(Enchantment_Core.FrameSkipEquip(item));
            });
            
            new Terminal.ConsoleCommand("setenchantall", "", (args) =>
            {
                if(!Utils.IsDebug_Strict) return;
                int level = int.Parse(args[1]);

                foreach (ItemDrop.ItemData item in Player.m_localPlayer.m_inventory.m_inventory.Where(x => SyncedData.GetReqs(x.m_dropPrefab?.name) != null))
                {
                    Enchantment_Core.Enchanted en = item.Data().GetOrCreate<Enchantment_Core.Enchanted>();
                    en.level = level;
                    en.EnchantReroll();
                }
            });
        }
    }
}