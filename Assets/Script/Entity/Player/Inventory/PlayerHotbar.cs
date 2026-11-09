using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHotbar : MonoBehaviour
{
    private PlayerInventory inventory;
    private PlayerEquipment equipment;
    private InventoryUIManager uiManager;

    public int currentHotbarIndex = -1;

    private void Awake()
    {
        EnsureReferences();
    }

    void Start()
    {
        EnsureReferences();
        SelectSlot(-1);
    }

    public void EnsureReferences()
    {
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
        if (equipment == null) equipment = GetComponent<PlayerEquipment>();
        if (uiManager == null) uiManager = Object.FindFirstObjectByType<InventoryUIManager>();
    }

    void Update()
    {
        // Chắc chắn bàn phím có kết nối
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectSlot(3);
            if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectSlot(4);
            if (Keyboard.current.digit6Key.wasPressedThisFrame) SelectSlot(5);
            if (Keyboard.current.digit7Key.wasPressedThisFrame) SelectSlot(6);
            if (Keyboard.current.digit8Key.wasPressedThisFrame) SelectSlot(7);
            if (Keyboard.current.digit9Key.wasPressedThisFrame) SelectSlot(8);
            if (Keyboard.current.digit0Key.wasPressedThisFrame) SelectSlot(9);
        }

        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > 0f)
            {
                int nextIndex = currentHotbarIndex - 1;
                if (nextIndex < 0) nextIndex = 9;
                SelectSlot(nextIndex);
            }
            else if (scroll < 0f)
            {
                int nextIndex = currentHotbarIndex + 1;
                if (nextIndex > 9) nextIndex = 0;
                SelectSlot(nextIndex);
            }
        }
    }

    public void SelectSlot(int hotbarKeyIndex)
    {
        EnsureReferences();

        if (hotbarKeyIndex < 0 || hotbarKeyIndex > 9)
        {
            currentHotbarIndex = -1;
            if (uiManager != null) uiManager.HighlightHotbarSlot(-1);
            if (equipment != null) equipment.EquipItem(null);
            return;
        }

        if (currentHotbarIndex == hotbarKeyIndex)
        {
            currentHotbarIndex = -1;
            if (uiManager != null) uiManager.HighlightHotbarSlot(-1);
            if (equipment != null) equipment.EquipItem(null);
            return;
        }

        currentHotbarIndex = hotbarKeyIndex;
        int realInventoryIndex = hotbarKeyIndex + 30;

        if (uiManager != null)
        {
            uiManager.HighlightHotbarSlot(realInventoryIndex);
        }

        if (inventory != null && inventory.slots != null && realInventoryIndex >= 0 && realInventoryIndex < inventory.slots.Length)
        {
            ItemData selectedItem = inventory.slots[realInventoryIndex]?.itemData;
            if (equipment != null) equipment.EquipItem(selectedItem);
        }
    }
}
