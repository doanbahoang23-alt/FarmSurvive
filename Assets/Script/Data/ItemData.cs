using UnityEngine;
using UnityEngine.Serialization;

public enum ItemType 
{ 
    Material,    
    Weapon,     
    Tool,        
    Seed,       
    Consumable, 
    Trap,
    Food
}

[CreateAssetMenu(fileName = "New Item", menuName = "KnightGame/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Điểm chung")]
    public string itemName;
    public Sprite icon;
    [TextArea(2, 4)]
    public int maxStack = 50;

    [Header("Phân loại")]
    public ItemType itemType = ItemType.Material;

    [Header("Vũ khí")]
    public ToolType toolType = ToolType.None;

    public WeaponData weaponData;

    public ToolType EffectiveToolType => (weaponData != null && weaponData.ToolType != ToolType.None) ? weaponData.ToolType : toolType;

    [Header("Vật phẩm đặt")]
    public GameObject worldPrefab;

    [Header("Đồ ăn")]
    [Min(0)]
    public float foodRestoreAmount = 0f;

    [Header("Đồ mua bán")]
    public bool isSellable = true;
    [Min(0)] public int sellPrice = 0;
    [Min(0)] public int buyPrice = 0;

    [Header("Cơ chế lạm phát giá (Chỉ dành cho Bẫy/Vũ khí)")]
    [Min(0)] public int priceIncrement = 10;

    public bool applyEscalatingPrice = false;

    [Header("Cơ chế Mua 1 Lần (Vũ khí / Bẫy / Đồ đặc biệt)")]
    public bool isOneTimePurchase = false;

    public int baseBuyPrice => buyPrice;
    public int GetEffectiveBaseBuyPrice() => buyPrice;

    public bool isHoe => ToolManager.CanTillSoil(EffectiveToolType);
    public bool isPickaxe => ToolManager.CanMineRock(EffectiveToolType) || toolType == ToolType.PickaxeAndHoe || (itemName != null && (itemName.IndexOf("pickaxe", System.StringComparison.OrdinalIgnoreCase) >= 0 || itemName.IndexOf("cúp", System.StringComparison.OrdinalIgnoreCase) >= 0));

    public GameObject plantPrefab => worldPrefab;

    public GameObject placeablePrefab => worldPrefab;

    public bool IsPlaceable => worldPrefab != null || itemType == ItemType.Seed || itemType == ItemType.Trap;

    public bool IsFood => itemType == ItemType.Food;

    public bool IsSeed => itemType == ItemType.Seed ||
        (!string.IsNullOrEmpty(itemName) && itemName.IndexOf("Seed", System.StringComparison.OrdinalIgnoreCase) >= 0) ||
        (!string.IsNullOrEmpty(name) && name.IndexOf("Seed", System.StringComparison.OrdinalIgnoreCase) >= 0);

    public bool CanSell => isSellable && GetEffectiveSellPrice() > 0;

    public int GetEffectiveSellPrice()
    {
        if (sellPrice > 0) return sellPrice;
        if (!isSellable) return 0;

        string n = (!string.IsNullOrEmpty(itemName) ? itemName : name).ToLower();
        if (n.Contains("wood") || n.Contains("gỗ")) return 2;
        if (n.Contains("stone") || n.Contains("rock") || n.Contains("đá")) return 4;

        return 0;
    }

    public int GetTotalSellPrice(int amount)
    {
        if (!CanSell || amount <= 0) return 0;
        return GetEffectiveSellPrice() * amount;
    }
}
