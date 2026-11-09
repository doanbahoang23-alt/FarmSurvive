using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [SerializeField] private int slotIndex;
    [SerializeField] private PlayerInventory inventory;

    [Header("Drag and Drop UI")]
    [SerializeField] private Image itemIcon;
    static GameObject dragIconObj;
    [SerializeField] private TextMeshProUGUI amountText;

    public int SlotIndex => slotIndex;

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<PlayerInventory>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return;

        // Click chuột phải: Tiêu thụ nếu là Thức ăn (Food)
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (inventory != null && inventory.TryConsumeFoodAtSlot(slotIndex))
            {
                return;
            }
        }

        if (inventory != null && inventory.uiManager != null)
        {
            inventory.uiManager.OnSlotClicked(slotIndex);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventory == null || inventory.slots == null || slotIndex < 0 || slotIndex >= inventory.slots.Length) return;
        if (inventory.slots[slotIndex].IsEmpty()) return;

        dragIconObj = new GameObject("DragIcon");
        dragIconObj.transform.SetParent(transform.root);
        dragIconObj.transform.SetAsLastSibling();

        Image img = dragIconObj.AddComponent<Image>();
        img.sprite = itemIcon != null ? itemIcon.sprite : null;
        img.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIconObj != null)
        {
            dragIconObj.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIconObj != null)
        {
            Destroy(dragIconObj);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI draggedSlotUI = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<InventorySlotUI>() : null;

        if (draggedSlotUI != null && draggedSlotUI != this && inventory != null && inventory.slots != null)
        {
            int fromIndex = draggedSlotUI.slotIndex;
            int toIndex = this.slotIndex;

            if (fromIndex >= 0 && fromIndex < inventory.slots.Length && toIndex >= 0 && toIndex < inventory.slots.Length)
            {
                InventorySlotData temp = inventory.slots[toIndex];
                inventory.slots[toIndex] = inventory.slots[fromIndex];
                inventory.slots[fromIndex] = temp;

                if (inventory.uiManager != null)
                {
                    inventory.uiManager.UpdateUI(inventory.slots);
                    inventory.uiManager.ClearSelection();
                }

                PlayerHotbar hotbar = inventory.GetComponent<PlayerHotbar>();
                if (hotbar != null && hotbar.currentHotbarIndex >= 0)
                {
                    int hotbarRealIndex = hotbar.currentHotbarIndex + 30;
                    if (fromIndex == hotbarRealIndex || toIndex == hotbarRealIndex || fromIndex == hotbar.currentHotbarIndex || toIndex == hotbar.currentHotbarIndex)
                    {
                        hotbar.SelectSlot(hotbar.currentHotbarIndex);
                    }
                }
            }
        }
    }

    public void SetupSlot(Sprite icon, int amount)
    {
        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(icon != null);
            itemIcon.sprite = icon;
        }

        if (amountText != null)
        {
            amountText.gameObject.SetActive(amount > 1);
            amountText.text = amount > 1 ? amount.ToString() : "";
        }
    }

    public void ClearSlot()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.gameObject.SetActive(false);
        }

        if (amountText != null)
        {
            amountText.text = "";
            amountText.gameObject.SetActive(false);
        }
    }
}
