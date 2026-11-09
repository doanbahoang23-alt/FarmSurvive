using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioSliderController : MonoBehaviour
{
    public enum AudioType
    {
        BGM,
        SFX
    }

    [Header("Loại Âm Thanh")]
    [SerializeField] private AudioType audioType = AudioType.BGM;

    [Header("Tham Chiếu UI")]
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI percentText;

    private float lastSFXPreviewTime = 0f;

    private void Awake()
    {
        FindComponentsIfNull();
    }

    private void OnEnable()
    {
        FindComponentsIfNull();
        SyncSliderWithCurrentVolume();

        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }

    private void OnDisable()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }

    private void FindComponentsIfNull()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>() ?? GetComponentInChildren<Slider>(true);
        }

        if (percentText == null)
        {
            // Tìm trong con hoặc cùng cha
            if (transform.parent != null)
            {
                Transform found = transform.parent.Find("PercentText");
                if (found != null) percentText = found.GetComponent<TextMeshProUGUI>();
            }
            if (percentText == null)
            {
                percentText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }
    }

    public void SyncSliderWithCurrentVolume()
    {
        float currentVol = (audioType == AudioType.BGM)
            ? (AudioManager.Instance != null ? AudioManager.Instance.BGMVolume : PlayerPrefs.GetFloat("BGM_Volume", 0.4f))
            : (AudioManager.Instance != null ? AudioManager.Instance.SFXVolume : PlayerPrefs.GetFloat("SFX_Volume", 0.8f));

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(currentVol);
        }

        UpdatePercentText(currentVol);
    }

    public void OnSliderValueChanged(float value)
    {
        value = Mathf.Clamp01(value);

        if (audioType == AudioType.BGM)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBGMVolume(value);
            }
            else
            {
                PlayerPrefs.SetFloat("BGM_Volume", value);
                PlayerPrefs.Save();
            }
        }
        else
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(value);
                if (Time.unscaledTime - lastSFXPreviewTime > 0.15f)
                {
                    lastSFXPreviewTime = Time.unscaledTime;
                    AudioClip previewClip = AudioManager.Instance.pickUpClip ?? AudioManager.Instance.coinPickUpClip ?? AudioManager.Instance.attackClip;
                    if (previewClip != null)
                    {
                        AudioManager.Instance.PlaySFX(previewClip);
                    }
                }
            }
            else
            {
                PlayerPrefs.SetFloat("SFX_Volume", value);
                PlayerPrefs.Save();
            }
        }

        UpdatePercentText(value);
    }

    private void UpdatePercentText(float value)
    {
        if (percentText != null)
        {
            percentText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }

    public void Setup(AudioType type, Slider s, TextMeshProUGUI text)
    {
        this.audioType = type;
        this.slider = s;
        this.percentText = text;
        SyncSliderWithCurrentVolume();
    }
}

