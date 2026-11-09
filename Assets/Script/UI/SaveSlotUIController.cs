using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotUIController : MonoBehaviour
{
    public enum SaveLoadMode
    {
        Save,
        Load
    }

    [Header("Cấu Hình Giao Diện 3 Slot")]
    [Tooltip("Text tiêu đề của bảng (sẽ đổi theo chế độ Lưu / Tải)")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("Mảng 3 nút đại diện cho Slot 1, 2, 3")]
    [SerializeField] private Button[] slotButtons = new Button[3];

    [Tooltip("Mảng 3 Text hiển thị thông tin bên trong 3 nút")]
    [SerializeField] private TextMeshProUGUI[] slotInfoTexts = new TextMeshProUGUI[3];

    [Tooltip("Nút đóng bảng")]
    [SerializeField] private Button closeButton;

    private SaveLoadMode currentMode = SaveLoadMode.Save;

    private void Awake()
    {
        AutoFindReferencesIfNull();
        RegisterSlotEvents();
    }

    private void OnEnable()
    {
        RefreshSlotsUI();
    }

    /// <summary>
    /// Tự động tìm kiếm tham chiếu trong Hierarchy nếu chưa kéo thả vào Inspector
    /// </summary>
    public void AutoFindReferencesIfNull()
    {
        if (titleText == null)
        {
            Transform t = transform.Find("SaveLoadBox/Title") ?? transform.Find("Title");
            if (t != null) titleText = t.GetComponent<TextMeshProUGUI>();
        }

        if (slotButtons == null || slotButtons.Length < 3 || slotButtons[0] == null)
        {
            slotButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                int slotNum = i + 1;
                Transform btnTransform = transform.Find($"SaveLoadBox/SlotGroup/Btn_Slot_{slotNum}")
                                      ?? transform.Find($"SlotGroup/Btn_Slot_{slotNum}")
                                      ?? transform.Find($"Btn_Slot_{slotNum}");
                if (btnTransform != null)
                {
                    slotButtons[i] = btnTransform.GetComponent<Button>();
                }
            }
        }

        if (slotInfoTexts == null || slotInfoTexts.Length < 3 || slotInfoTexts[0] == null)
        {
            slotInfoTexts = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                if (slotButtons[i] != null)
                {
                    slotInfoTexts[i] = slotButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
        }

        if (closeButton == null)
        {
            Transform c = transform.Find("SaveLoadBox/Btn_Close") ?? transform.Find("Btn_Close");
            if (c != null) closeButton = c.GetComponent<Button>();
        }
    }

    private void RegisterSlotEvents()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slotIndex = i + 1; // Slot 1, 2, 3
            if (slotButtons[i] != null)
            {
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() => OnSlotButtonClicked(slotIndex));
            }
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    /// <summary>
    /// Mở bảng chọn Slot theo chế độ Lưu (Save) hoặc Tải (Load)
    /// </summary>
    public void Open(SaveLoadMode mode)
    {
        currentMode = mode;
        AutoFindReferencesIfNull();

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        // 1. Cập nhật tiêu đề bảng theo chế độ
        if (titleText != null)
        {
            titleText.text = (mode == SaveLoadMode.Save) 
                ? "CHỌN VỊ TRÍ ĐỂ LƯU GAME" 
                : "CHỌN BẢN LƯU ĐỂ TIẾP TỤC";
        }

        // 2. Làm mới giao diện dữ liệu của 3 Slot
        RefreshSlotsUI();
    }

    /// <summary>
    /// Đọc thông tin file lưu từ SaveManager để hiển thị lên 3 Slot
    /// </summary>
    public void RefreshSlotsUI()
    {
        for (int i = 0; i < 3; i++)
        {
            int slotIndex = i + 1;
            Button btn = (slotButtons != null && i < slotButtons.Length) ? slotButtons[i] : null;
            TextMeshProUGUI txt = (slotInfoTexts != null && i < slotInfoTexts.Length) ? slotInfoTexts[i] : null;

            if (txt == null && btn != null)
            {
                txt = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            // Đọc DTO GameSaveData từ SaveManager
            GameSaveData data = null;
            if (SaveManager.Instance != null)
            {
                data = SaveManager.Instance.GetSlotMetadata(slotIndex);
            }

            if (data != null && !string.IsNullOrEmpty(data.saveDate))
            {
                // Slot đã có dữ liệu
                if (txt != null)
                {
                    txt.text = $"SLOT {slotIndex}\nCấp {data.currentLevel} - Đợt quái {data.currentWaveCount}\n<size=75%>{data.saveDate}</size>";
                }
                if (btn != null)
                {
                    btn.interactable = true;
                }
            }
            else
            {
                // Slot trống
                if (txt != null)
                {
                    txt.text = $"SLOT {slotIndex}\n[ TRỐNG ]";
                }

                if (btn != null)
                {
                    // Chế độ Save: Vẫn cho phép bấm để lưu mới vào ô trống
                    // Chế độ Load: Khóa nút không cho bấm để tránh nạp file rỗng
                    btn.interactable = (currentMode == SaveLoadMode.Save);
                }
            }
        }
    }

    /// <summary>
    /// Xử lý khi người chơi bấm chọn 1 trong 3 Slot
    /// </summary>
    public void OnSlotButtonClicked(int slotIndex)
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[SaveSlotUIController] Không tìm thấy SaveManager.Instance!");
            return;
        }

        if (currentMode == SaveLoadMode.Save)
        {
            // Thực hiện Lưu Game
            bool success = SaveManager.Instance.SaveGame(slotIndex);
            if (success)
            {
                Debug.Log($"<color=#70E880>[SaveSlotUIController] Đã lưu thành công vào Slot {slotIndex}!</color>");
                Close();

                // Đóng cài đặt và tiếp tục trò chơi
                if (StartMenuManager.Instance != null)
                {
                    StartMenuManager.Instance.CloseSettings();
                    StartMenuManager.Instance.UpdateSaveLoadUI();
                }
            }
        }
        else if (currentMode == SaveLoadMode.Load)
        {
            // Thực hiện Nạp Game
            bool success = SaveManager.Instance.LoadGame(slotIndex);
            if (success)
            {
                Debug.Log($"<color=#70E880>[SaveSlotUIController] Đã nạp thành công từ Slot {slotIndex}!</color>");
                Close();

                // Ẩn menu, hiện HUD và tiếp tục game
                if (StartMenuManager.Instance != null)
                {
                    StartMenuManager.Instance.ResumeGameAfterLoad();
                }
            }
        }
    }

    /// <summary>
    /// Đóng bảng SaveLoadPanel
    /// </summary>
    public void Close()
    {
        gameObject.SetActive(false);
    }
}
