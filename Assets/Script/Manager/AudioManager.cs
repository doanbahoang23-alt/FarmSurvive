using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<AudioManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    instance = go.AddComponent<AudioManager>();
                }
            }
            return instance;
        }
        private set => instance = value;
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM Config")]
    [SerializeField] private AudioClip defaultBGM;
    [SerializeField] private bool playBGMOnStart = true;
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.4f;

    [Header("SFX Config")]
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.8f;

    [Header("Common Sound Clips")]
    public AudioClip tillSoilClip;
    public AudioClip plantSuccessClip;
    public AudioClip plantFailClip;
    public AudioClip chopWoodClip;
    public AudioClip mineRockClip;
    public AudioClip attackClip;
    public AudioClip pickUpClip;
    public AudioClip coinPickUpClip;
    public AudioClip playerDeathClip;
    public AudioClip enemyDeathClip;
    public AudioClip arrowShootClip;

    [Header("Shop Sound Clips")]
    public AudioClip buyItemClip;
    public AudioClip sellItemClip;

    [Header("Footstep Sound Clips")]
    public AudioClip[] footstepClips;

    private int lastFootstepIndex = -1;

    private void Awake()
    {
        if (instance == null || instance == this)
        {
            instance = this;
            InitAudioSources();
        }
        else
        {
            if (gameObject.name == "AudioManager")
            {
                Destroy(gameObject);
            }
            else
            {
                // Nếu gắn chung trên GameManager, cập nhật tham chiếu instance cho scene mới
                instance = this;
                InitAudioSources();
            }
        }
    }

    private void Start()
    {
        if (playBGMOnStart && defaultBGM != null)
        {
            PlayBGM(defaultBGM);
        }
    }

    private void InitAudioSources()
    {
        if (bgmSource == null)
        {
            GameObject bgmObj = new GameObject("BGM_Source");
            bgmObj.transform.SetParent(transform);
            bgmSource = bgmObj.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f; // 2D Sound
        }

        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFX_Source");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D Sound
        }

        bgmVolume = PlayerPrefs.GetFloat("BGM_Volume", bgmVolume);
        sfxVolume = PlayerPrefs.GetFloat("SFX_Volume", sfxVolume);

        bgmSource.volume = bgmVolume;
        sfxSource.volume = sfxVolume;
    }

    #region BGM Methods (Nhạc nền)
    public float BGMVolume => bgmVolume;

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (clip == null || bgmSource == null) return;

        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void PauseBGM()
    {
        if (bgmSource != null) bgmSource.Pause();
    }

    public void ResumeBGM()
    {
        if (bgmSource != null) bgmSource.UnPause();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
        PlayerPrefs.SetFloat("BGM_Volume", bgmVolume);
        PlayerPrefs.Save();
    }
    #endregion

    #region SFX Methods (Hiệu ứng âm thanh)
    public float SFXVolume => sfxVolume;
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * Mathf.Clamp01(volumeScale));
    }

    public void PlayFootstep(float volumeScale = 1f)
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        int index = 0;
        if (footstepClips.Length > 1)
        {
            do
            {
                index = Random.Range(0, footstepClips.Length);
            } while (index == lastFootstepIndex);
        }

        lastFootstepIndex = index;
        AudioClip clip = footstepClips[index];
        if (clip != null)
        {
            PlaySFX(clip, volumeScale);
        }
    }

    public void PlayArrowShoot(float volumeScale = 1f)
    {
        if (arrowShootClip != null)
        {
            PlaySFX(arrowShootClip, volumeScale);
        }
    }

    public AudioClip GetRandomFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return null;
        int index = Random.Range(0, footstepClips.Length);
        return footstepClips[index];
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        PlayerPrefs.SetFloat("SFX_Volume", sfxVolume);
        PlayerPrefs.Save();
    }
    #endregion
}

