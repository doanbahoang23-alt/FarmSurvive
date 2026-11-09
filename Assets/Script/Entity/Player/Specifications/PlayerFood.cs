using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PlayerFood : MonoBehaviour
{
    public static PlayerFood Instance { get; private set; }

    [Header("Cấu hình Thức Ăn (Food / Hunger)")]
    [SerializeField] private float maxFood = 100f;
    private float currentFood;

    public float CurrentFood => currentFood;
    public float MaxFood => maxFood;
    public bool IsStarving => currentFood <= 0f;

    public event Action<float, float> OnFoodChanged;
    public event Action<GameObject> OnStarving;

    [Header("Cơ chế Tiêu Hao Tự Nhiên (Đói Theo Thời Gian)")]
    [Tooltip("Lượng thức ăn giảm sau mỗi chu kỳ (mặc định 4 đơn vị)")]
    [SerializeField] private float decayAmount = 4f;

    [Tooltip("Thời gian giữa các lần giảm thức ăn (mặc định 60 giây = 1 phút)")]
    [SerializeField] private float decayInterval = 60f;

    [Header("Cơ chế Đói & Chết Đói (Starvation Damage)")]
    [Tooltip("Lượng máu bị trừ mỗi chu kỳ khi thanh đói chạm mốc 0 (mặc định 2 HP/giây)")]
    [SerializeField] private float starvationDamagePerSecond = 2f;
    [Tooltip("Khoảng thời gian trừ máu định kỳ khi đói (1 giây)")]
    [SerializeField] private float starvationInterval = 1f;

    private float decayTimer = 0f;
    private float starvationTimer = 0f;
    private bool eatRequested = false;

    public float DecayTimer
    {
        get => decayTimer;
        set => decayTimer = value;
    }

    #region Component References
    private BaseHealth playerHealth;
    private PlayerEquipment equipment;
    private PlayerInventory inventory;
    private PlayerHotbar hotbar;
    #endregion

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

        currentFood = maxFood;
        playerHealth = GetComponent<BaseHealth>();
        equipment = GetComponent<PlayerEquipment>();
        inventory = GetComponent<PlayerInventory>();
        hotbar = GetComponent<PlayerHotbar>();
    }

    private void Start()
    {
        decayTimer = 0f;
        NotifyFoodChanged();
    }

    private void Update()
    {
        HandleFoodDecay();
        HandleStarvation();

        if (eatRequested)
        {
            eatRequested = false;
            TryEat();
        }
    }

    #region Food Decay & Stats Logic
    /// <summary>
    /// Xử lý trừ máu định kỳ khi nhân vật đói kiệt sức (CurrentFood <= 0)
    /// </summary>
    private void HandleStarvation()
    {
        if (Time.timeScale <= 0f) return;
        if (playerHealth == null) playerHealth = GetComponent<BaseHealth>();
        if (playerHealth != null && playerHealth.IsDead) return;

        if (IsStarving)
        {
            starvationTimer += Time.deltaTime;
            if (starvationTimer >= starvationInterval)
            {
                starvationTimer = 0f;
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(starvationDamagePerSecond);
                }
            }
        }
        else
        {
            // Khi có thức ăn (CurrentFood > 0), dừng ngay lập tức vòng lặp trừ máu
            starvationTimer = 0f;
        }
    }

    /// <summary>
    /// Xử lý giảm lượng thức ăn theo chu kỳ
    /// </summary>
    private void HandleFoodDecay()
    {
        if (Time.timeScale <= 0f) return;
        if (playerHealth != null && playerHealth.IsDead) return;

        decayTimer += Time.deltaTime;

        if (decayTimer >= decayInterval)
        {
            decayTimer = 0f;
            ConsumeFood(decayAmount);
        }
    }

    public void ConsumeFood(float amount)
    {
        if (amount <= 0f) return;

        currentFood -= amount;
        currentFood = Mathf.Clamp(currentFood, 0f, maxFood);
        NotifyFoodChanged();

        if (currentFood <= 0f)
        {
            OnStarving?.Invoke(gameObject);
        }
    }

    public void RestoreFood(float amount)
    {
        if (amount <= 0f) return;

        currentFood += amount;
        currentFood = Mathf.Clamp(currentFood, 0f, maxFood);
        starvationTimer = 0f; // Reset ngay khi người chơi ăn thức ăn
        NotifyFoodChanged();
    }

    public void SetFood(float current, float max = -1f)
    {
        if (max > 0f) maxFood = max;
        currentFood = Mathf.Clamp(current, 0f, maxFood);
        if (currentFood > 0f) starvationTimer = 0f;
        NotifyFoodChanged();
    }

    private void NotifyFoodChanged()
    {
        OnFoodChanged?.Invoke(currentFood, maxFood);
    }
    #endregion

    #region Eating Logic (Tích hợp từ PlayerEating)
    private void OnEat(InputValue inputValue)
    {
        if (inputValue.isPressed)
        {
            eatRequested = true;
        }
    }

    public void TryEat()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (inventory != null && inventory.uiManager != null && inventory.uiManager.inventoryPanel != null && inventory.uiManager.inventoryPanel.activeSelf)
        {
            return;
        }

        if (equipment == null) equipment = GetComponent<PlayerEquipment>();
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
        if (hotbar == null) hotbar = GetComponent<PlayerHotbar>();

        if (equipment == null || equipment.currentItem == null || !equipment.currentItem.IsFood)
        {
            return;
        }

        ItemData foodItem = equipment.currentItem;

        // Ăn và hồi phục Food
        Eat(foodItem);

        // Trừ số lượng món ăn trong Hotbar hoặc Inventory
        bool consumed = false;
        if (hotbar != null && hotbar.currentHotbarIndex >= 0 && inventory != null && inventory.slots != null)
        {
            int slotIndex = hotbar.currentHotbarIndex + 30;
            if (slotIndex >= 0 && slotIndex < inventory.slots.Length)
            {
                InventorySlotData slot = inventory.slots[slotIndex];
                if (slot != null && slot.itemData == foodItem && slot.amount > 0)
                {
                    slot.amount--;
                    consumed = true;

                    if (slot.amount <= 0)
                    {
                        slot.amount = 0;
                        slot.itemData = null;
                        equipment.EquipItem(null);
                    }

                    if (inventory.uiManager != null)
                    {
                        inventory.uiManager.UpdateUI(inventory.slots);
                    }
                }
            }
        }

        if (!consumed && inventory != null)
        {
            inventory.RemoveItem(foodItem, 1);
            if (inventory.GetItemAmount(foodItem) <= 0)
            {
                equipment.EquipItem(null);
            }
        }
    }

    /// <summary>
    /// Tiêu thụ thức ăn và hồi phục thanh Food theo foodRestoreAmount của món ăn
    /// </summary>
    public void Eat(ItemData foodItem)
    {
        if (foodItem == null || !foodItem.IsFood) return;

        RestoreFood(foodItem.foodRestoreAmount);
    }
    #endregion
}
