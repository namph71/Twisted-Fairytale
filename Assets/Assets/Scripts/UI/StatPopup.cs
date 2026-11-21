using TMPro;
using UnityEngine;

public class StatPopup : MonoBehaviour
{
    [SerializeField] private PlayerWeaponInformation playerWeaponInformation;
    [SerializeField] private TextMeshProUGUI HealthStar;
    [SerializeField] private TextMeshProUGUI StaminaStar;
    [SerializeField] private TextMeshProUGUI DamageStar;
    [SerializeField] private TextMeshProUGUI WeaponLevel;
    [SerializeField] private TextMeshProUGUI CurrentHealth;
    [SerializeField] private TextMeshProUGUI CurrentStamina;
    [SerializeField] private TextMeshProUGUI DamageMultiplier;
    [SerializeField] private TextMeshProUGUI WeaponDamageMultiplier;
    [SerializeField] private TextMeshProUGUI StarLeft;

    private void OnEnable()
    {
        StarManager.StarsChanged += OnStarChanged;
        OnStarChanged();
        WeaponLevel.text = $"Level: {SaveManager.Instance.GetWeapon(playerWeaponInformation.weaponData.Type).Level}";
        float mult = playerWeaponInformation.DamageMultiplier;
        WeaponDamageMultiplier.text = $"(+{mult - 1f:0.##}) x{mult:0.##}";
    }
    private void OnDisable()
    {
        StarManager.StarsChanged -= OnStarChanged;
    }
    private void OnStarChanged()
    {
        HealthStar.text = $"<sprite index=0> {StarManager.Instance.GetStars(StatType.Health)}";
        StaminaStar.text = $"<sprite index=0> {StarManager.Instance.GetStars(StatType.Stamina)}";
        DamageStar.text = $"<sprite index=0> {StarManager.Instance.GetStars(StatType.Damage)}";
        CurrentHealth.text = $"(+{(StarManager.Instance.GetStarsMultiplier(StatType.Health) - 1) * 100}%) {PlayerController.Instance.PlayerHP.MaxHealth}";
        CurrentStamina.text = $"(+{(StarManager.Instance.GetStarsMultiplier(StatType.Stamina) - 1) * 100}%) {PlayerController.Instance.PlayerStamina.MaxStamina}";
        DamageMultiplier.text = $"(+{(StarManager.Instance.GetStarsMultiplier(StatType.Damage) - 1)}) x{StarManager.Instance.GetStarsMultiplier(StatType.Damage)}";
        float left = StarManager.Instance.StarsLeft;
        StarLeft.gameObject.SetActive(left > 0f);          // hết sao dư thì ẩn hẳn dòng chữ
        StarLeft.text = $"Star left: {left:0.#}";
    }
}
