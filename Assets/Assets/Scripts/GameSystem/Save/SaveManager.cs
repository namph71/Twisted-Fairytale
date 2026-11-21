using System;
using System.Collections.Generic;
using UnityEngine;

// Chạy trước các script khác để dữ liệu có sẵn khi chúng Awake/Start
[DefaultExecutionOrder(-150)]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    // Tăng số này khi đổi cấu trúc UserData mà save cũ không còn dùng được
    private const int CurrentVersion = 1;

    [Tooltip("JSON xuống dòng dễ đọc khi log. Tắt khi build để file gọn hơn")]
    [SerializeField] private bool prettyPrint = true;
    [SerializeField] private bool autoSaveOnQuit = true;

    public UserData Data { get; private set; }

    /// <summary>Gọi sau mỗi lần Load/Reset để UI và các hệ thống cập nhật lại.</summary>
    public event Action<UserData> OnDataLoaded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    // ===== Save / Load =====

    /// <summary>Lấy trạng thái player hiện tại rồi ghi xuống ổ đĩa.</summary>
    public void Save()
    {
        CaptureFromPlayer();
        WriteToPrefs();
    }

    /// <summary>Đọc lại từ PlayerPrefs. Gọi lại bất cứ lúc nào để reload.</summary>
    public void Load()
    {
        Data = ReadFromPrefs() ?? new UserData();
        EnsureAllEntries();
        OnDataLoaded?.Invoke(Data);
    }

    /// <summary>Xoá save và bắt đầu lại từ đầu (New Game).</summary>
    public void ResetData()
    {
        PlayerPrefs.DeleteKey(SaveKeys.UserData);
        PlayerPrefs.DeleteKey(SaveKeys.SaveVersion);
        Data = new UserData();
        EnsureAllEntries();
        WriteToPrefs(); // KHÔNG gọi Save(): Save sẽ chép trạng thái player cũ đè lên dữ liệu vừa reset
        OnDataLoaded?.Invoke(Data);
    }

    public bool HasSave() => PlayerPrefs.HasKey(SaveKeys.UserData);

    // ===== Trạng thái player =====

    // Chép trạng thái player vào Data (chưa ghi xuống ổ đĩa)
    private void CaptureFromPlayer()
    {
        PlayerController p = PlayerController.Instance;
        if (p == null) return;      // scene không có player (menu) hoặc player đã bị huỷ khi thoát game
        if (p.IsDead) return;       // không lưu lúc đang chết, tránh load lên với 0 máu

        Data.CurrentHealth = p.PlayerHP.CurrentHealth;
        Data.CurrentStamina = p.PlayerStamina.Stamina;
        Data.CurrentHealthPotion = p.PlayerInformation.HealthPotionAmount;
        Data.CurrentStaminaPotion = p.PlayerInformation.StaminaPotionAmount;
        Data.HasPlayerState = true;
    }

    /// <summary>
    /// Áp trạng thái đã lưu lên player. Gọi từ PlayerController SAU khi mọi component đã Start.
    /// Vị trí không lưu: luôn đưa về checkpoint.
    /// </summary>
    public void ApplyToPlayer(PlayerController p)
    {
        if (p == null) return;

        p.Movement.Teleport(p.GetRespawnPosition()); // checkpoint, hoặc chỗ đặt trong scene nếu chưa có

        if (!HasSave() || !Data.HasPlayerState)
        {
            Debug.Log("[Save] Chưa có trạng thái player đã lưu, dùng mặc định từ PlayerData.");
            return;
        }

        // Máu đã lưu <= 0 là dữ liệu lỗi: cho đầy máu thay vì vào game là chết
        p.PlayerHP.SetHealth(Data.CurrentHealth > 0f ? Data.CurrentHealth : p.PlayerHP.MaxHealth);
        p.PlayerStamina.SetStamina(Data.CurrentStamina);
        p.PlayerInformation.SetPotions(Data.CurrentHealthPotion, Data.CurrentStaminaPotion);

        Debug.Log($"[Save] Đã áp save: HP={p.PlayerHP.CurrentHealth}/{p.PlayerHP.MaxHealth}, " +
                  $"ST={p.PlayerStamina.Stamina}/{p.PlayerStamina.MaxStamina}, " +
                  $"Bình={p.PlayerInformation.HealthPotionAmount}/{p.PlayerInformation.StaminaPotionAmount}");
    }

    // ===== Đọc / ghi PlayerPrefs =====

    public void WriteToPrefs()
    {
        string json = JsonUtility.ToJson(Data, prettyPrint);
        PlayerPrefs.SetString(SaveKeys.UserData, json);
        PlayerPrefs.SetInt(SaveKeys.SaveVersion, CurrentVersion);
        PlayerPrefs.Save();
        Debug.Log($"[Save] Đã lưu:\n{json}");
    }

    private UserData ReadFromPrefs()
    {
        if (!HasSave()) return null;

        int version = PlayerPrefs.GetInt(SaveKeys.SaveVersion, 0);
        if (version != CurrentVersion)
        {
            Debug.LogWarning($"[Save] Save phiên bản {version}, hiện tại {CurrentVersion}. Bỏ save cũ.");
            return null;
        }

        string json = PlayerPrefs.GetString(SaveKeys.UserData);
        try
        {
            UserData loaded = JsonUtility.FromJson<UserData>(json);
            Debug.Log($"[Save] Đã load:\n{json}");
            return loaded;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Save bị hỏng, tạo dữ liệu mới. Lỗi: {e.Message}");
            return null;
        }
    }

    // Đảm bảo mỗi StatType / WeaponType luôn có đúng 1 phần tử, kể cả save cũ hoặc khi thêm loại mới sau này
    private void EnsureAllEntries()
    {
        if (Data.StarsCollected == null) Data.StarsCollected = new List<Star>();
        if (Data.WeaponLevels == null) Data.WeaponLevels = new List<WeaponLevel>();
        if (Data.DroppedStars == null) Data.DroppedStars = new List<Star>();

        foreach (StatType type in Enum.GetValues(typeof(StatType)))
        {
            if (Data.StarsCollected.Find(s => s.Type == type) == null)
                Data.StarsCollected.Add(new Star { Type = type, AmountCollected = 0 });
        }

        foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
        {
            if (Data.WeaponLevels.Find(w => w.Type == type) == null)
                Data.WeaponLevels.Add(new WeaponLevel { Type = type, Level = 1, Experience = 0f });
        }
    }

    // ===== Tiện ích cho vũ khí =====

    /// <summary>Trả về đúng object trong save: sửa Level/Experience trên nó là sửa luôn dữ liệu sẽ được lưu.</summary>
    public WeaponLevel GetWeapon(WeaponType type) => Data.WeaponLevels.Find(w => w.Type == type);

    public void SetWeaponProgress(WeaponType type, int level, float experience)
    {
        WeaponLevel w = GetWeapon(type);
        w.Level = level;
        w.Experience = experience;
    }


    // ===== Tự lưu =====

    private void OnApplicationQuit()
    {
        if (autoSaveOnQuit && Instance == this) Save();
    }

    // Mobile: app bị đưa xuống nền có thể bị kill mà không gọi OnApplicationQuit
    private void OnApplicationPause(bool paused)
    {
        if (paused && autoSaveOnQuit && Instance == this) Save();
    }
}