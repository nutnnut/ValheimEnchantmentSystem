using JetBrains.Annotations;
using kg.ValheimEnchantmentSystem.Misc;
using TMPro;
using Valheim.SettingsGui;

namespace kg.ValheimEnchantmentSystem.UI;

public static class SettingsUI
{
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    [ClientOnlyPatch]
    static class Menu_Start_Patch
    {
        private static bool firstInit = true; 
 
        [UsedImplicitly]
        private static void Postfix(FejdStartup __instance) 
        {
            if (ValheimEnchantmentSystem.NoGraphics) return;
            if (!firstInit) return;
            firstInit = false;
            GameObject settingsPrefab = __instance.m_settingsPrefab;
            Transform gameplay = settingsPrefab.transform.Find("Panel/TabButtons/Gameplay");
            if (!gameplay) gameplay = settingsPrefab.transform.Find("Panel/TabButtons/Tabs/Gameplay");
            if (!gameplay) return;
            Transform newButton = UnityEngine.Object.Instantiate(gameplay);
            newButton.transform.Find("KeyHint").gameObject.SetActive(false); 
            newButton.SetParent(gameplay.parent, false); 
            newButton.name = "kg_Enchantment";
            newButton.SetAsLastSibling();
            Transform textTransform = newButton.transform.Find("Label");
            Transform textTransform_Selected = newButton.transform.Find("Selected/LabelSelected");
            if (!textTransform || !textTransform_Selected) return;
            textTransform.GetComponent<TMP_Text>().text = "$enchantment_enchantment".Localize();
            textTransform_Selected.GetComponent<TMP_Text>().text = "$enchantment_enchantment".Localize();
            TabHandler tabHandler = settingsPrefab.transform.Find("Panel/TabButtons").GetComponent<TabHandler>();
            Transform page = settingsPrefab.transform.Find("Panel/TabContent");
            GameObject newPage = UnityEngine.Object.Instantiate(ValheimEnchantmentSystem._asset.LoadAsset<GameObject>("kg_Enchantments_Settings"));
            newPage.AddComponent<VesSettings>();
            Localization.instance.Localize(newPage.transform);
            newPage.transform.SetParent(page);
            newPage.name = "kg_Enchantment";
            newPage.SetActive(false);
            TabHandler.Tab newTab = new TabHandler.Tab
            {
                m_default = false,
                m_button = newButton.GetComponent<Button>(),
                m_page = newPage.GetComponent<RectTransform>()
            };
            tabHandler.m_tabs.Add(newTab);
            newPage.transform.localScale *= 1.2f;
        }
    }

    public sealed class VesSettings : MonoBehaviour, ISettingsTab
    {
        // --- ISettingsTab ---
        public event Action<string, int> SharedSettingChanged;

        // Optional: keep if your mod uses it elsewhere (your old code calls this.Saved?.Invoke()).
        public event Action Saved;

        // --- Internal setting state (UI edits change these; OnOkAsync commits to config) ---
        private bool _enableHotbarVisual_Internal;
        private bool _enableMainVFX_Internal;
        private bool _enableInventoryVisual_Internal;
        private VES_UI.Duration _enchantmentAnimationDuration_Internal;
        private Notifications_UI.Filter _filterConfig_Internal;
        private int _notificationDuration_Internal;

        // --- Cached UI refs ---
        private Transform _optionsRoot;

        private Button _btHotbar;
        private GameObject _ckHotbar;

        private Button _btMain;
        private GameObject _ckMain;

        private Button _btInventory;
        private GameObject _ckInventory;

        private Button _btSpeed1;
        private GameObject _ckSpeed1;
        private Button _btSpeed3;
        private GameObject _ckSpeed3;
        private Button _btSpeed6;
        private GameObject _ckSpeed6;

        private Button _btNotifySuccess;
        private GameObject _ckNotifySuccess;
        private Button _btNotifyFail;
        private GameObject _ckNotifyFail;

        private Slider _slNotifyDuration;
        private Text _txtNotifyDuration;

        private bool _initialized;

        // ---------------- ISettingsTab lifecycle ----------------

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            CacheUiOrDisable();
            if (_optionsRoot == null)
            {
                // UI prefab structure is not as expected; tab will still exist but do nothing.
                return;
            }

            ReadFromConfigIntoInternalState();
            ApplyInternalStateToUi();

            SubscribeEvents();
        }

        public void Terminate()
        {
            if (!_initialized) return;

            UnsubscribeEvents();
            _initialized = false;
        }

        public void OnTabOpen(Button backButton, Button okButton)
        {
            // Refresh from config each time the tab opens (matches “official tab” behavior for safety).
            // This also ensures that if configs changed elsewhere, UI stays in sync.
            if (_optionsRoot == null) return;

            ReadFromConfigIntoInternalState();
            ApplyInternalStateToUi();
        }

        public void OnOkAsync(OkActionCompletedHandler okActionCompletedCallback)
        {
            if (okActionCompletedCallback == null) throw new ArgumentNullException(nameof(okActionCompletedCallback));

            try
            {
                SaveInternalStateToConfig();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VES] Failed to save settings: {ex}");
                // Even on failure, we should unblock UI flow.
            }
            finally
            {
                okActionCompletedCallback();
            }
        }

        public void OnBack()
        {
            // Optional: if you want “Back” to discard changes, you can reload from config here.
            // Current implementation: do nothing (same as many official tabs).
        }

        public void OnSharedSettingChanged(string setting, int value)
        {
            // Not used by this tab currently.
            // Keep method for interface compatibility.
        }

        // ---------------- Internal helpers ----------------

        private void ReadFromConfigIntoInternalState()
        {
            _enableHotbarVisual_Internal = Enchantment_VFX._enableHotbarVisual.Value;
            _enableMainVFX_Internal = Enchantment_VFX._enableMainVFX.Value;
            _enableInventoryVisual_Internal = Enchantment_VFX._enableInventoryVisual.Value;
            _enchantmentAnimationDuration_Internal = VES_UI._enchantmentAnimationDuration.Value;
            _filterConfig_Internal = Notifications_UI._filterConfig.Value;
            _notificationDuration_Internal = Notifications_UI._duration.Value;
        }

        private void SaveInternalStateToConfig()
        {
            Enchantment_VFX._enableHotbarVisual.Value = _enableHotbarVisual_Internal;
            Enchantment_VFX._enableMainVFX.Value = _enableMainVFX_Internal;
            Enchantment_VFX._enableInventoryVisual.Value = _enableInventoryVisual_Internal;

            VES_UI._enchantmentAnimationDuration.Value = _enchantmentAnimationDuration_Internal;
            Notifications_UI._filterConfig.Value = _filterConfig_Internal;
            Notifications_UI._duration.Value = _notificationDuration_Internal;

            // Apply/refresh visuals
            Enchantment_VFX.UpdateGrid();

            // Persist (your old code saved only one config file; keep as-is to avoid behavior change)
            Enchantment_VFX._enableHotbarVisual.ConfigFile.Save();

            Saved?.Invoke();
        }

        private void ApplyInternalStateToUi()
        {
            // toggles
            if (_ckHotbar) _ckHotbar.SetActive(_enableHotbarVisual_Internal);
            if (_ckMain) _ckMain.SetActive(_enableMainVFX_Internal);
            if (_ckInventory) _ckInventory.SetActive(_enableInventoryVisual_Internal);

            // speed radio-like group
            SetSpeedCheckmarks(_enchantmentAnimationDuration_Internal);

            // notify filter flags
            if (_ckNotifySuccess) _ckNotifySuccess.SetActive(_filterConfig_Internal.HasFlagFast(Notifications_UI.Filter.Success));
            if (_ckNotifyFail) _ckNotifyFail.SetActive(_filterConfig_Internal.HasFlagFast(Notifications_UI.Filter.Fail));

            // duration slider + text
            if (_slNotifyDuration)
            {
                // Set without notify to avoid triggering our listener while syncing UI.
                _slNotifyDuration.SetValueWithoutNotify(_notificationDuration_Internal);
            }
            UpdateNotifyDurationText(_notificationDuration_Internal);
        }

        private void SetSpeedCheckmarks(VES_UI.Duration d)
        {
            if (_ckSpeed1) _ckSpeed1.SetActive(d == VES_UI.Duration._1);
            if (_ckSpeed3) _ckSpeed3.SetActive(d == VES_UI.Duration._3);
            if (_ckSpeed6) _ckSpeed6.SetActive(d == VES_UI.Duration._6);
        }

        private void UpdateNotifyDurationText(int seconds)
        {
            if (_txtNotifyDuration) _txtNotifyDuration.text = seconds + "s";
        }

        // ---------------- UI caching & wiring ----------------

        private void CacheUiOrDisable()
        {
            _optionsRoot = transform.Find("Background/options");
            if (_optionsRoot == null)
            {
                Debug.LogError("[VES] VesSettings: Missing UI path 'Background/options'. Tab disabled.");
                return;
            }

            // Sections
            var hotbarVFX = FindT(_optionsRoot, "HotbarVFX");
            var mainVFX = FindT(_optionsRoot, "MainVFX");
            var inventoryVFX = FindT(_optionsRoot, "InventoryVFX");
            var enchantSpeed = FindT(_optionsRoot, "EnchantSpeed");
            var notifyFilter = FindT(_optionsRoot, "NotificationsFilter");
            var notifyDuration = FindT(_optionsRoot, "NotificationsDuration");

            // Hotbar
            _btHotbar = GetButton(hotbarVFX, "Button");
            _ckHotbar = FindGo(hotbarVFX, "Button/Checkmark");

            // Main
            _btMain = GetButton(mainVFX, "Button");
            _ckMain = FindGo(mainVFX, "Button/Checkmark");

            // Inventory
            _btInventory = GetButton(inventoryVFX, "Button");
            _ckInventory = FindGo(inventoryVFX, "Button/Checkmark");

            // Speed buttons
            _btSpeed1 = GetButton(enchantSpeed, "Button_1");
            _ckSpeed1 = FindGo(enchantSpeed, "Button_1/Checkmark");

            _btSpeed3 = GetButton(enchantSpeed, "Button_3");
            _ckSpeed3 = FindGo(enchantSpeed, "Button_3/Checkmark");

            _btSpeed6 = GetButton(enchantSpeed, "Button_6");
            _ckSpeed6 = FindGo(enchantSpeed, "Button_6/Checkmark");

            // Notify filters
            _btNotifySuccess = GetButton(notifyFilter, "Success");
            _ckNotifySuccess = FindGo(notifyFilter, "Success/Checkmark");

            _btNotifyFail = GetButton(notifyFilter, "Fail");
            _ckNotifyFail = FindGo(notifyFilter, "Fail/Checkmark");

            // Notify duration
            _slNotifyDuration = GetSlider(notifyDuration, "Slider");
            _txtNotifyDuration = GetText(notifyDuration, "text");
        }

        private void SubscribeEvents()
        {
            // Defensive: ensure no duplicates even if Initialize called unexpectedly.
            UnsubscribeEvents();

            if (_btHotbar) _btHotbar.onClick.AddListener(OnClickHotbar);
            if (_btInventory) _btInventory.onClick.AddListener(OnClickInventory);
            if (_btMain) _btMain.onClick.AddListener(OnClickMain);

            if (_btSpeed1) _btSpeed1.onClick.AddListener(OnClickSpeed1);
            if (_btSpeed3) _btSpeed3.onClick.AddListener(OnClickSpeed3);
            if (_btSpeed6) _btSpeed6.onClick.AddListener(OnClickSpeed6);

            if (_btNotifySuccess) _btNotifySuccess.onClick.AddListener(OnClickNotifySuccess);
            if (_btNotifyFail) _btNotifyFail.onClick.AddListener(OnClickNotifyFail);

            if (_slNotifyDuration) _slNotifyDuration.onValueChanged.AddListener(OnNotifyDurationChanged);
        }

        private void UnsubscribeEvents()
        {
            // RemoveAllListeners is safe and easiest here since these UI objects are dedicated to this tab.
            if (_btHotbar) _btHotbar.onClick.RemoveAllListeners();
            if (_btInventory) _btInventory.onClick.RemoveAllListeners();
            if (_btMain) _btMain.onClick.RemoveAllListeners();

            if (_btSpeed1) _btSpeed1.onClick.RemoveAllListeners();
            if (_btSpeed3) _btSpeed3.onClick.RemoveAllListeners();
            if (_btSpeed6) _btSpeed6.onClick.RemoveAllListeners();

            if (_btNotifySuccess) _btNotifySuccess.onClick.RemoveAllListeners();
            if (_btNotifyFail) _btNotifyFail.onClick.RemoveAllListeners();

            if (_slNotifyDuration) _slNotifyDuration.onValueChanged.RemoveAllListeners();
        }

        // ---------------- UI event handlers ----------------

        private void OnClickHotbar()
        {
            VES_UI.PlayClick();
            _enableHotbarVisual_Internal = !_enableHotbarVisual_Internal;
            if (_ckHotbar) _ckHotbar.SetActive(_enableHotbarVisual_Internal);
        }

        private void OnClickInventory()
        {
            VES_UI.PlayClick();
            _enableInventoryVisual_Internal = !_enableInventoryVisual_Internal;
            if (_ckInventory) _ckInventory.SetActive(_enableInventoryVisual_Internal);
        }

        private void OnClickMain()
        {
            VES_UI.PlayClick();
            _enableMainVFX_Internal = !_enableMainVFX_Internal;
            if (_ckMain) _ckMain.SetActive(_enableMainVFX_Internal);
        }

        private void OnClickSpeed1()
        {
            VES_UI.PlayClick();
            _enchantmentAnimationDuration_Internal = VES_UI.Duration._1;
            SetSpeedCheckmarks(_enchantmentAnimationDuration_Internal);
        }

        private void OnClickSpeed3()
        {
            VES_UI.PlayClick();
            _enchantmentAnimationDuration_Internal = VES_UI.Duration._3;
            SetSpeedCheckmarks(_enchantmentAnimationDuration_Internal);
        }

        private void OnClickSpeed6()
        {
            VES_UI.PlayClick();
            _enchantmentAnimationDuration_Internal = VES_UI.Duration._6;
            SetSpeedCheckmarks(_enchantmentAnimationDuration_Internal);
        }

        private void OnClickNotifySuccess()
        {
            VES_UI.PlayClick();
            _filterConfig_Internal ^= Notifications_UI.Filter.Success;
            if (_ckNotifySuccess) _ckNotifySuccess.SetActive(_filterConfig_Internal.HasFlagFast(Notifications_UI.Filter.Success));
        }

        private void OnClickNotifyFail()
        {
            VES_UI.PlayClick();
            _filterConfig_Internal ^= Notifications_UI.Filter.Fail;
            if (_ckNotifyFail) _ckNotifyFail.SetActive(_filterConfig_Internal.HasFlagFast(Notifications_UI.Filter.Fail));
        }

        private void OnNotifyDurationChanged(float val)
        {
            int currentVal = Mathf.RoundToInt(val);
            _notificationDuration_Internal = currentVal;
            UpdateNotifyDurationText(currentVal);
        }

        // ---------------- tiny safe find/get helpers ----------------

        private static Transform FindT(Transform root, string path)
        {
            if (!root) return null;
            var t = root.Find(path);
            if (!t) Debug.LogError($"[VES] Missing UI path: '{root.name}/{path}'");
            return t;
        }

        private static GameObject FindGo(Transform root, string path)
        {
            var t = FindT(root, path);
            return t ? t.gameObject : null;
        }

        private static Button GetButton(Transform root, string path)
        {
            var t = FindT(root, path);
            if (!t) return null;
            var c = t.GetComponent<Button>();
            if (!c) Debug.LogError($"[VES] Missing Button component at: '{t.name}'");
            return c;
        }

        private static Slider GetSlider(Transform root, string path)
        {
            var t = FindT(root, path);
            if (!t) return null;
            var c = t.GetComponent<Slider>();
            if (!c) Debug.LogError($"[VES] Missing Slider component at: '{t.name}'");
            return c;
        }

        private static Text GetText(Transform root, string path)
        {
            var t = FindT(root, path);
            if (!t) return null;
            var c = t.GetComponent<Text>();
            if (!c) Debug.LogError($"[VES] Missing Text component at: '{t.name}'");
            return c;
        }
    }
}