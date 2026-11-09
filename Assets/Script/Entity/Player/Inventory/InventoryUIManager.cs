using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject inventoryPanel;

    [Header("Slots Array")]
    public InventorySlotUI[] allSlots;

    [Header("Coin UI")]
    [SerializeField] private TextMeshProUGUI coinText;

    [Header("Sell Config")]
    [SerializeField] private Button sellButton;
    [SerializeField] private bool sellEntireStack = true;
    [SerializeField] private Color selectedSlotColor = new Color(1f, 0.9f, 0.3f, 0.45f);

    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;

    private int selectedSellSlotIndex = -1;

    public int SelectedSellSlotIndex => selectedSellSlotIndex;

    private void Start()
    {
        FindCoinTextIfNull();
        FindPlayerInventoryIfNull();
        InitSellButton();

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }

    private void FindPlayerInventoryIfNull()
    {
        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>();
        }
    }

    private void InitSellButton()
    {
        if (sellButton == null && inventoryPanel != null)
        {
            Transform sellBtnTransform = inventoryPanel.transform.Find("SellButton");
            if (sellBtnTransform != null)
            {
                sellButton = sellBtnTransform.GetComponent<Button>();
            }
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveListener(OnSellButtonClicked);
            sellButton.onClick.AddListener(OnSellButtonClicked);
            sellButton.interactable = false;
        }
    }

    public void OnSlotClicked(int slotIndex)
    {
        if (playerInventory == null)
        {
            FindPlayerInventoryIfNull();
        }

        if (playerInventory == null || playerInventory.slots == null || slotIndex < 0 || slotIndex >= playerInventory.slots.Length)
        {
            return;
        }

        // Bấm lại vào chính ô đang chọn -> Hủy chọn
        if (selectedSellSlotIndex == slotIndex)
        {
            ClearSelection();
            return;
        }

        InventorySlotData slot = playerInventory.slots[slotIndex];
        if (slot == null || slot.IsEmpty() || slot.itemData == null)
        {
            ClearSelection();
            return;
        }

        selectedSellSlotIndex = slotIndex;
        HighlightSelectedSlot(slotIndex);

        if (sellButton != null)
        {
            sellButton.interactable = slot.itemData.CanSell;
        }
    }

    public void ClearSelection()
    {
        selectedSellSlotIndex = -1;
        HighlightSelectedSlot(-1);

        if (sellButton != null)
        {
            sellButton.interactable = false;
        }
    }

    private void OnSellButtonClicked()
    {
        if (selectedSellSlotIndex < 0 || playerInventory == null) return;

        bool success = playerInventory.SellItemAtSlot(selectedSellSlotIndex, sellEntireStack);
        if (success)
        {
            if (selectedSellSlotIndex >= 0 && selectedSellSlotIndex < playerInventory.slots.Length)
            {
                InventorySlotData slot = playerInventory.slots[selectedSellSlotIndex];
                if (slot == null || slot.IsEmpty() || slot.itemData == null)
                {
                    ClearSelection();
                }
                else
                {
                    if (sellButton != null)
                    {
                        sellButton.interactable = slot.itemData.CanSell;
                    }
                }
            }
            else
            {
                ClearSelection();
            }
        }
    }

    public void OpenInventory()
    {
        if (inventoryPanel != null)
        {
            ClearSelection();
            inventoryPanel.SetActive(true);
            Time.timeScale = 0f;
            if (playerInventory != null)
            {
                UpdateCoinUI(playerInventory.Coins);
            }
        }
    }

    public void CloseInventory()
    {
        if (inventoryPanel != null)
        {
            ClearSelection();
            inventoryPanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    public void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            if (inventoryPanel.activeSelf)
            {
                CloseInventory();
            }
            else
            {
                OpenInventory();
            }
        }
    }

    private void OnDisable()
    {
        // Khôi phục timeScale nếu UI bị disable đột ngột khi đang mở bảng đồ
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            Time.timeScale = 1f;
        }
    }

    public void UpdateCoinUI(int coins)
    {
        FindCoinTextIfNull();

        if (coinText != null)
        {
            coinText.text = coins.ToString();
        }
    }

    private void FindCoinTextIfNull()
    {
        if (coinText == null && inventoryPanel != null)
        {
            Transform coinTextTransform = inventoryPanel.transform.Find("CoinText");
            if (coinTextTransform != null)
            {
                coinText = coinTextTransform.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    public void HighlightHotbarSlot(int selectedIndex)
    {
        if (allSlots == null)
        {
            return;
        }

        for (int i = 30; i < 40; i++)
        {
            if (i >= allSlots.Length) break;

            if (allSlots[i] == null) continue;

            Image slotBg = allSlots[i].GetComponent<Image>();

            if (slotBg != null)
            {
                if (i == selectedIndex)
                {
                    slotBg.color = new Color(1f, 1f, 0.5f, 0.5f);
                }
                else if (i != selectedSellSlotIndex)
                {
                    slotBg.color = new Color(1f, 1f, 1f, 0f);
                }
            }
        }
    }

    private void HighlightSelectedSlot(int index)
    {
        if (allSlots == null) return;

        for (int i = 0; i < allSlots.Length; i++)
        {
            if (allSlots[i] == null) continue;

            // Kiểm tra xem slot hotbar có đang được chọn bởi PlayerHotbar không
            bool isHotbarSelected = false;
            if (i >= 30 && playerInventory != null)
            {
                PlayerHotbar hotbar = playerInventory.GetComponent<PlayerHotbar>();
                if (hotbar != null && hotbar.currentHotbarIndex >= 0 && (hotbar.currentHotbarIndex + 30) == i)
                {
                    isHotbarSelected = true;
                }
            }

            Image slotBg = allSlots[i].GetComponent<Image>();
            if (slotBg != null)
            {
                if (i == index)
                {
                    slotBg.color = selectedSlotColor;
                }
                else if (isHotbarSelected)
                {
                    slotBg.color = new Color(1f, 1f, 0.5f, 0.5f);
                }
                else
                {
                    slotBg.color = new Color(1f, 1f, 1f, 0f);
                }
            }
        }
    }

    public void UpdateUI(InventorySlotData[] slotsData)
    {
        if (allSlots == null)
        {
            return;
        }
        if (slotsData == null)
        {
            return;
        }

        for (int i = 0; i < allSlots.Length; i++)
        {
            if (allSlots[i] == null) continue;

            if (i < slotsData.Length && slotsData[i] != null && slotsData[i].itemData != null)
            {
                allSlots[i].SetupSlot(slotsData[i].itemData.icon, slotsData[i].amount);
            }
            else
            {
                allSlots[i].ClearSlot();
            }
        }
    }
}
