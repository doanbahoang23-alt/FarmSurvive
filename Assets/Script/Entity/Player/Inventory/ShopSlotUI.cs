using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlotUI : MonoBehaviour
{
    [Header("UI References (Kéo thả từ Prefab)")]
    [Tooltip("Image hiển thị icon của vật phẩm")]
    [SerializeField] private Image itemIcon;

    [Tooltip("Text hiển thị giá tiền của vật phẩm")]
    [SerializeField] private TextMeshProUGUI priceText;

    [Tooltip("Nút bấm tổng của ô slot (tự lấy nếu để trống)")]
    [SerializeField] private Button buyButton;

    private ItemData itemData;
    private ShopManager shopManager;

    public ItemData ItemData => itemData;

    private void Awake()
    {
        if (buyButton == null)
        {
            buyButton = GetComponent<Button>();
        }

        DisableChildRaycasts();
    }

    /// <summary>
    /// Nạp dữ liệu vật phẩm, cập nhật icon và giá tiền từ ShopManager, gán sự kiện click mua
    /// </summary>
    public void Setup(ItemData itemData, ShopManager manager)
    {
        this.itemData = itemData;
        this.shopManager = manager;

        if (this.itemData != null)
        {
            // Cập nhật Icon vật phẩm
            if (itemIcon != null)
            {
                itemIcon.sprite = this.itemData.icon;
                itemIcon.enabled = (this.itemData.icon != null);
            }

            // Cập nhật giá hiển thị (được tính toán qua ShopManager)
            RefreshPrice();
        }

        // Gán sự kiện click mua hàng
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() =>
            {
                if (this.shopManager != null && this.itemData != null)
                {
                    this.shopManager.BuyItem(this.itemData, this);
                }
            });
        }

        DisableChildRaycasts();
    }

    /// <summary>
    /// Cập nhật lại số tiền trên UI (lấy giá mới nhất từ ShopManager)
    /// </summary>
    public void RefreshPrice()
    {
        if (priceText != null && itemData != null && shopManager != null)
        {
            int currentPrice = shopManager.GetCurrentPrice(itemData);
            priceText.text = currentPrice.ToString();
        }
    }

    /// <summary>
    /// Tắt raycastTarget của toàn bộ icon/text con để không chặn sự kiện click của nút bấm cha
    /// </summary>
    private void DisableChildRaycasts()
    {
        Graphic[] allGraphics = GetComponentsInChildren<Graphic>(true);
        foreach (var graphic in allGraphics)
        {
            if (graphic.gameObject != this.gameObject)
            {
                graphic.raycastTarget = false;
            }
        }
    }
}
