using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ShopManager : MonoBehaviour
{
    #region Singleton & Events
    public static ShopManager Instance { get; private set; }
    public event Action<ItemData> OnItemPurchased;
    #endregion

    #region Serialized Fields & Shop Catalog
    [Header("UI GRID")]
    [SerializeField] private Transform seedGrid;
    [SerializeField] private Transform cropGrid;
    [SerializeField] private Transform trapGrid;
    [SerializeField] private Transform weaponGrid;

    [Header("Coin Text & Slot Prefab")]
    [SerializeField] private GameObject shopSlotPrefab;
    [SerializeField] private TextMeshProUGUI playerCoinText;

    [Header("Shop Item Catalogs")]
    [SerializeField] private List<ItemData> seedProducts = new List<ItemData>();
    [SerializeField] private List<ItemData> foodProducts = new List<ItemData>();
    [SerializeField] private List<ItemData> trapProducts = new List<ItemData>();
    [SerializeField] private List<ItemData> weaponProducts = new List<ItemData>();

    private readonly List<ShopSlotUI> weaponSlotUIs = new List<ShopSlotUI>();
    private readonly Dictionary<ItemData, int> trapPurchaseCounts = new Dictionary<ItemData, int>();
    #endregion

    #region Unity Lifecycles & Event Subscriptions
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }
    }

    private void Start()
    {
        InitShopUI();
    }

    private void OnEnable()
    {
        RefreshWeaponVisibility();
        UpdateCoinUI();

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnCoinChanged += OnPlayerCoinChanged;
            PlayerInventory.Instance.OnInventoryChanged += RefreshWeaponVisibility;
        }
    }

    private void OnDisable()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnCoinChanged -= OnPlayerCoinChanged;
            PlayerInventory.Instance.OnInventoryChanged -= RefreshWeaponVisibility;
        }
    }
    #endregion

    #region Shop UI Initialization & Grid Generation
    public void InitShopUI()
    {
        ClearGrid(seedGrid);
        ClearGrid(cropGrid);
        ClearGrid(trapGrid);
        ClearGrid(weaponGrid);

        weaponSlotUIs.Clear();

        if (shopSlotPrefab == null)
        {
            Debug.LogError("[ShopManager] Chưa gán shopSlotPrefab trong Inspector của ShopPanel!");
            return;
        }

        SpawnSlotsForGrid(seedGrid, seedProducts, 4, false);
        SpawnSlotsForGrid(cropGrid, foodProducts, 4, false);
        SpawnSlotsForGrid(trapGrid, trapProducts, 2, false);
        SpawnSlotsForGrid(weaponGrid, weaponProducts, 4, true);

        RefreshWeaponVisibility();
        UpdateCoinUI();
    }

    private Transform GetActualGridTransform(Transform target)
    {
        if (target == null) return null;

        GridLayoutGroup childGrid = target.GetComponentInChildren<GridLayoutGroup>(true);
        if (childGrid != null)
        {
            childGrid.cellSize = new Vector2(94f, 94f);
            childGrid.spacing = new Vector2(8f, 8f);
            return childGrid.transform;
        }

        GridLayoutGroup myGrid = target.GetComponent<GridLayoutGroup>();
        if (myGrid != null)
        {
            myGrid.cellSize = new Vector2(94f, 94f);
            myGrid.spacing = new Vector2(8f, 8f);
            return target;
        }

        VerticalLayoutGroup vlg = target.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            Destroy(vlg);
        }

        myGrid = target.gameObject.AddComponent<GridLayoutGroup>();
        myGrid.cellSize = new Vector2(94f, 94f);
        myGrid.spacing = new Vector2(8f, 8f);
        myGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        myGrid.constraintCount = 2;
        myGrid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter csf = target.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = target.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return target;
    }

    private void ClearGrid(Transform gridTransform)
    {
        if (gridTransform == null) return;
        Transform actualGrid = GetActualGridTransform(gridTransform);
        if (actualGrid == null) return;

        for (int i = actualGrid.childCount - 1; i >= 0; i--)
        {
            Transform child = actualGrid.GetChild(i);
            if (child.name.Contains("Title") || child.GetComponent<TextMeshProUGUI>() != null)
            {
                continue;
            }
            Destroy(child.gameObject);
        }
    }

    private void SpawnSlotsForGrid(Transform gridTransform, List<ItemData> items, int maxCount, bool isWeaponCategory)
    {
        if (gridTransform == null || items == null) return;
        Transform actualGrid = GetActualGridTransform(gridTransform);
        if (actualGrid == null) return;

        int count = Mathf.Min(items.Count, maxCount);
        for (int i = 0; i < count; i++)
        {
            ItemData item = items[i];
            if (item == null) continue;

            GameObject slotObj = Instantiate(shopSlotPrefab, actualGrid);
            ShopSlotUI slotUI = slotObj.GetComponent<ShopSlotUI>();
            if (slotUI != null)
            {
                slotUI.Setup(item, this);

                if (isWeaponCategory)
                {
                    weaponSlotUIs.Add(slotUI);
                }
            }
        }
    }
    #endregion

    #region Price & Inflation Calculation
    public int GetCurrentPrice(ItemData item)
    {
        if (item == null) return 0;

        if (IsTrap(item))
        {
            int purchasedTimes = 0;
            trapPurchaseCounts.TryGetValue(item, out purchasedTimes);
            return item.buyPrice + (purchasedTimes * 10);
        }

        return item.buyPrice;
    }
    #endregion

    #region Purchase & Transaction Orchestration (SRP)
    public bool BuyItem(ItemData item, ShopSlotUI clickedSlot = null)
    {
        if (item == null) return false;

        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("[ShopManager] Không tìm thấy PlayerInventory.Instance!");
            return false;
        }

        int currentPrice = GetCurrentPrice(item);

        // 1. Kiểm tra điều kiện đủ tiền
        if (!CanAffordItem(currentPrice))
        {
            HandleInsufficientFunds(item, currentPrice);
            return false;
        }

        // 2. Thực hiện giao dịch tài chính & túi đồ (SRP)
        ProcessTransaction(item, currentPrice);

        // 3. Ghi nhận lịch sử mua hàng & lạm phát giá (SRP)
        RecordPurchaseHistory(item);

        // 4. Phát âm thanh mua hàng
        PlayPurchaseAudio();

        // 5. Cập nhật giao diện Cửa Hàng (SRP)
        UpdateShopUI(item, clickedSlot);

        // 6. Phát Event C# thông báo mua hàng thành công
        OnItemPurchased?.Invoke(item);

        Debug.Log($"<color=#70E880>[Shop] Mua thành công {item.itemName} với giá {currentPrice} Coin!</color>");
        return true;
    }

    private bool CanAffordItem(int price)
    {
        return PlayerInventory.Instance != null && PlayerInventory.Instance.Coins >= price;
    }

    public void ProcessTransaction(ItemData item, int price)
    {
        PlayerInventory.Instance.RemoveCoin(price);
        PlayerInventory.Instance.AddItem(item);
    }

    public void RecordPurchaseHistory(ItemData item)
    {
        if (!IsTrap(item)) return;

        if (trapPurchaseCounts.ContainsKey(item))
        {
            trapPurchaseCounts[item]++;
        }
        else
        {
            trapPurchaseCounts[item] = 1;
        }
    }

    private void HandleInsufficientFunds(ItemData item, int price)
    {
        Debug.Log($"<color=yellow>[Shop] Không đủ tiền mua {item.itemName}! Cần {price} Coin, hiện có {PlayerInventory.Instance.Coins} Coin.</color>");

        if (AudioManager.Instance != null && AudioManager.Instance.plantFailClip != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.plantFailClip);
        }
    }

    private void PlayPurchaseAudio()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.buyItemClip != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buyItemClip);
        }
    }
    #endregion

    #region Shop UI Updates & Presentation
    public void UpdateShopUI(ItemData purchasedItem = null, ShopSlotUI clickedSlot = null)
    {
        // 1. Cập nhật lại giá 
        if (clickedSlot != null)
        {
            clickedSlot.RefreshPrice();
        }

        // 2. Nếu là vũ khí/công cụ: cập nhật ẩn ô slot (Sold Out)
        if (purchasedItem != null && IsWeapon(purchasedItem))
        {
            RefreshWeaponVisibility();
        }

        // 3. Cập nhật lại số dư Coin 
        UpdateCoinUI();
    }

    public void RefreshWeaponVisibility()
    {
        if (PlayerInventory.Instance == null) return;

        bool hasChanged = false;
        foreach (ShopSlotUI slotUI in weaponSlotUIs)
        {
            if (slotUI != null && slotUI.ItemData != null)
            {
                bool playerHasItem = PlayerInventory.Instance.HasItem(slotUI.ItemData);
                bool shouldBeActive = !playerHasItem;
                if (slotUI.gameObject.activeSelf != shouldBeActive)
                {
                    slotUI.gameObject.SetActive(shouldBeActive);
                    hasChanged = true;
                }
            }
        }

        if (hasChanged && weaponGrid != null)
        {
            RectTransform wGridRect = weaponGrid.GetComponent<RectTransform>();
            if (wGridRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(wGridRect);
            }
        }
    }

    public void UpdateCoinUI()
    {
        if (playerCoinText != null)
        {
            int currentCoins = PlayerInventory.Instance != null ? PlayerInventory.Instance.Coins : 0;
            playerCoinText.text = currentCoins.ToString();
        }
    }

    private void OnPlayerCoinChanged(int newCoinAmount)
    {
        if (playerCoinText != null)
        {
            playerCoinText.text = newCoinAmount.ToString();
        }

        // Tự động kiểm tra và hiện lại slot vũ khí nếu người chơi vừa bán đồ
        RefreshWeaponVisibility();
    }
    #endregion

    #region Product Catalog Queries & Helpers
    private bool IsTrap(ItemData item)
    {
        return item != null && (item.itemType == ItemType.Trap || trapProducts.Contains(item));
    }

    private bool IsWeapon(ItemData item)
    {
        return item != null && (item.itemType == ItemType.Weapon || item.itemType == ItemType.Tool || weaponProducts.Contains(item));
    }

    public void ResetPurchaseCounts()
    {
        trapPurchaseCounts.Clear();
    }

    public ItemData FindProductByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;
        foreach (var it in seedProducts) if (it != null && it.name.Equals(itemName, StringComparison.OrdinalIgnoreCase)) return it;
        foreach (var it in foodProducts) if (it != null && it.name.Equals(itemName, StringComparison.OrdinalIgnoreCase)) return it;
        foreach (var it in trapProducts) if (it != null && it.name.Equals(itemName, StringComparison.OrdinalIgnoreCase)) return it;
        foreach (var it in weaponProducts) if (it != null && it.name.Equals(itemName, StringComparison.OrdinalIgnoreCase)) return it;
        return null;
    }

    public List<ItemData> TrapProducts => trapProducts;

    public List<ItemData> AllProducts
    {
        get
        {
            List<ItemData> list = new List<ItemData>();
            if (seedProducts != null) list.AddRange(seedProducts);
            if (foodProducts != null) list.AddRange(foodProducts);
            if (trapProducts != null) list.AddRange(trapProducts);
            if (weaponProducts != null) list.AddRange(weaponProducts);
            return list;
        }
    }

    public Dictionary<ItemData, int> TrapPurchaseCounts => trapPurchaseCounts;

    public Dictionary<string, int> itemPurchaseCounts
    {
        get
        {
            Dictionary<string, int> records = new Dictionary<string, int>();
            foreach (var pair in trapPurchaseCounts)
            {
                if (pair.Key != null)
                {
                    records[pair.Key.name] = pair.Value;
                }
            }
            return records;
        }
    }
    #endregion

    #region Save / Load Persistence Integration
    public void LoadPurchaseRecords(List<SavedPurchaseRecord> records)
    {
        trapPurchaseCounts.Clear();
        if (records == null) return;
        foreach (var rec in records)
        {
            if (string.IsNullOrEmpty(rec.itemName)) continue;
            ItemData item = Resources.Load<ItemData>(rec.itemName);
            if (item != null)
            {
                trapPurchaseCounts[item] = rec.purchaseCount;
            }
        }
    }
    #endregion

    #region Editor Utilities
#if UNITY_EDITOR
    [ContextMenu("Tự Động Nạp Item Vào 4 Phân Khu Từ Project")]
    public void AutoPopulateItemsFromProject()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
        seedProducts.Clear();
        foodProducts.Clear();
        trapProducts.Clear();
        weaponProducts.Clear();

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null || item.buyPrice <= 0) continue;

            if (item.IsSeed || item.itemType == ItemType.Seed)
            {
                if (!seedProducts.Contains(item)) seedProducts.Add(item);
            }
            else if (item.IsFood || item.itemType == ItemType.Food)
            {
                if (!foodProducts.Contains(item)) foodProducts.Add(item);
            }
            else if (item.itemType == ItemType.Trap)
            {
                if (!trapProducts.Contains(item)) trapProducts.Add(item);
            }
            else if (item.itemType == ItemType.Weapon || item.itemType == ItemType.Tool)
            {
                if (!weaponProducts.Contains(item)) weaponProducts.Add(item);
            }
        }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"<color=#70E880>[ShopManager] Đã nạp: {seedProducts.Count} Hạt giống, {foodProducts.Count} Nông sản/Đồ ăn, {trapProducts.Count} Bẫy, {weaponProducts.Count} Vũ khí/Công cụ!</color>");
    }
#endif
    #endregion
}
