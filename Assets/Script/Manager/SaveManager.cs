using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public class SaveManager : MonoBehaviour
{
    #region Singleton & Sự Kiện
    private static SaveManager instance;
    public static SaveManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<SaveManager>(FindObjectsInactive.Include);
                if (instance == null)
                {
                    GameObject go = new GameObject("SaveManager");
                    instance = go.AddComponent<SaveManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    public event Action<GameSaveData> OnSaveSuccess;
    public event Action<string> OnSaveFailed;
    public event Action<GameSaveData> OnLoadSuccess;
    public event Action<string> OnLoadFailed;

    [Header("Item Database (Tùy chọn gán từ Inspector hoặc tự lấy qua ShopManager)")]
    [SerializeField] private List<ItemData> itemDatabase = new List<ItemData>();
    private readonly Dictionary<string, ItemData> itemCache = new Dictionary<string, ItemData>(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Đường Dẫn & Quản Lý File Lưu
    public string GetSaveFilePath(int slotIndex)
    {
        string saveDir = Path.Combine(Application.persistentDataPath, "SaveData");
        if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);
        return Path.Combine(saveDir, $"save_game_{slotIndex}.json");
    }

    public bool HasSaveData(int slotIndex)
    {
        return File.Exists(GetSaveFilePath(slotIndex));
    }

    public GameSaveData LoadSaveDataHeader(int slotIndex)
    {
        string path = GetSaveFilePath(slotIndex);
        if (!File.Exists(path)) return null;

        try
        {
            return JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }

    public GameSaveData GetSlotMetadata(int slotIndex) => LoadSaveDataHeader(slotIndex);

    private GameSaveData CreateInitialSaveData()
    {
        return new GameSaveData
        {
            saveDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            gameVersion = Application.version
        };
    }

    private void WriteSaveFile(int slotIndex, GameSaveData data)
    {
        string filePath = GetSaveFilePath(slotIndex);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);
    }

    private GameSaveData ReadSaveFile(string filePath)
    {
        string json = File.ReadAllText(filePath);
        return JsonUtility.FromJson<GameSaveData>(json);
    }
    #endregion

    #region Điều Phối Lưu Game (SaveGame - Orchestrator)
    public bool SaveGame(int slotIndex)
    {
        try
        {
            GameSaveData data = CreateInitialSaveData();

            if (!CollectPlayerData(data)) return false;
            CollectInventoryData(data);
            CollectFarmingData(data);
            CollectWaveData(data);

            WriteSaveFile(slotIndex, data);

            Debug.Log($"<color=#70E880>[SaveManager] Lưu game thành công vào Slot {slotIndex}!</color>");
            OnSaveSuccess?.Invoke(data);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Lỗi khi lưu Slot {slotIndex}: {ex.Message}");
            OnSaveFailed?.Invoke(ex.Message);
            return false;
        }
    }
    //Thu thập dữ liệu người chơi
    private bool CollectPlayerData(GameSaveData data)
    {
        GameObject player = GetPlayerObject();
        if (player == null)
        {
            OnSaveFailed?.Invoke("Không tìm thấy Player!");
            return false;
        }

        CollectPlayerTransform(player, data);
        CollectPlayerStats(player, data);
        return true;
    }

    //tìm kiếm player gameobjetc
    private GameObject GetPlayerObject()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            PlayerController pc = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc != null) player = pc.gameObject;
        }
        return player;
    }
    //Lấy tọa độ player
    private void CollectPlayerTransform(GameObject player, GameSaveData data)
    {
        Vector3 pos = player.transform.position;
        data.posX = pos.x;
        data.posY = pos.y;
        data.posZ = pos.z;
    }

    // trạng thái player
    private void CollectPlayerStats(GameObject player, GameSaveData data)
    {
        BaseHealth health = player.GetComponent<BaseHealth>();
        if (health != null)
        {
            data.currentHealth = health.CurrentHealth;
            data.maxHealth = health.MaxHealth;
        }

        PlayerStamina stamina = player.GetComponent<PlayerStamina>();
        if (stamina != null)
        {
            data.currentStamina = stamina.CurrentStamina;
            data.maxStamina = stamina.MaxStamina;
        }

        PlayerFood food = player.GetComponent<PlayerFood>();
        if (food != null)
        {
            data.currentFood = food.CurrentFood;
            data.maxFood = food.MaxFood;
            data.foodDecayTimer = food.DecayTimer;
        }

        PlayerExperience exp = player.GetComponent<PlayerExperience>();
        if (exp != null)
        {
            data.currentLevel = exp.CurrentLevel;
            data.currentExp = exp.CurrentExp;
            data.maxExp = exp.MaxExp;
        }
    }

    // Vật phẩm sở hữu
    private void CollectInventoryData(GameSaveData data)
    {
        if (PlayerInventory.Instance != null)
        {
            data.coins = PlayerInventory.Instance.Coins;
            CollectInventorySlots(data);
        }

        PlayerHotbar hotbar = Object.FindFirstObjectByType<PlayerHotbar>(FindObjectsInactive.Include);
        if (hotbar != null)
        {
            data.currentHotbarIndex = hotbar.currentHotbarIndex;
        }
    }

    //Kiểm tra slot đồ
    private void CollectInventorySlots(GameSaveData data)
    {
        if (PlayerInventory.Instance.slots == null) return;

        for (int i = 0; i < PlayerInventory.Instance.slots.Length; i++)
        {
            var slot = PlayerInventory.Instance.slots[i];
            if (slot != null && slot.itemData != null && slot.amount > 0)
            {
                data.inventorySlots.Add(new SavedSlotData(i, slot.itemData.name, slot.amount));
            }
        }
    }

    //Trạn thái map
    private void CollectFarmingData(GameSaveData data)
    {
        CollectTilledTiles(data);
        CollectPlants(data);
        CollectPlacedItems(data);
    }

    private void CollectTilledTiles(GameSaveData data)
    {
        Tilemap farmTilemap = GetFarmTilemap();
        if (farmTilemap == null) return;

        BoundsInt bounds = farmTilemap.cellBounds;
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (farmTilemap.HasTile(cell))
            {
                data.tilledTiles.Add(new SavedTilePos(cell.x, cell.y, cell.z));
            }
        }
    }

    private void CollectPlants(GameSaveData data)
    {
        PlantController[] plants = Object.FindObjectsByType<PlantController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in plants)
        {
            if (p == null) continue;
            string pName = p.PlantData != null ? p.PlantData.name : p.gameObject.name.Replace("(Clone)", "").Trim();
            Vector3 pos = p.transform.position;
            data.plants.Add(new SavedPlantData(pName, pos.x, pos.y, pos.z, p.CurrentStage, p.GrowthTimer, p.IsFullyGrown));
        }
    }

    private void CollectPlacedItems(GameSaveData data)
    {
        HashSet<GameObject> placedSet = new HashSet<GameObject>();
        try
        {
            GameObject[] traps = GameObject.FindGameObjectsWithTag("Trap");
            foreach (var t in traps) if (t != null) placedSet.Add(t);
        }
        catch { }

        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb is IPlaceable && !(mb is PlantController)) placedSet.Add(mb.gameObject);
        }

        foreach (var go in placedSet)
        {
            if (go == null) continue;
            Vector3 pos = go.transform.position;
            SavedPlacedItemData placedData = new SavedPlacedItemData
            {
                itemName = go.name.Replace("(Clone)", "").Trim(),
                posX = pos.x,
                posY = pos.y,
                posZ = pos.z
            };
            data.placedItems.Add(placedData);
        }
    }

    private void CollectWaveData(GameSaveData data)
    {
        if (EnemySpawn.Instance != null)
        {
            data.currentWaveCount = EnemySpawn.Instance.CurrentWaveCount;
            data.isWaitingForNextWave = EnemySpawn.Instance.IsWaitingForNextWave;
            data.waveCooldownTimer = EnemySpawn.Instance.WaveCooldownTimer;
        }

        CollectShopData(data);
    }

    private void CollectShopData(GameSaveData data)
    {
        if (ShopManager.Instance != null && ShopManager.Instance.itemPurchaseCounts != null)
        {
            foreach (var pair in ShopManager.Instance.itemPurchaseCounts)
            {
                data.purchaseRecords.Add(new SavedPurchaseRecord(pair.Key, pair.Value));
            }
        }
    }
    #endregion

    #region Điều Phối Nạp Game (LoadGame - Orchestrator)
    public bool LoadGame(int slotIndex)
    {
        string filePath = GetSaveFilePath(slotIndex);
        if (!File.Exists(filePath))
        {
            OnLoadFailed?.Invoke($"Không tìm thấy file lưu Slot {slotIndex}!");
            return false;
        }

        try
        {
            GameSaveData data = ReadSaveFile(filePath);
            if (data == null)
            {
                OnLoadFailed?.Invoke("Dữ liệu bản lưu bị lỗi!");
                return false;
            }

            CleanupWorldState();

            RestorePlayerData(data);
            RestoreInventoryData(data);
            RestoreFarmingData(data);
            RestoreWaveData(data);

            Debug.Log($"<color=#70E880>[SaveManager] Nạp game thành công từ Slot {slotIndex} ({data.saveDate})!</color>");
            OnLoadSuccess?.Invoke(data);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Lỗi khi nạp Slot {slotIndex}: {ex.Message}");
            OnLoadFailed?.Invoke(ex.Message);
            return false;
        }
    }

    private void RestorePlayerData(GameSaveData data)
    {
        GameObject player = GetPlayerObject();
        if (player == null) return;

        RestorePlayerTransform(player, data);
        RestorePlayerStats(player, data);
    }

    private void RestorePlayerTransform(GameObject player, GameSaveData data)
    {
        player.transform.position = new Vector3(data.posX, data.posY, data.posZ);
        if (player.TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void RestorePlayerStats(GameObject player, GameSaveData data)
    {
        BaseHealth health = player.GetComponent<BaseHealth>();
        if (health != null)
        {
            float maxH = data.maxHealth > 0f ? data.maxHealth : health.MaxHealth;
            float curH = data.currentHealth > 0f ? data.currentHealth : maxH;
            health.SetHealth(curH, maxH);
        }

        PlayerStamina stamina = player.GetComponent<PlayerStamina>();
        if (stamina != null)
        {
            float maxS = data.maxStamina > 0f ? data.maxStamina : stamina.MaxStamina;
            float curS = data.currentStamina > 0f ? data.currentStamina : maxS;
            stamina.SetStamina(curS, maxS);
        }

        PlayerFood food = player.GetComponent<PlayerFood>();
        if (food != null)
        {
            float maxF = data.maxFood > 0f ? data.maxFood : (food.MaxFood > 0f ? food.MaxFood : 100f);
            float curF = data.currentFood > 0f ? data.currentFood : maxF;
            food.SetFood(curF, maxF);
            food.DecayTimer = data.foodDecayTimer;
        }

        PlayerExperience exp = player.GetComponent<PlayerExperience>();
        if (exp != null)
        {
            int lvl = data.currentLevel > 0 ? data.currentLevel : 1;
            float maxE = data.maxExp > 0f ? data.maxExp : 100f;
            exp.SetExpAndLevel(lvl, data.currentExp, maxE);
        }
    }

    private void RestoreInventoryData(GameSaveData data)
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.isInitialized = true;
            PlayerInventory.Instance.InitSlotsIfEmpty();
            PlayerInventory.Instance.ClearInventory();
            PlayerInventory.Instance.SetCoins(data.coins);

            PopulateInventorySlots(data.inventorySlots);
            PlayerInventory.Instance.RefreshInventoryUI();
        }

        PlayerHotbar hotbar = Object.FindFirstObjectByType<PlayerHotbar>(FindObjectsInactive.Include);
        if (hotbar != null)
        {
            hotbar.SelectSlot(data.currentHotbarIndex);
        }
    }

    private void PopulateInventorySlots(List<SavedSlotData> savedSlots)
    {
        if (savedSlots == null || PlayerInventory.Instance == null || PlayerInventory.Instance.slots == null) return;

        foreach (var savedSlot in savedSlots)
        {
            if (savedSlot == null || savedSlot.slotIndex < 0 || savedSlot.slotIndex >= PlayerInventory.Instance.slots.Length) continue;

            ItemData item = FindItemData(savedSlot.itemName);
            if (item != null)
            {
                PlayerInventory.Instance.slots[savedSlot.slotIndex].itemData = item;
                PlayerInventory.Instance.slots[savedSlot.slotIndex].amount = savedSlot.amount;
            }
        }
    }

    private void RestoreFarmingData(GameSaveData data)
    {
        RestoreTilledTiles(data.tilledTiles);
        RestorePlants(data.plants);
        RestorePlacedItems(data.placedItems);
    }

    private void RestoreTilledTiles(List<SavedTilePos> tilledTiles)
    {
        if (tilledTiles == null) return;

        Tilemap farmTilemap = GetFarmTilemap();
        if (farmTilemap == null) return;

        TileBase dirtTile = null;
        PlayerFarming pf = Object.FindFirstObjectByType<PlayerFarming>(FindObjectsInactive.Include);
        if (pf != null) dirtTile = pf.DirtTile;

        if (dirtTile == null) return;

        foreach (var t in tilledTiles)
        {
            farmTilemap.SetTile(new Vector3Int(t.x, t.y, t.z), dirtTile);
        }
    }

    private void RestorePlants(List<SavedPlantData> plants)
    {
        if (plants == null) return;

        foreach (var p in plants)
        {
            if (p == null) continue;
            ItemData seed = FindItemData(p.plantName) ?? FindItemData($"{p.plantName}Seed");
            GameObject prefab = (seed != null && seed.plantPrefab != null) ? seed.plantPrefab : Resources.Load<GameObject>(p.plantName);

            if (prefab == null) continue;

            Vector3 pos = new Vector3(p.posX, p.posY, p.posZ);
            GameObject plantObj = (PoolManager.Instance != null)
                ? PoolManager.Instance.Spawn(prefab, pos, Quaternion.identity)
                : Instantiate(prefab, pos, Quaternion.identity);

            if (plantObj != null && plantObj.TryGetComponent(out PlantController pc))
            {
                pc.SetPlantState(p.growthStageIndex, p.growthTimer, p.isFullyGrown);
            }
        }
    }

    private void RestorePlacedItems(List<SavedPlacedItemData> placedItems)
    {
        if (placedItems == null) return;

        List<ItemData> allItems = GetAllAvailableItems();

        foreach (var pi in placedItems)
        {
            if (pi == null || string.IsNullOrEmpty(pi.itemName)) continue;

            GameObject prefabToSpawn = ResolvePlacedItemPrefab(pi.itemName, allItems);

            if (prefabToSpawn != null)
            {
                Vector3 spawnPos = new Vector3(pi.posX, pi.posY, pi.posZ);
                if (PoolManager.Instance != null)
                {
                    PoolManager.Instance.Spawn(prefabToSpawn, spawnPos, Quaternion.identity);
                }
                else
                {
                    Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                }
            }
            else
            {
                Debug.LogWarning($"[SaveManager] Không thể tìm thấy Prefab để khôi phục bẫy: '{pi.itemName}'!");
            }
        }
    }

    private GameObject ResolvePlacedItemPrefab(string itemName, List<ItemData> allItems)
    {
        ItemData item = FindItemData(itemName);
        if (item != null && item.placeablePrefab != null)
        {
            return item.placeablePrefab;
        }

        if (allItems != null)
        {
            foreach (var dbItem in allItems)
            {
                if (dbItem != null && dbItem.placeablePrefab != null)
                {
                    string prefabName = dbItem.placeablePrefab.name.Trim();
                    if (prefabName.Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
                        prefabName.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        itemName.IndexOf(prefabName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return dbItem.placeablePrefab;
                    }
                }
            }
        }

        return Resources.Load<GameObject>(itemName);
    }

    private void RestoreWaveData(GameSaveData data)
    {
        if (EnemySpawn.Instance != null)
        {
            EnemySpawn.Instance.RestartWave(data.currentWaveCount, data.isWaitingForNextWave, data.waveCooldownTimer);
        }

        RestoreShopData(data.purchaseRecords);
    }

    private void RestoreShopData(List<SavedPurchaseRecord> purchaseRecords)
    {
        if (ShopManager.Instance == null) return;

        ShopManager.Instance.LoadPurchaseRecords(purchaseRecords);
        ShopManager.Instance.RefreshWeaponVisibility();
        ShopManager.Instance.UpdateCoinUI();
    }
    #endregion

    #region Điều Phối Dọn Dẹp Thế Giới (CleanupWorldState - Orchestrator)
    public void CleanupWorldState()
    {
        ClearEnemies();
        ClearPlants();
        ClearDrops();
        ClearTraps();
        ClearTiles();
    }

    private void ClearEnemies()
    {
        if (EnemySpawn.Instance != null)
        {
            EnemySpawn.Instance.ClearAllEnemies();
        }
        else
        {
            SafeReleaseAll<EnemyController>();
        }
    }

    private void ClearPlants()
    {
        SafeReleaseAll<PlantController>();
    }

    private void ClearDrops()
    {
        SafeReleaseAll<ItemPickUp>();
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
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveManager] Lưu ý khi quét tag Trap: {ex.Message}");
        }

        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb is IPlaceable && !(mb is PlantController))
            {
                SafeReleaseOrDestroy(mb.gameObject);
            }
        }
    }

    private void ClearTiles()
    {
        Tilemap farmTilemap = GetFarmTilemap();
        if (farmTilemap != null)
        {
            farmTilemap.ClearAllTiles();
        }
    }
    #endregion

    #region Pool & Memory Management Helpers
    private void SafeReleaseAll<T>() where T : Component
    {
        T[] objects = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var obj in objects)
        {
            if (obj != null)
            {
                SafeReleaseOrDestroy(obj.gameObject);
            }
        }
    }

    private void SafeReleaseOrDestroy(GameObject target)
    {
        if (target == null) return;

        if (PoolManager.Instance != null && target.GetComponent<PoolReturner>() != null)
        {
            PoolManager.Instance.Release(target);
        }
        else
        {
            Destroy(target);
        }
    }

    private Tilemap GetFarmTilemap()
    {
        Tilemap farmTilemap = FarmableArea.ActiveFarmTilemap;
        if (farmTilemap == null)
        {
            FarmableArea fa = Object.FindFirstObjectByType<FarmableArea>(FindObjectsInactive.Include);
            if (fa != null) farmTilemap = fa.GetComponent<Tilemap>();
        }
        return farmTilemap;
    }
    #endregion

    #region Helper Tra Cứu ItemData & Prefab
    public List<ItemData> GetAllAvailableItems()
    {
        List<ItemData> list = new List<ItemData>();
        if (itemDatabase != null && itemDatabase.Count > 0)
        {
            list.AddRange(itemDatabase);
        }

        if (ShopManager.Instance != null && ShopManager.Instance.AllProducts != null)
        {
            foreach (var prod in ShopManager.Instance.AllProducts)
            {
                if (prod != null && !list.Contains(prod))
                {
                    list.Add(prod);
                }
            }
        }

        ItemData[] resItems = Resources.LoadAll<ItemData>("");
        if (resItems != null)
        {
            foreach (var rItem in resItems)
            {
                if (rItem != null && !list.Contains(rItem))
                {
                    list.Add(rItem);
                }
            }
        }

        return list;
    }
    
    public ItemData FindItemData(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;

        if (itemCache.TryGetValue(itemName, out ItemData cached) && cached != null)
        {
            return cached;
        }

        if (itemDatabase != null)
        {
            foreach (var it in itemDatabase)
            {
                if (it != null && (it.name.Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
                                   (!string.IsNullOrEmpty(it.itemName) && it.itemName.Equals(itemName, StringComparison.OrdinalIgnoreCase))))
                {
                    itemCache[itemName] = it;
                    return it;
                }
            }
        }

        ItemData item = Resources.Load<ItemData>(itemName);
        if (item == null && ShopManager.Instance != null) item = ShopManager.Instance.FindProductByName(itemName);

        if (item != null) itemCache[itemName] = item;
        return item;
    }
    #endregion
}
