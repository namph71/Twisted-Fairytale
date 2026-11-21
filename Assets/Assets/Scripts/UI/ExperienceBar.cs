using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExperienceBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Image whiteImage;
    [SerializeField] private TextMeshProUGUI OldLevel;
    [SerializeField] private TextMeshProUGUI NextLevel;
    [Header("Tween")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease ease = Ease.OutQuad;
    [Header("Thanh trắng")]
    [Tooltip("Mất máu xong chờ chừng này giây thanh trắng mới tụt. Trúng đòn liên tiếp thì chờ lại từ đầu")]
    [SerializeField] private float whiteDelay = 0.15f;
    [SerializeField] private float whiteDuration = 0.3f;
    [SerializeField] private Ease whiteEase = Ease.InQuad;
    private void OnEnable()
    {
        PlayerWeaponInformation.ChargeExperienceBar += OnExperienceChanged;
    }
    private void OnDisable()
    {
        PlayerWeaponInformation.ChargeExperienceBar -= OnExperienceChanged;
    }

    private void OnExperienceChanged(float current, float next, int level)
    {
        float ratio = next > 0f ? Mathf.Clamp01(current / next) : 0f;

        // Thanh thật: chạy ngay tới giá trị mới
        fillImage.DOKill();
        fillImage.DOFillAmount(ratio, duration).SetEase(ease);

        // Thanh trắng: hồi stamina thì nhảy lên ngay, tiêu stamina thì chờ rồi mới tụt
        if (whiteImage != null)
        {
            whiteImage.DOKill();
            if (ratio >= whiteImage.fillAmount) whiteImage.fillAmount = ratio;
            else whiteImage.DOFillAmount(ratio, whiteDuration).SetDelay(whiteDelay).SetEase(whiteEase);
        }
        OldLevel.text = level.ToString();
        NextLevel.text = (level + 1).ToString();
    }
}
