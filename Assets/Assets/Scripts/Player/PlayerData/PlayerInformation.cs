using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thể hiện ra ngoài của PlayerData. Save lưu số bình hiện tại ở đây (PlayerData chỉ khai báo max số bình)
/// </summary>
[DefaultExecutionOrder(-80)]
public class PlayerInformation : MonoBehaviour
{
    public PlayerData playerData;
    [SerializeField] private TextMeshProUGUI HealthPotionText;
    [SerializeField] private Image HealthPotionIcon;
    [SerializeField] private TextMeshProUGUI StaminaPotionText;
    [SerializeField] private Image StaminaPotionIcon;

    private static readonly Color32 EmptyColor = new Color32(100, 100, 100, 255);

    public int HealthPotionAmount { get; private set; }
    public int StaminaPotionAmount { get; private set; }

    public bool CanUseHealthPotion => HealthPotionAmount > 0;
    public bool CanUseStaminaPotion => StaminaPotionAmount > 0;

    private void Start()
    {
        UserData data = SaveManager.Instance?.Data;
        if (data != null && data.HasPlayerState)
            SetPotions(data.CurrentHealthPotion, data.CurrentStaminaPotion); // có save: dùng số đã lưu
        else
            RegainPotions();                                                // chưa có save: đầy bình
    }
    /// <summary>Đặt số bình về giá trị cụ thể (load save, mặc định) và cập nhật UI.</summary>
    public void SetPotions(int health, int stamina)
    {
        HealthPotionAmount = Mathf.Clamp(health, 0, playerData.MaxHealthPotionAmount);
        StaminaPotionAmount = Mathf.Clamp(stamina, 0, playerData.MaxStaminaPotionAmount);
        SyncToSave();
        RefreshUI();
    }

    public void RegainPotions() => SetPotions(playerData.MaxHealthPotionAmount, playerData.MaxStaminaPotionAmount);

    public void OnUseHealthPotion()
    {
        if (!CanUseHealthPotion) return;
        HealthPotionAmount--;
        SyncToSave();
        RefreshUI();
    }

    public void OnUseStaminaPotion()
    {
        if (!CanUseStaminaPotion) return;
        StaminaPotionAmount--;
        SyncToSave();
        RefreshUI();
    }

    // Cập nhật Data trong bộ nhớ ngay lập tức. Ổ đĩa được ghi ở lần WriteToPrefs/Save kế tiếp
    private void SyncToSave()
    {
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.Data.CurrentHealthPotion = HealthPotionAmount;
        SaveManager.Instance.Data.CurrentStaminaPotion = StaminaPotionAmount;
    }

    private void RefreshUI()
    {
        HealthPotionText.text = $"x{HealthPotionAmount.ToString()}";
        HealthPotionIcon.color = CanUseHealthPotion ? Color.white : (Color)EmptyColor;
        StaminaPotionText.text = $"x{StaminaPotionAmount.ToString()}";
        StaminaPotionIcon.color = CanUseStaminaPotion ? Color.white : (Color)EmptyColor;
    }
}