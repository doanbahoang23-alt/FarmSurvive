using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    #region 1. Metadata (Thông Tin Bản Lưu)
    public string saveDate;
    public string gameVersion;
    #endregion

    #region 2. Player Stats (Chỉ Số Sinh Tồn & Cấp Độ)
    public float posX;
    public float posY;
    public float posZ;

    public float currentHealth;
    public float maxHealth;

    public float currentStamina;
    public float maxStamina;

    public float currentFood;
    public float maxFood;
    public float foodDecayTimer;

    public int currentLevel;
    public float currentExp;
    public float maxExp;
    #endregion

    #region 3. Inventory & Hotbar (Túi Đồ & Tiền Tệ)
    public int coins;
    public int currentHotbarIndex;
    public List<SavedSlotData> inventorySlots = new List<SavedSlotData>();
    #endregion

    #region 4. Farming & World State (Nông Nghiệp & Môi Trường)
    public List<SavedTilePos> tilledTiles = new List<SavedTilePos>();
    public List<SavedPlantData> plants = new List<SavedPlantData>();
    public List<SavedPlacedItemData> placedItems = new List<SavedPlacedItemData>();
    #endregion

    #region 5. Shop System (Lịch Sử Mua Hàng & Lạm Phát Giá)
    public List<SavedPurchaseRecord> purchaseRecords = new List<SavedPurchaseRecord>();
    #endregion

    #region 6. Wave System (Đợt Quái & Thời Gian Chờ)
    public int currentWaveCount;
    public bool isWaitingForNextWave;
    public float waveCooldownTimer;
    #endregion

    public GameSaveData()
    {
        saveDate = string.Empty;
        gameVersion = "1.0.0";
        maxHealth = 100f;
        maxStamina = 100f;
        maxFood = 100f;
        maxExp = 100f;
        currentHealth = 100f;
        currentStamina = 100f;
        currentFood = 100f;
        currentLevel = 1;
    }
}

#region Các Lớp Dữ Liệu Con Bổ Trợ (DTO Subclasses)
/// <summary>
/// Dữ liệu của một ô đồ trong túi hoặc trên thanh hotbar
/// </summary>
[Serializable]
public class SavedSlotData
{
    public int slotIndex;
    public string itemName;
    public int amount;

    public SavedSlotData() { }

    public SavedSlotData(int slotIndex, string itemName, int amount)
    {
        this.slotIndex = slotIndex;
        this.itemName = itemName;
        this.amount = amount;
    }
}

/// <summary>
/// Tọa độ ô đất canh tác (xới đất) trên Tilemap
/// </summary>
[Serializable]
public class SavedTilePos
{
    public int x;
    public int y;
    public int z;

    public SavedTilePos() { }

    public SavedTilePos(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

/// <summary>
/// Trạng thái cây trồng trên ruộng (tên cây, vị trí, giai đoạn sinh trưởng)
/// </summary>
[Serializable]
public class SavedPlantData
{
    public string plantName;
    public float posX;
    public float posY;
    public float posZ;
    public int growthStageIndex;
    public float growthTimer;
    public bool isFullyGrown;

    public SavedPlantData() { }

    public SavedPlantData(string plantName, float posX, float posY, float posZ, int growthStageIndex, float growthTimer, bool isFullyGrown)
    {
        this.plantName = plantName;
        this.posX = posX;
        this.posY = posY;
        this.posZ = posZ;
        this.growthStageIndex = growthStageIndex;
        this.growthTimer = growthTimer;
        this.isFullyGrown = isFullyGrown;
    }
}

/// <summary>
/// Dữ liệu vật phẩm đặt trên mặt đất (bẫy, hàng rào, đuốc...)
/// </summary>
[Serializable]
public class SavedPlacedItemData
{
    public string itemName;
    public float posX;
    public float posY;
    public float posZ;

    public SavedPlacedItemData() { }

    public SavedPlacedItemData(string itemName, float posX, float posY, float posZ)
    {
        this.itemName = itemName;
        this.posX = posX;
        this.posY = posY;
        this.posZ = posZ;
    }
}

/// <summary>
/// Số lần đã mua của vật phẩm trong Shop (dùng để tính lạm phát giá bẫy)
/// </summary>
[Serializable]
public class SavedPurchaseRecord
{
    public string itemName;
    public int purchaseCount;

    public SavedPurchaseRecord() { }

    public SavedPurchaseRecord(string itemName, int purchaseCount)
    {
        this.itemName = itemName;
        this.purchaseCount = purchaseCount;
    }
}
#endregion
