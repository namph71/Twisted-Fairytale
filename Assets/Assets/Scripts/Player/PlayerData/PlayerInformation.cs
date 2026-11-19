using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// thể hiện ra ngoài của playerdata. playerpref lưu trạng thái số bình hiện tại ở đây (playerdata chỉ khai báo max số bình)
/// </summary>
public class PlayerInformation : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private TextMeshProUGUI HealthPotionText;
    [SerializeField] private Image HealthPotionIcon;
    [SerializeField] private TextMeshProUGUI StaminaPotionText;
    [SerializeField] private Image StaminaPotionIcon;
    private int HealthPotionAmount;
    private int StaminaPotionAmount;
    void Start()
    {
        RegainPotions();
    }
    public bool CanUseHealthPotion => HealthPotionAmount > 0;
    public bool CanUseStaminaPotion => StaminaPotionAmount > 0;
    public void OnUseHealthPotion()
    {
        if (!CanUseHealthPotion) return;
        HealthPotionAmount--;
        if (HealthPotionAmount <= 0)
        {
            HealthPotionAmount = 0;
            HealthPotionIcon.color = new Color32(100, 100, 100, 255);
        }
        HealthPotionText.text = HealthPotionAmount.ToString();
    }
    public void OnUseStaminaPotion()
    {
        if (!CanUseStaminaPotion) return;
        StaminaPotionAmount--;
        if (StaminaPotionAmount <= 0)
        {
            StaminaPotionAmount = 0;
            StaminaPotionIcon.color = new Color32(100, 100, 100, 255);
        }
        StaminaPotionText.text = StaminaPotionAmount.ToString();
    }
    public void RegainPotions()
    {
        HealthPotionAmount = playerData.MaxHealthPotionAmount;
        HealthPotionText.text = HealthPotionAmount.ToString();
        HealthPotionIcon.color = Color.white;
        StaminaPotionAmount = playerData.MaxStaminaPotionAmount;
        StaminaPotionText.text = StaminaPotionAmount.ToString();
        StaminaPotionIcon.color = Color.white;
    }
}
