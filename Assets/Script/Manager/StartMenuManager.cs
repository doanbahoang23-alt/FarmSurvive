using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using TMPro;
using Object = UnityEngine.Object;

public class StartMenuManager : MonoBehaviour
{
    #region Enum & State Machine Definitions
    public enum MenuState
    {
        MainMenu,
        InGame,
        Settings,
        SaveLoad
    }
    #endregion

    #region Singleton & Serialized Fields
    public static StartMenuManager Instance { get; private set; }

    [Header("Trạng Thái Giao Diện")]
    [SerializeField] private MenuState currentMenuState = MenuState.MainMenu;
    private MenuState previousMenuState = MenuState.MainMenu;

    [Header("Các Panel Giao Diện")]
    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject menuBlurVolume;

    [Header("Cài Đặt Âm Thanh (BGM & SFX)")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI bgmValueText;
    [SerializeField] private TextMeshProUGUI sfxValueText;

    [Header("Các Thanh HUD Khi Chơi")]
    [SerializeField] private GameObject healthBar;
    [SerializeField] private GameObject staminaBar;
    [SerializeField] private GameObject expBar;
    [SerializeField] private GameObject foodBar;
    [SerializeField] private GameObject toolbar;
    [SerializeField] private GameObject hudSettingsButton;

    [Header("Nút Lưu & Tải Game")]
    [SerializeField] private Button loadGameButton;

    [Header("Bảng Chọn Bản Lưu (Multi-Slot Save/Load)")]
    [SerializeField] private SaveSlotUIController saveSlotUIController;
    [SerializeField] private GameObject saveLoadPanel;

    [Header("Tham Chiếu Người Chơi")]
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private GameObject player;
    [SerializeField] private Transform customSpawnPoint;

    private Vector3 initialPlayerPosition;
    private bool hasSavedInitialPosition = false;
    private float lastSFXPreviewTime = 0f;
    #endregion

    #region Properties
    public bool IsInGame => currentMenuState == MenuState.InGame;
    public MenuState CurrentMenuState => currentMenuState;
    #endregion

    #region Unity Lifecycles
    private void Awake()
    {
        if (Instance == null) Instance = this;
        FindReferencesIfNull();
    }

    private void Start()
    {
        InitAudioSliders();
        ShowStartMenu();
        UpdateSaveLoadUI();
    }

    private void Update()
    {
        if (IsEscapePressed())
        {
            HandleEscapeInput();
        }
    }
    #endregion

    #region UI Auto-Binding & References
    public void FindReferencesIfNull()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

        BindPanelReferences(canvas);
        BindHUDReferences(canvas);
        BindSaveLoadReferences(canvas);
        BindAudioReferences();
        BindPlayerReferences();
    }

    private void BindPanelReferences(Canvas canvas)
    {
        if (canvas != null)
        {
            if (startMenuPanel == null) startMenuPanel = FindChild(canvas.transform, "StartMenuPanel");
            if (settingsPanel == null) settingsPanel = FindChild(canvas.transform, "SettingsPanel");
        }

        if (startMenuPanel == null) startMenuPanel = GameObject.Find("StartMenuPanel");
        if (settingsPanel == null) settingsPanel = GameObject.Find("SettingsPanel");
    }

    private void BindHUDReferences(Canvas canvas)
    {
        if (hudSettingsButton == null)
        {
            hudSettingsButton = (canvas != null ? FindChild(canvas.transform, "Btn_HUD_Settings") : null) 
                                ?? GameObject.Find("Btn_HUD_Settings");
        }

        if (hudSettingsButton != null)
        {
            TextMeshProUGUI tmp = hudSettingsButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null && tmp.text.Contains("\u2699"))
            {
                tmp.text = "CÀI ĐẶT";
            }
        }

        if (toolbar == null)
        {
            toolbar = (canvas != null ? FindChild(canvas.transform, "BarGrid", "Hotbar", "Toolbar") : null)
                      ?? FindInScene("BarGrid", "Hotbar", "Toolbar");
        }

        if (healthBar == null) healthBar = FindInScene("HealthBar");
        if (staminaBar == null) staminaBar = FindInScene("StaminaBar");
        if (expBar == null) expBar = FindInScene("ExpBar", "Exp", "ExperienceBar");
        if (foodBar == null) foodBar = FindInScene("FoodBar", "Food", "HungerBar");
    }

    private void BindSaveLoadReferences(Canvas canvas)
    {
        if (saveSlotUIController == null)
        {
            saveSlotUIController = Object.FindFirstObjectByType<SaveSlotUIController>(FindObjectsInactive.Include);
        }

        if (saveLoadPanel == null && saveSlotUIController != null)
        {
            saveLoadPanel = saveSlotUIController.gameObject;
        }

        if (saveLoadPanel == null && canvas != null)
        {
            GameObject p = FindChild(canvas.transform, "SaveLoadPanel");
            if (p != null)
            {
                saveLoadPanel = p;
                if (saveSlotUIController == null) saveSlotUIController = p.GetComponent<SaveSlotUIController>();
            }
        }

        if (loadGameButton == null && startMenuPanel != null)
        {
            GameObject btn = FindChild(startMenuPanel.transform, "ButtonGroup/Btn_Continue", "ButtonGroup/Btn_LoadGame", "Btn_Continue", "Btn_LoadGame");
            if (btn != null) loadGameButton = btn.GetComponent<Button>();
        }
    }

    private void BindAudioReferences()
    {
        if (settingsPanel == null) return;

        if (bgmSlider == null || sfxSlider == null)
        {
            Slider[] sliders = settingsPanel.GetComponentsInChildren<Slider>(true);
            foreach (var s in sliders)
            {
                if (bgmSlider == null && IsMatchingName(s.gameObject, "BGM")) bgmSlider = s;
                else if (sfxSlider == null && IsMatchingName(s.gameObject, "SFX")) sfxSlider = s;
            }
        }

        if (bgmValueText == null || sfxValueText == null)
        {
            TextMeshProUGUI[] texts = settingsPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                if (t.name != "PercentText") continue;
                if (bgmValueText == null && IsMatchingName(t.gameObject, "BGM")) bgmValueText = t;
                else if (sfxValueText == null && IsMatchingName(t.gameObject, "SFX")) sfxValueText = t;
            }
        }
    }

    private void BindPlayerReferences()
    {
        if (player == null)
        {
            PlayerController pc = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc != null) player = pc.gameObject;
            else
            {
                PlayerInventory inv = Object.FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);
                if (inv != null) player = inv.gameObject;
                else player = GameObject.FindWithTag("Player");
            }
        }

        if (player != null && playerEquipment == null)
        {
            playerEquipment = player.GetComponent<PlayerEquipment>();
        }

        if (player != null && !hasSavedInitialPosition)
        {
            initialPlayerPosition = player.transform.position;
            hasSavedInitialPosition = true;
        }
    }

    private GameObject FindChild(Transform parent, params string[] relativePaths)
    {
        if (parent == null) return null;
        foreach (var path in relativePaths)
        {
            Transform t = parent.Find(path);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    private GameObject FindInScene(params string[] objectNames)
    {
        foreach (var name in objectNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null) return obj;
        }
        return null;
    }

    private bool IsMatchingName(GameObject go, string keyword)
    {
        if (go.name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (go.transform.parent != null && go.transform.parent.name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }
    #endregion

    #region Menu State Machine & Input Handling
    private bool IsEscapePressed()
    {
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }

    private void HandleEscapeInput()
    {
        switch (currentMenuState)
        {
            case MenuState.Settings:
                CloseSettings();
                break;

            case MenuState.SaveLoad:
                CloseSaveLoadPanel();
                break;

            case MenuState.InGame:
                OpenSettings();
                break;

            case MenuState.MainMenu:
                // Ở Menu chính, bỏ qua hoặc không làm gì
                break;
        }
    }
    #endregion

    #region Menu Navigation & Visibility (ToggleHUD / ToggleMenu)
    public void ToggleHUD(bool isActive)
    {
        if (healthBar != null) healthBar.SetActive(isActive);
        if (staminaBar != null) staminaBar.SetActive(isActive);
        if (expBar != null) expBar.SetActive(isActive);
        if (foodBar != null) foodBar.SetActive(isActive);
        if (toolbar != null) toolbar.SetActive(isActive);
        if (hudSettingsButton != null) hudSettingsButton.SetActive(isActive);
    }

    public void ToggleMenu(bool isActive)
    {
        if (startMenuPanel != null)
        {
            startMenuPanel.SetActive(isActive);
            if (isActive)
            {
                startMenuPanel.transform.SetAsLastSibling();
            }
        }

        if (menuBlurVolume != null)
        {
            menuBlurVolume.SetActive(isActive);
        }
    }

    private void TogglePlayer(bool active)
    {
        if (player != null)
        {
            player.SetActive(active);
            PlayerInput pInput = player.GetComponent<PlayerInput>();
            if (pInput != null) pInput.enabled = active;
            PlayerController pCtrl = player.GetComponent<PlayerController>();
            if (pCtrl != null) pCtrl.enabled = active;
        }
        if (playerEquipment != null) playerEquipment.enabled = active;
    }

    public void SetHUDActive(bool active) => ToggleHUD(active);
    public void SetPlayerActive(bool active) => TogglePlayer(active);

    public void ShowStartMenu()
    {
        FindReferencesIfNull();

        Time.timeScale = 0f;
        currentMenuState = MenuState.MainMenu;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        UpdateSaveLoadUI();

        ToggleMenu(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (saveLoadPanel != null) saveLoadPanel.SetActive(false);

        ToggleHUD(false);
        TogglePlayer(false);
    }

    public void StartNewGame()
    {
        FindReferencesIfNull();

        CleanupWorldEntities();
        ResetPlayerStatus();
        ResetEconomyAndInventory();
        ResetEnemyWave();
        EnterGameWorld();

        Debug.Log("<color=#70E880>[StartMenuManager] Bắt đầu Ván Chơi Mới thành công! Đã dọn dẹp thế giới và reset các chỉ số.</color>");
    }

    public void ResumeGameAfterLoad()
    {
        FindReferencesIfNull();
        EnterGameWorld();
    }

    private void EnterGameWorld()
    {
        Time.timeScale = 1f;
        currentMenuState = MenuState.InGame;

        ToggleMenu(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (saveLoadPanel != null) saveLoadPanel.SetActive(false);
        if (menuBlurVolume != null) menuBlurVolume.SetActive(false);

        ToggleHUD(true);
        TogglePlayer(true);
    }

    public void OpenSettings()
    {
        previousMenuState = currentMenuState;
        currentMenuState = MenuState.Settings;

        Time.timeScale = 0f;
        InitAudioSliders();

        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (startMenuPanel != null && startMenuPanel.activeSelf) startMenuPanel.SetActive(false);
        if (menuBlurVolume != null) menuBlurVolume.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (previousMenuState == MenuState.InGame)
        {
            currentMenuState = MenuState.InGame;
            Time.timeScale = 1f;
            if (menuBlurVolume != null) menuBlurVolume.SetActive(false);
        }
        else
        {
            currentMenuState = MenuState.MainMenu;
            ToggleMenu(true);
        }
    }

    public void ReturnToMainMenu()
    {
        if (EnemySpawn.Instance != null)
        {
            EnemySpawn.Instance.ClearAllEnemies();
        }

        ShowStartMenu();
    }

    public void OnExitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion

    #region World Entities Cleanup & Reset Mechanics
    private void SafeReleaseOrDestroy(GameObject obj)
    {
        if (obj == null) return;
        if (PoolManager.Instance != null)
            PoolManager.Instance.Release(obj);
        else
            Destroy(obj);
    }

    private void SafeReleaseAll<T>() where T : Component
    {
        T[] items = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in items)
        {
            if (item != null)
            {
                SafeReleaseOrDestroy(item.gameObject);
            }
        }
    }

    public void CleanupWorldEntities()
    {
        if (EnemySpawn.Instance != null)
        {
            EnemySpawn.Instance.ClearAllEnemies();
        }

        SafeReleaseAll<PlantController>();
        SafeReleaseAll<ItemPickUp>();
        ClearTraps();
        ClearFarmTilemap();
    }

    private void ClearTraps()
    {
        try
        {
            GameObject[] traps = GameObject.FindGameObjectsWithTag("Trap");
            foreach (var trap in traps)
            {
                SafeReleaseOrDestroy(trap);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[StartMenuManager] Lưu ý khi quét tag Trap: {ex.Message}");
        }

        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb is IPlaceable && !(mb is PlantController))
            {
                SafeReleaseOrDestroy(mb.gameObject);
            }
        }
    }

    private void ClearFarmTilemap()
    {
        Tilemap farmTilemap = FarmableArea.ActiveFarmTilemap;
        if (farmTilemap == null)
        {
            FarmableArea fa = Object.FindFirstObjectByType<FarmableArea>(FindObjectsInactive.Include);
            if (fa != null) farmTilemap = fa.GetComponent<Tilemap>();
        }
        if (farmTilemap != null)
        {
            farmTilemap.ClearAllTiles();
        }
    }

    private void ResetPlayerStatus()
    {
        if (player == null) player = GameObject.FindWithTag("Player");
        if (player == null) return;

        BaseHealth health = player.GetComponent<BaseHealth>();
        if (health != null) health.ResetHealth();

        PlayerStamina stamina = player.GetComponent<PlayerStamina>();
        if (stamina != null) stamina.SetStamina(stamina.MaxStamina, stamina.MaxStamina);

        PlayerFood food = player.GetComponent<PlayerFood>();
        if (food != null) food.SetFood(food.MaxFood, food.MaxFood);

        PlayerExperience exp = player.GetComponent<PlayerExperience>();
        if (exp != null) exp.ResetExperience();

        ResetPlayerPosition();

        if (playerEquipment != null) playerEquipment.EquipItem(null);
    }

    private void ResetPlayerPosition()
    {
        if (player == null) player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector3 spawnPos;
        if (customSpawnPoint != null)
        {
            spawnPos = customSpawnPoint.position;
        }
        else if (hasSavedInitialPosition)
        {
            spawnPos = initialPlayerPosition;
        }
        else
        {
            GameObject spawnObj = GameObject.Find("PlayerSpawnPoint") ?? GameObject.Find("PlayerSpawn");
            spawnPos = spawnObj != null ? spawnObj.transform.position : player.transform.position;
        }

        spawnPos.z = 0f;
        player.transform.position = spawnPos;

        if (player.TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void ResetEconomyAndInventory()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.ClearInventory();
            PlayerInventory.Instance.SetCoins(0);
            PlayerInventory.Instance.GiveStartingItems();
        }

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.ResetPurchaseCounts();
            ShopManager.Instance.RefreshWeaponVisibility();
            ShopManager.Instance.UpdateCoinUI();
        }
    }

    private void ResetEnemyWave()
    {
        if (EnemySpawn.Instance != null)
        {
            EnemySpawn.Instance.ResetToInitialState();
        }
    }
    #endregion

    #region Audio Settings (BGM & SFX)
    public void InitAudioSliders()
    {
        FindReferencesIfNull();

        float currentBGM = 0.4f;
        float currentSFX = 0.8f;

        if (AudioManager.Instance != null)
        {
            currentBGM = AudioManager.Instance.BGMVolume;
            currentSFX = AudioManager.Instance.SFXVolume;
        }
        else
        {
            currentBGM = PlayerPrefs.GetFloat("BGM_Volume", 0.4f);
            currentSFX = PlayerPrefs.GetFloat("SFX_Volume", 0.8f);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.SetValueWithoutNotify(currentBGM);
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }
        UpdateVolumeText(bgmValueText, currentBGM);

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.SetValueWithoutNotify(currentSFX);
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
        UpdateVolumeText(sfxValueText, currentSFX);
    }

    public void OnBGMVolumeChanged(float val)
    {
        val = Mathf.Clamp01(val);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(val);
        }
        else
        {
            PlayerPrefs.SetFloat("BGM_Volume", val);
            PlayerPrefs.Save();
        }
        UpdateVolumeText(bgmValueText, val);
    }

    public void OnSFXVolumeChanged(float val)
    {
        val = Mathf.Clamp01(val);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(val);

            if (Time.unscaledTime - lastSFXPreviewTime > 0.15f)
            {
                lastSFXPreviewTime = Time.unscaledTime;
                AudioClip previewClip = AudioManager.Instance.pickUpClip ?? AudioManager.Instance.coinPickUpClip ?? AudioManager.Instance.attackClip;
                if (previewClip != null)
                {
                    AudioManager.Instance.PlaySFX(previewClip);
                }
            }
        }
        else
        {
            PlayerPrefs.SetFloat("SFX_Volume", val);
            PlayerPrefs.Save();
        }
        UpdateVolumeText(sfxValueText, val);
    }

    private void UpdateVolumeText(TextMeshProUGUI label, float val)
    {
        if (label != null)
        {
            label.text = $"{Mathf.RoundToInt(val * 100f)}%";
        }
    }
    #endregion

    #region Save / Load Integration (Multi-Slot)
    public void OnSaveGameButtonClicked()
    {
        OpenSaveLoadPanel(SaveSlotUIController.SaveLoadMode.Save);
    }

    public void OnLoadGameButtonClicked()
    {
        OpenSaveLoadPanel(SaveSlotUIController.SaveLoadMode.Load);
    }

    public void OpenSaveLoadPanel(SaveSlotUIController.SaveLoadMode mode)
    {
        FindReferencesIfNull();
        if (saveSlotUIController != null)
        {
            previousMenuState = currentMenuState;
            currentMenuState = MenuState.SaveLoad;
            saveSlotUIController.Open(mode);
        }
        else
        {
            Debug.LogWarning("[StartMenuManager] saveSlotUIController chưa được gán!");
        }
    }

    public void CloseSaveLoadPanel()
    {
        if (saveSlotUIController != null)
        {
            saveSlotUIController.Close();
        }
        if (saveLoadPanel != null)
        {
            saveLoadPanel.SetActive(false);
        }

        if (previousMenuState == MenuState.InGame)
        {
            currentMenuState = MenuState.InGame;
            Time.timeScale = 1f;
            if (menuBlurVolume != null) menuBlurVolume.SetActive(false);
        }
        else
        {
            currentMenuState = MenuState.MainMenu;
            ToggleMenu(true);
        }
    }

    public void UpdateSaveLoadUI()
    {
        if (loadGameButton != null)
        {
            bool hasAnySave = false;
            if (SaveManager.Instance != null)
            {
                hasAnySave = SaveManager.Instance.HasSaveData(1) 
                          || SaveManager.Instance.HasSaveData(2) 
                          || SaveManager.Instance.HasSaveData(3);
            }
            loadGameButton.interactable = hasAnySave;
        }
    }
    #endregion
}
