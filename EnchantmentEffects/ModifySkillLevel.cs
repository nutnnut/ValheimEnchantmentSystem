using ItemDataManager;
using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using TMPro;
using static kg.ValheimEnchantmentSystem.Enchantment_Core;
using static Skills;
using Object = UnityEngine.Object;

namespace kg.ValheimEnchantmentSystem.EnchantmentEffects
{
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

            int getSkillIncrease(SkillType[] types, string effectName)
            {
                int result = 0;
                if (types.Contains(skillType))
                {
                    result += (int)Math.Round(player.GetTotalEnchantedValue(effectName), MidpointRounding.AwayFromZero);
                }
                return result;
            }

            increase += getSkillIncrease(new[] { player.GetCurrentWeapon().m_shared.m_skillType }, "weapon_skill");
            increase += getSkillIncrease(new[] { SkillType.Run, SkillType.Jump, SkillType.Swim, SkillType.Sneak }, "movement_skill");

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
            __result = Mathf.Lerp(low, high, UnityEngine.Random.value);
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
                        UnityEngine.Object.Destroy(bar.gameObject);
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
                    extraLevelbar.GetComponent<UnityEngine.UI.Image>().color = Color.magenta;
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