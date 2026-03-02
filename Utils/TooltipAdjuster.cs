// using kg.ValheimEnchantmentSystem.Misc;
// using TMPro;
//
// namespace kg.ValheimEnchantmentSystem;
//
// public static class TooltipAdjuster
// {
//     [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.UpdateTextElements))]
//     [ClientOnlyPatch]
//     public static class UITooltip_UpdateTextElements_Patch
//     {
//         private static readonly FieldInfo TooltipField = AccessTools.Field(typeof(UITooltip), "m_tooltip") ?? AccessTools.Field(typeof(UITooltip), "m_tooltipInstance");
//         private static readonly PropertyInfo ColumnCountProperty = typeof(TMP_Text).GetProperty("columnCount") ?? typeof(TMP_Text).GetProperty("columns");
//
//         public static void Postfix()
//         {
//             if (TooltipField == null) return;
//             GameObject tooltip = (GameObject)TooltipField.GetValue(null);
//             if (tooltip == null) return;
//
//             Transform textTransform = Utils.FindChild(tooltip.transform, "Text");
//             if (textTransform == null) return;
//             
//             TMP_Text text = textTransform.GetComponent<TMP_Text>();
//             if (text == null) return;
//
//             RectTransform backgroundRect = tooltip.transform.childCount > 0 
//                 ? tooltip.transform.GetChild(0) as RectTransform 
//                 : tooltip.GetComponent<RectTransform>();
//             if (backgroundRect == null) return;
//
//             // Thresholds
//             const float maxHeight = 600f;
//             const float wideWidth = 600f;
//             const float normalWidth = 300f;
//             const float padding = 40f;
//
//             // Force mesh update to get correct preferred height after text change
//             text.ForceMeshUpdate();
//             float prefHeight = text.preferredHeight;
//             int currentColumns = ColumnCountProperty != null ? (int)ColumnCountProperty.GetValue(text) : 1;
//
//             if (prefHeight > maxHeight)
//             {
//                 if (currentColumns < 2)
//                 {
//                     backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, wideWidth);
//                     if (textTransform is RectTransform textRect)
//                     {
//                         textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, wideWidth - padding);
//                     }
//                     Transform topicTransform = Utils.FindChild(tooltip.transform, "Topic");
//                     if (topicTransform is RectTransform topicRect)
//                     {
//                         topicRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, wideWidth - padding);
//                     }
//                     
//                     ColumnCountProperty?.SetValue(text, 2);
//                     text.SetAllDirty();
//                     Canvas.ForceUpdateCanvases();
//                     global::Utils.ClampUIToScreen(backgroundRect);
//                 }
//             }
//             else if (prefHeight < maxHeight * 0.4f && currentColumns >= 2)
//             {
//                 backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, normalWidth);
//                 if (textTransform is RectTransform textRect)
//                 {
//                     textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, normalWidth - padding);
//                 }
//                 Transform topicTransform = Utils.FindChild(tooltip.transform, "Topic");
//                 if (topicTransform is RectTransform topicRect)
//                 {
//                     topicRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, normalWidth - padding);
//                 }
//
//                 ColumnCountProperty?.SetValue(text, 1);
//                 text.SetAllDirty();
//                 Canvas.ForceUpdateCanvases();
//                 global::Utils.ClampUIToScreen(backgroundRect);
//             }
//         }
//     }
// }
