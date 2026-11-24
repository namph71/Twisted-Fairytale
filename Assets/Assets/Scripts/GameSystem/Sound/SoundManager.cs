using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Quản lý toàn bộ âm thanh phát ra trong game.
/// - Nhạc nền: 2 AudioSource luân phiên để chuyển bài mượt (crossfade).
/// - SFX: pool AudioSource dùng lại, phát theo AudioClip hoặc theo id trong thư viện.
/// - Âm lượng Master / Music / SFX lưu vào PlayerPrefs.
/// Gọi: SoundManager.Instance?.PlaySFX("parry");  SoundManager.Instance?.PlayMusic(forestTheme);
/// </summary>
[DefaultExecutionOrder(-130)]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    public enum SoundChannel { SFX, UI }

    [Serializable]
    public class SoundEntry
    {
        [Tooltip("SFX: âm chiến đấu, thế giới. UI: nút bấm, banner, menu")]
        public SoundChannel channel = SoundChannel.SFX;
        [Tooltip("Tên dùng khi gọi PlaySFX(\"...\"), ví dụ: parry, hit, star_get")]
        public string id;
        [Tooltip("Nhiều clip thì mỗi lần phát chọn ngẫu nhiên 1 cái, đỡ nhàm tai")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Lệch cao độ ngẫu nhiên ±, ví dụ 0.05. Giúp tiếng chém lặp lại không bị máy móc")]
        [Range(0f, 0.5f)] public float pitchVariance = 0.05f;
        [Tooltip("Khoảng cách tối thiểu giữa 2 lần phát cùng id (giây). Chặn 10 quái trúng đòn cùng frame thành 1 tiếng nổ chói tai")]
        public float minInterval = 0.05f;
    }

    [Header("Thư viện SFX")]
    [SerializeField] private List<SoundEntry> library = new List<SoundEntry>();

    [Header("Mixer (tuỳ chọn)")]
    [Tooltip("Để trống nếu không dùng Audio Mixer")]
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup ambienceGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;

    [Header("SFX")]
    [Tooltip("Số tiếng SFX phát cùng lúc tối đa. Hết chỗ thì cắt tiếng cũ nhất")]
    [SerializeField] private int sfxPoolSize = 16;

    [Header("Nhạc nền")]
    [SerializeField] private float defaultCrossfade = 1.5f;
    [SerializeField] private AudioClip startMusic;

    [Header("Ambience")]
    [SerializeField] private float defaultAmbienceFade = 2f;
    [SerializeField] private AudioClip startAmbience;

    [Header("Âm lượng mặc định (0-1)")]
    [SerializeField, Range(0f, 1f)] private float defaultMaster = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultMusic = 0.6f;
    [SerializeField, Range(0f, 1f)] private float defaultSfx = 0.8f;

    public float MasterVolume { get; private set; }
    public float MusicVolume { get; private set; }
    public float SfxVolume { get; private set; }
    public AudioClip CurrentMusic => musicSources[activeMusic].clip;

    private readonly Dictionary<string, SoundEntry> lookup = new Dictionary<string, SoundEntry>();
    private readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
    private readonly AudioSource[] musicSources = new AudioSource[2];
    private int activeMusic;
    private readonly AudioSource[] ambienceSources = new AudioSource[2];
    private int activeAmbience;
    private float musicClipVolume = 1f;   // volume riêng của bài đang phát (trước khi nhân âm lượng tổng)
    private AudioSource[] sfxPool;
    private int sfxCursor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildLookup();
        CreateSources();
        LoadVolumes();

        if (startMusic != null) PlayMusic(startMusic, 0f);
        if (startAmbience != null) PlayAmbience(startAmbience, 0f);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ===== SFX =====

    /// <summary>Phát SFX theo id trong thư viện.</summary>
    public void PlaySFX(string id)
    {
        if (!lookup.TryGetValue(id, out SoundEntry entry))
        {
            Debug.LogWarning($"[Sound] Không có SFX id '{id}' trong thư viện.", this);
            return;
        }
        if (entry.clips == null || entry.clips.Length == 0) return;

        // Chặn cùng 1 tiếng phát dồn dập trong 1 khoảnh khắc
        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(id, out float last) && now - last < entry.minInterval) return;
        lastPlayed[id] = now;

        AudioClip clip = entry.clips[UnityEngine.Random.Range(0, entry.clips.Length)];
        float pitch = 1f + UnityEngine.Random.Range(-entry.pitchVariance, entry.pitchVariance);
        PlayOnPool(clip, entry.volume, pitch, entry.channel == SoundChannel.UI ? uiGroup : sfxGroup);
    }

    /// <summary>Phát thẳng 1 AudioClip, không cần khai báo trong thư viện.</summary>
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        PlayOnPool(clip, volume, pitch, sfxGroup);
    }
    /// <summary>Phát SFX theo id sau một khoảng trễ (giây), vd: khớp tiếng chém với lúc hitbox bật.</summary>
    public void PlaySFX(string id, float delay)
    {
        if (delay <= 0f) { PlaySFX(id); return; }
        DOVirtual.DelayedCall(delay, () => PlaySFX(id)).SetLink(gameObject);
    }
    /// <summary>Phát thẳng 1 clip qua nhóm UI (nút bấm, banner).</summary>
    public void PlayUI(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        PlayOnPool(clip, volume, 1f, uiGroup);
    }

    /// <summary>Tắt mọi SFX đang phát (vd: khi về menu, khi chết).</summary>
    public void StopAllSFX()
    {
        foreach (AudioSource s in sfxPool) s.Stop();
    }

    private void PlayOnPool(AudioClip clip, float volume, float pitch, AudioMixerGroup group)
    {
        AudioSource src = GetFreeSource();
        src.outputAudioMixerGroup = group;
        src.clip = clip;
        src.volume = volume * SfxVolume * MasterVolume;
        src.pitch = pitch;
        src.Play();
    }

    // Ưu tiên nguồn đang rảnh. Hết rảnh thì lấy vòng tròn (cắt tiếng cũ nhất)
    private AudioSource GetFreeSource()
    {
        for (int i = 0; i < sfxPool.Length; i++)
        {
            int idx = (sfxCursor + i) % sfxPool.Length;
            if (!sfxPool[idx].isPlaying)
            {
                sfxCursor = (idx + 1) % sfxPool.Length;
                return sfxPool[idx];
            }
        }
        AudioSource oldest = sfxPool[sfxCursor];
        sfxCursor = (sfxCursor + 1) % sfxPool.Length;
        return oldest;
    }

    // ===== Nhạc nền =====

    /// <summary>
    /// Chuyển sang bài mới, mờ dần bài cũ và to dần bài mới cùng lúc.
    /// fade < 0: dùng defaultCrossfade. Gọi lại đúng bài đang phát thì bỏ qua.
    /// </summary>
    public void PlayMusic(AudioClip clip, float fade = -1f, float volume = 1f, bool loop = true)
    {
        if (clip == null) { StopMusic(fade); return; }
        if (fade < 0f) fade = defaultCrossfade;

        AudioSource current = musicSources[activeMusic];
        if (current.clip == clip && current.isPlaying) return;

        int nextIndex = 1 - activeMusic;
        AudioSource next = musicSources[nextIndex];

        musicClipVolume = volume;
        float target = MusicTarget();

        next.DOKill();
        next.clip = clip;
        next.loop = loop;
        next.volume = fade > 0f ? 0f : target;
        next.Play();

        if (fade > 0f)
        {
            next.DOFade(target, fade).SetUpdate(true).SetLink(gameObject);
            current.DOKill();
            current.DOFade(0f, fade).SetUpdate(true).SetLink(gameObject).OnComplete(current.Stop);
        }
        else
        {
            current.DOKill();
            current.Stop();
        }

        activeMusic = nextIndex;
    }

    /// <summary>
    /// Đổi bài nhưng giữ nguyên vị trí đang phát, dùng cho 2 phiên bản cùng 1 bản nhạc
    /// (vd: theme góc nhìn bóp méo / góc nhìn thật có cùng độ dài và nhịp).
    /// </summary>
    public void SwapMusicKeepTime(AudioClip clip, float fade = 0.5f)
    {
        AudioSource current = musicSources[activeMusic];
        float time = current.isPlaying && current.clip != null ? current.time : 0f;
        PlayMusic(clip, fade, musicClipVolume);
        AudioSource next = musicSources[activeMusic];
        if (next.clip != null) next.time = Mathf.Min(time, next.clip.length - 0.01f);
    }

    public void StopMusic(float fade = -1f)
    {
        if (fade < 0f) fade = defaultCrossfade;
        AudioSource current = musicSources[activeMusic];
        current.DOKill();
        if (fade > 0f)
            current.DOFade(0f, fade).SetUpdate(true).SetLink(gameObject).OnComplete(current.Stop);
        else
            current.Stop();
    }

    // ===== Ambience =====

    /// <summary>Đổi âm thanh nền của vùng (gió, linh hồn...), crossfade giống nhạc nền. Luôn lặp lại.</summary>
    public void PlayAmbience(AudioClip clip, float fade = -1f, float volume = 1f)
    {
        if (fade < 0f) fade = defaultAmbienceFade;
        AudioSource current = ambienceSources[activeAmbience];

        if (clip == null) { FadeOutAndStop(current, fade); return; }
        if (current.clip == clip && current.isPlaying) return;

        int nextIndex = 1 - activeAmbience;
        AudioSource next = ambienceSources[nextIndex];
        float target = volume * MasterVolume; // âm lượng nhóm Ambience chỉnh trong mixer

        next.DOKill();
        next.clip = clip;
        next.volume = fade > 0f ? 0f : target;
        next.Play();
        if (fade > 0f) next.DOFade(target, fade).SetUpdate(true).SetLink(gameObject);

        FadeOutAndStop(current, fade);
        activeAmbience = nextIndex;
    }

    public void StopAmbience(float fade = -1f) =>
        FadeOutAndStop(ambienceSources[activeAmbience], fade < 0f ? defaultAmbienceFade : fade);

    private void FadeOutAndStop(AudioSource src, float fade)
    {
        src.DOKill();
        if (fade > 0f && src.isPlaying)
            src.DOFade(0f, fade).SetUpdate(true).SetLink(gameObject).OnComplete(src.Stop);
        else
            src.Stop();
    }

    // ===== Âm lượng (cho thanh trượt trong Settings) =====

    public void SetMasterVolume(float v) { MasterVolume = Mathf.Clamp01(v); ApplyMusicVolume(); SaveVolumes(); }
    public void SetMusicVolume(float v) { MusicVolume = Mathf.Clamp01(v); ApplyMusicVolume(); SaveVolumes(); }
    public void SetSfxVolume(float v) { SfxVolume = Mathf.Clamp01(v); SaveVolumes(); }

    private float MusicTarget() => musicClipVolume * MusicVolume * MasterVolume;

    private void ApplyMusicVolume()
    {
        AudioSource current = musicSources[activeMusic];
        current.DOKill();
        if (current.isPlaying) current.volume = MusicTarget();
    }

    private void LoadVolumes()
    {
        MasterVolume = PlayerPrefs.GetFloat(SaveKeys.MasterVolume, defaultMaster);
        MusicVolume = PlayerPrefs.GetFloat(SaveKeys.MusicVolume, defaultMusic);
        SfxVolume = PlayerPrefs.GetFloat(SaveKeys.SfxVolume, defaultSfx);
    }

    private void SaveVolumes()
    {
        PlayerPrefs.SetFloat(SaveKeys.MasterVolume, MasterVolume);
        PlayerPrefs.SetFloat(SaveKeys.MusicVolume, MusicVolume);
        PlayerPrefs.SetFloat(SaveKeys.SfxVolume, SfxVolume);
        PlayerPrefs.Save();
    }

    // ===== Khởi tạo =====

    private void BuildLookup()
    {
        lookup.Clear();
        foreach (SoundEntry e in library)
        {
            if (string.IsNullOrEmpty(e.id)) continue;
            if (lookup.ContainsKey(e.id))
            {
                Debug.LogWarning($"[Sound] Trùng id '{e.id}' trong thư viện, bỏ qua bản sau.", this);
                continue;
            }
            lookup.Add(e.id, e);
        }
    }

    private void CreateSources()
    {
        for (int i = 0; i < 2; i++)
        {
            AudioSource m = gameObject.AddComponent<AudioSource>();
            m.playOnAwake = false;
            m.loop = true;
            m.outputAudioMixerGroup = musicGroup;
            musicSources[i] = m;
        }

        for (int i = 0; i < 2; i++)
        {
            AudioSource a = gameObject.AddComponent<AudioSource>();
            a.playOnAwake = false;
            a.loop = true;
            a.outputAudioMixerGroup = ambienceGroup;
            ambienceSources[i] = a;
        }

        sfxPool = new AudioSource[Mathf.Max(1, sfxPoolSize)];
        for (int i = 0; i < sfxPool.Length; i++)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.outputAudioMixerGroup = sfxGroup;
            sfxPool[i] = s;
        }
    }

}