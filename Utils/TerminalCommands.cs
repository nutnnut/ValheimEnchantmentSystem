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
                if (args.Length < 3)
                {
                    __instance.AddString("Usage: checkenchant <index> <level>");
                    return;
                }

                if (!int.TryParse(args[1], out int index) || !int.TryParse(args[2], out int level))
                {
                    __instance.AddString("Invalid arguments. Usage: checkenchant <index> <level>");
                    return;
                }

                ItemDrop.ItemData item = Player.m_localPlayer.GetInventory().GetItem(index);
                if (item == null || !item.m_dropPrefab)
                {
                    __instance.AddString($"No item found at index {index}.");
                    return;
                }

                Enchantment_Core.Enchanted en = item.Data().GetOrCreate<Enchantment_Core.Enchanted>();
                Chat.instance.m_hideTimer = 0f;
                Chat.instance.AddString("Level: " + en.level);
                Chat.instance.AddString("Floats: " + en.randomizedFloat.SerializeJson());
                ValheimEnchantmentSystem._thistype.StartCoroutine(Enchantment_Core.FrameSkipEquip(item));
            });

            new Terminal.ConsoleCommand("setenchant", "", (args) =>
            {
                if (!Utils.IsDebug_Strict) return;
                if (args.Length < 3)
                {
                    __instance.AddString("Usage: setenchant <index> <level>");
                    return;
                }

                if (!int.TryParse(args[1], out int index) || !int.TryParse(args[2], out int level))
                {
                    __instance.AddString("Invalid arguments. Usage: setenchant <index> <level>");
                    return;
                }

                ItemDrop.ItemData item = Player.m_localPlayer.GetInventory().GetItem(index);
                if (item == null || !item.m_dropPrefab)
                {
                    __instance.AddString($"No item found at index {index}.");
                    return;
                }

                Enchantment_Core.Enchanted en = item.Data().GetOrCreate<Enchantment_Core.Enchanted>();
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