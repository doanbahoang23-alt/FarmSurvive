using UnityEngine;

[System.Serializable]
public class InventorySlotData
{
    public ItemData itemData;
    public int amount = 0;

    public bool IsEmpty()
    {
        return itemData == null || amount <= 0;
    }
    public bool IsFood => !IsEmpty() && itemData.IsFood;
    public bool IsPlaceable => !IsEmpty() && itemData.IsPlaceable;
}
[System.Serializable]
public class ItemInventory : InventorySlotData
{
    public ItemInventory() : base() { }

    public ItemInventory(ItemData data, int count)
    {
        itemData = data;
        amount = count;
    }
}
