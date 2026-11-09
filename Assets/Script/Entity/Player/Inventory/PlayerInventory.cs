using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [SerializeField] private int totalSlots = 34;
    public InventorySlotData[] slots;
    public InventoryUIManager uiManager;

    [Header("Currency")]
    [Tooltip("Số tiền vàng người chơi đang có")]
    [SerializeField] private int coins = 0;
    [Tooltip("Số tiền vàng khởi đầu cấp cho người chơi ở ván mới (mặc định: 0)")]
    [SerializeField] private int startingCoins = 0;
    public int Coins => coins;
    public int StartingCoins => startingCoins;
    public event Action<int> OnCoinChanged;
    public event Action OnInventoryChanged;

    [Header("Vật Phẩm Khởi Đầu Tân Thủ (New Game Starting Items)")]
    [Tooltip("Danh sách trang bị khi tạo ván mới. Để chỉ nhận Cung & Giáo: Trong Inspector đặt Size = 2, Element 0 kéo Bow.asset, Element 1 kéo Spear.asset")]
    [SerializeField] private ItemData[] startingItems;
    [Tooltip("Số lượng khởi đầu cho vật phẩm tiêu hao (hạt giống, đồ ăn). Riêng Vũ khí và Công cụ sẽ luôn chỉ nhận đúng 1 món.")]
    [SerializeField] private int startingAmount = 5;

    /// <summary>
    /// Biến cờ bảo vệ túi đồ đã được khởi tạo.
    /// Khi Load Game từ file save, SaveManager sẽ gán isInitialized = true để ngăn Start() nạp đè đồ tân thủ.
    /// </summary>
    [HideInInspector] public bool isInitialized = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        InitSlotsIfEmpty();
    }

    public void InitSlotsIfEmpty()
    {
        if (slots == null || slots.Length != totalSlots)
        {
            slots = new InventorySlotData[totalSlots];
            for (int i = 0; i < totalSlots; i++) slots[i] = new InventorySlotData();
        }
    }

    void Start()
    {
        InitSlotsIfEmpty();

        // Chỉ cấp vật phẩm tân thủ nếu là Ván Chơi Mới
        if (!isInitialized)
        {
            if (coins <= 0 && startingCoins > 0)
            {
                coins = startingCoins;
            }

            GiveStartingItems();
        }

        if (uiManager != null)
        {
            uiManager.UpdateUI(slots);
            uiManager.UpdateCoinUI(coins);
        }
        OnCoinChanged?.Invoke(coins);
    }

    /// <summary>
    /// Cấp trang bị khởi đầu tân thủ (Spear, Bow theo danh sách startingItems)
    /// </summary>
    public void GiveStartingItems()
    {
        InitSlotsIfEmpty();

        if (startingItems != null && startingItems.Length > 0)
        {
            foreach (ItemData item in startingItems)
            {
                if (item != null)
                {
                    bool isEquipment = item.maxStack == 1 || item.itemType == ItemType.Weapon || item.itemType == ItemType.Tool;
                    int amountToAdd = isEquipment ? 1 : startingAmount;
                    AddItem(item, amountToAdd);
                }
            }
        }
        else
        {
            // Dự phòng: Tìm Spear trong project nếu mảng startingItems chưa được kéo
            ItemData spear = Resources.Load<ItemData>("Spear");
            if (spear != null)
            {
                AddItem(spear, 1);
            }
        }

        isInitialized = true;

        if (uiManager != null)
        {
            uiManager.UpdateUI(slots);
            uiManager.UpdateCoinUI(coins);
        }
        OnInventoryChanged?.Invoke();
    }

    public void AddCoins(int amountToAdd)
    {
        if (amountToAdd <= 0) return;
        coins += amountToAdd;
        OnCoinChanged?.Invoke(coins);
        if (uiManager != null)
        {
            uiManager.UpdateCoinUI(coins);
        }
    }

    public void SetCoins(int amount)
    {
        coins = Mathf.Max(0, amount);
        OnCoinChanged?.Invoke(coins);
        if (uiManager != null)
        {
            uiManager.UpdateCoinUI(coins);
        }
    }

    public void RefreshInventoryUI()
    {
        if (uiManager != null)
        {
            uiManager.UpdateUI(slots);
            uiManager.UpdateCoinUI(coins);
        }
        OnCoinChanged?.Invoke(coins);
        OnInventoryChanged?.Invoke();
    }

    public void ClearInventory()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].itemData = null;
                slots[i].amount = 0;
            }
        }
        if (uiManager != null) uiManager.UpdateUI(slots);

        OnInventoryChanged?.Invoke();
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RefreshWeaponVisibility();
        }
    }

    public void ResetToStartingItems()
    {
        InitSlotsIfEmpty();
        ClearInventory();
        SetCoins(startingCoins);
        GiveStartingItems();
    }

    /// <summary>
    /// Kiểm tra xem túi đồ có đủ chỗ chứa lượng vật phẩm này không
    /// </summary>
    public bool CanAddItem(ItemData itemToAdd, int amountToAdd = 1)
    {
        if (itemToAdd == null || slots == null || amountToAdd <= 0) return false;
        int remaining = amountToAdd;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null || slots[i].IsEmpty()) return true;
            if (slots[i].itemData == itemToAdd && slots[i].amount < itemToAdd.maxStack)
            {
                remaining -= (itemToAdd.maxStack - slots[i].amount);
                if (remaining <= 0) return true;
            }
        }
        return false;
    }

    public bool RemoveCoins(int amountToRemove)
    {
        if (amountToRemove <= 0 || coins < amountToRemove) return false;
        coins -= amountToRemove;
        OnCoinChanged?.Invoke(coins);
        if (uiManager != null)
        {
            uiManager.UpdateCoinUI(coins);
        }
        return true;
    }

    public bool RemoveCoin(int amount) => RemoveCoins(amount);

    /// <summary>
    /// Thử trừ tiền an toàn khi mua hàng hoặc nâng cấp
    /// </summary>
    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0) return true;
        return RemoveCoins(amount);
    }

    public void AddItem(ItemData itemToAdd, int amountToAdd = 1)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty() && slots[i].itemData == itemToAdd && slots[i].amount < itemToAdd.maxStack)
            {
                int spaceLeft = itemToAdd.maxStack - slots[i].amount;
                if (amountToAdd <= spaceLeft)
                {
                    slots[i].amount += amountToAdd;
                    amountToAdd = 0;
                    break;
                }
                else
                {
                    slots[i].amount = itemToAdd.maxStack;
                    amountToAdd -= spaceLeft;
                }
            }
        }

        if (amountToAdd > 0)
        {
            // Nếu là Vũ khí hoặc Công cụ và túi đồ có hàng Hotbar (slot 30-39), ưu tiên đặt vào ô Hotbar trống trước
            bool isEquipment = itemToAdd.itemType == ItemType.Weapon || itemToAdd.itemType == ItemType.Tool;
            if (isEquipment && slots.Length >= 40)
            {
                for (int i = 30; i < slots.Length; i++)
                {
                    if (slots[i].IsEmpty())
                    {
                        slots[i].itemData = itemToAdd;
                        slots[i].amount = 1;
                        amountToAdd -= 1;
                        break;
                    }
                }
            }

            // Nếu chưa được xếp vào Hotbar (hoặc là vật phẩm thông thường), xếp vào ô trống đầu tiên trong túi đồ
            if (amountToAdd > 0)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i].IsEmpty())
                    {
                        slots[i].itemData = itemToAdd;
                        if (amountToAdd <= itemToAdd.maxStack)
                        {
                            slots[i].amount = amountToAdd;
                            amountToAdd = 0;
                            break;
                        }
                        else
                        {
                            slots[i].amount = itemToAdd.maxStack;
                            amountToAdd -= itemToAdd.maxStack;
                        }
                    }
                }
            }
        }

        if (uiManager != null) uiManager.UpdateUI(slots);

        OnInventoryChanged?.Invoke();
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RefreshWeaponVisibility();
        }
    }

    public void RemoveItem(ItemData itemToRemove, int amountToRemove)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].itemData == itemToRemove)
            {
                if (slots[i].amount >= amountToRemove)
                {
                    slots[i].amount -= amountToRemove;
                    amountToRemove = 0;

                    if (slots[i].amount == 0)
                    {
                        slots[i].itemData = null;
                    }

                    break;
                }
                else
                {
                    amountToRemove -= slots[i].amount;
                    slots[i].amount = 0;
                    slots[i].itemData = null;
                }
            }
        }

        if (uiManager != null) uiManager.UpdateUI(slots);

        OnInventoryChanged?.Invoke();
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RefreshWeaponVisibility();
        }
    }

    public int GetItemAmount(ItemData itemToCount)
    {
        int totalAmount = 0;

        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty() && slots[i].itemData == itemToCount)
            {
                totalAmount += slots[i].amount;
            }
        }

        return totalAmount;
    }

    /// <summary>
    /// Kiểm tra người chơi có đang sở hữu ít nhất 1 món đồ này trong túi không (so sánh cả tham chiếu và tên)
    /// </summary>
    public bool HasItem(ItemData item)
    {
        if (item == null || slots == null) return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && !slots[i].IsEmpty() && slots[i].itemData != null)
            {
                if (slots[i].itemData == item ||
                    (!string.IsNullOrEmpty(item.itemName) && slots[i].itemData.itemName == item.itemName) ||
                    slots[i].itemData.name == item.name)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool TryConsumeFoodAtSlot(int slotIndex)
    {
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return false;
        InventorySlotData slot = slots[slotIndex];
        if (slot == null || slot.IsEmpty() || slot.itemData == null) return false;

        ItemData item = slot.itemData;
        // CHỈ cho phép ăn nếu vật phẩm là Thức Ăn (ItemType.Food)
        if (!item.IsFood) return false;

        PlayerFood food = GetComponent<PlayerFood>() ?? PlayerFood.Instance;
        if (food != null)
        {
            float restoreAmount = item.foodRestoreAmount > 0f ? item.foodRestoreAmount : 20f;
            food.RestoreFood(restoreAmount);

            slot.amount--;
            if (slot.amount <= 0)
            {
                slot.amount = 0;
                slot.itemData = null;
            }

            if (uiManager != null) uiManager.UpdateUI(slots);

        OnInventoryChanged?.Invoke();
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RefreshWeaponVisibility();
        }

            PlayerEquipment equip = GetComponent<PlayerEquipment>();
            if (equip != null && equip.currentItem == item)
            {
                if (GetItemAmount(item) <= 0)
                {
                    equip.EquipItem(null);
                }
            }

            return true;
        }

        return false;
    }

    public bool SellItemAtSlot(int slotIndex, bool sellEntireStack = true)
    {
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return false;

        InventorySlotData slot = slots[slotIndex];
        if (slot == null || slot.IsEmpty() || slot.itemData == null || !slot.itemData.CanSell)
        {
            return false;
        }

        int amountToSell = sellEntireStack ? slot.amount : 1;
        int coinsEarned = slot.itemData.GetTotalSellPrice(amountToSell);

        slot.amount -= amountToSell;
        if (slot.amount <= 0)
        {
            slot.amount = 0;
            slot.itemData = null;

            // Nếu slot này đang được equip qua hotbar thì bỏ trang bị
            PlayerHotbar hotbar = GetComponent<PlayerHotbar>();
            if (hotbar != null && hotbar.currentHotbarIndex >= 0 && (hotbar.currentHotbarIndex + 30) == slotIndex)
            {
                PlayerEquipment equipment = GetComponent<PlayerEquipment>();
                if (equipment != null)
                {
                    equipment.EquipItem(null);
                }
            }
        }

        AddCoins(coinsEarned);

        if (AudioManager.Instance != null && AudioManager.Instance.sellItemClip != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.sellItemClip);
        }

        if (uiManager != null)
        {
            uiManager.UpdateUI(slots);
        }

        OnInventoryChanged?.Invoke();

        // Tự động thông báo cho ShopManager để mở lại slot vũ khí nếu vừa bán
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RefreshWeaponVisibility();
        }

        return true;
    }

    private void OnToggleInventory(InputValue inputValue)
    {
        if (inputValue.isPressed)
        {
            if (uiManager != null)
            {
                uiManager.ToggleInventory();
                uiManager.UpdateCoinUI(coins);
            }
        }
    }
}
