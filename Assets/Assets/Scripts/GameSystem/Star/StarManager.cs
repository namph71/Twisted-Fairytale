using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mọi logic về sao: nhận sao, đổi sao, rơi/nhặt sao khi chết.
/// Dữ liệu nằm trong SaveManager.Data, StarManager chỉ đọc/sửa rồi nhờ SaveManager ghi xuống ổ đĩa.
/// </summary>
// Chạy ngay sau SaveManager (-150), trước PlayerHP/PlayerStamina (-80) vì MaxHealth/MaxStamina cần hệ số sao
[DefaultExecutionOrder(-140)]
public class StarManager : MonoBehaviour
{
    public static StarManager Instance { get; private set; }

    [Tooltip("Tổng số sao tối đa (cộng cả 3 chỉ số). Nơi duy nhất quy định giới hạn này")]
    [SerializeField] private int maxStars = 50;
    public int MaxStars => maxStars;

    /// <summary>Số sao (đã gắn hoặc dư) thay đổi: bảng Stats cập nhật lại.</summary>
    public static event Action StarsChanged;

    private static UserData Data => SaveManager.Instance.Data;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Load/New Game xong thì UI sao cũng phải cập nhật
        if (SaveManager.Instance != null) SaveManager.Instance.OnDataLoaded += HandleDataLoaded;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        if (SaveManager.Instance != null) SaveManager.Instance.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleDataLoaded(UserData _) => StarsChanged?.Invoke();

    // ===== Tiện ích cho Star =====

    public int GetStars(StatType type) => Data.StarsCollected.Find(s => s.Type == type).AmountCollected;

    /// <summary>Thiếu SaveManager (scene test) thì trả 1 = giá trị gốc.</summary>
    public float GetStarsMultiplier(StatType type) =>
        SaveManager.Instance == null ? 1f : Data.StarsCollected.Find(s => s.Type == type).CalculateMultiplier;

    public int TotalStars() => SumStars(Data.StarsCollected);

    private static int SumStars(List<Star> stars)
    {
        int total = 0;
        foreach (Star s in stars) total += s.AmountCollected;
        return total;
    }

    public bool IsStarsFull => TotalStars() >= maxStars;

    /// <summary>Cộng sao, tự chặn ở MaxStars. Trả về số sao thực sự được cộng.</summary>
    public int AddStar(StatType type, int amount = 1)
    {
        int added = Mathf.Clamp(amount, 0, maxStars - TotalStars());
        Data.StarsCollected.Find(s => s.Type == type).AmountCollected += added;
        if (added > 0) StarsChanged?.Invoke();
        return added;
    }

    // ===== Star Exchange (chỉ gọi khi đứng ở checkpoint) =====
    // Rút 1 sao -> +0.5 sao dư. Gắn 1 sao -> tốn 1 sao dư. Lưu dạng nửa sao (int) để không sai số.

    public float StarsLeft => Data.HalfStarsLeft * 0.5f;
    public bool CanRemoveStar(StatType type) => GetStars(type) > 0;
    public bool CanAssignStar => Data.HalfStarsLeft >= 2 && !IsStarsFull;
    // Cho Button OnClick: 0 = Health, 1 = Stamina, 2 = Damage (theo thứ tự trong enum StatType)
    public void RemoveStarButton(int statType) => RemoveStar((StatType)statType);
    public void AssignStarButton(int statType) => AssignStar((StatType)statType);
    public void RemoveStar(StatType type)
    {
        if (!CanRemoveStar(type))
        {
            StarAnnouncement.Instance?.SpawnAny($"Cannot remove star. [{type} has no stars]");
            return;
        }
        Data.StarsCollected.Find(s => s.Type == type).AmountCollected--;
        Data.HalfStarsLeft += 1;
        OnStarsModified();
    }

    public void AssignStar(StatType type)
    {
        if (IsStarsFull)
        {
            StarAnnouncement.Instance?.SpawnAny($"Cannot add star. [All {maxStars} stars are already assigned]");
            return;
        }
        if (Data.HalfStarsLeft < 2)
        {
            StarAnnouncement.Instance?.SpawnAny($"Cannot add star. [Need 1 star left, you have {StarsLeft:0.#}]");
            return;
        }

        Data.HalfStarsLeft -= 2;
        Data.StarsCollected.Find(s => s.Type == type).AmountCollected++;
        OnStarsModified();
    }

    private void OnStarsModified()
    {
        // MaxHealth/MaxStamina vừa đổi: kẹp lại giá trị hiện tại và cập nhật thanh máu, stamina
        PlayerController p = PlayerController.Instance;
        if (p != null)
        {
            p.PlayerHP.SetHealth(p.PlayerHP.CurrentHealth);
            p.PlayerStamina.SetStamina(p.PlayerStamina.Stamina);
        }
        StarsChanged?.Invoke();
    }

    // ===== Rơi sao khi chết =====
    // StarDrop giữ bản sao số sao lúc chết, Data lưu thêm 1 bản để thoát game vẫn còn vệt sáng.
    // Nhặt: lấy lại TOÀN BỘ, sao trả về đúng chỉ số cũ, sao dư trả về sao dư.

    /// <summary>StarDrop gọi lúc chết: lưu bản sao, đặt chỉ số và sao dư về 0.</summary>
    public void DropStars(Vector2 position, List<Star> droppedStars, int droppedHalfStarsLeft)
    {
        Data.DroppedStars = StarDrop.CopyStars(droppedStars);
        Data.DroppedHalfStarsLeft = droppedHalfStarsLeft;
        Data.StarDropPosition = position;
        Data.HasStarDrop = SumStars(droppedStars) > 0 || droppedHalfStarsLeft > 0;

        foreach (Star s in Data.StarsCollected) s.AmountCollected = 0;
        Data.HalfStarsLeft = 0;

        SaveManager.Instance.WriteToPrefs(); // lưu ngay: thoát game lúc này vệt sáng vẫn còn
        StarsChanged?.Invoke();
    }

    /// <summary>StarDrop gọi khi được nhặt: trả lại toàn bộ.</summary>
    public void PickUpStars(List<Star> stars, int halfStarsLeft)
    {
        int overflowHalf = 0;

        foreach (Star dropped in stars)
        {
            // Sau khi chết có thể đã nhặt thêm sao từ boss: phần vượt MaxStars đổ vào sao dư, đủ giá trị
            int room = Mathf.Max(0, maxStars - TotalStars());
            int restore = Mathf.Min(dropped.AmountCollected, room);

            Data.StarsCollected.Find(s => s.Type == dropped.Type).AmountCollected += restore;
            overflowHalf += (dropped.AmountCollected - restore) * 2;
        }

        Data.HalfStarsLeft += halfStarsLeft + overflowHalf;

        Data.DroppedStars.Clear();
        Data.DroppedHalfStarsLeft = 0;
        Data.HasStarDrop = false;

        SaveManager.Instance.WriteToPrefs();
        OnStarsModified(); // MaxHealth/MaxStamina tăng lại: cập nhật thanh máu, stamina và bảng Stats
    }
}