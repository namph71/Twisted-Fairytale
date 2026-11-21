using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public enum AttackType { Normal, Heavy, Ultimate, Dash, Block }

public class CooldownBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private PlayerWeapon player;
    [SerializeField] private int UnlockLevel;
    [SerializeField] private AttackType ChosenType;

    private bool IsUnlocked => player.CurrentWeaponEquipped.Level >= UnlockLevel;

    private IEnumerator Start()
    {
        player.PerformAttack += OnCooldownChanged;
        player.CurrentWeaponEquipped.DoLevelUp += OnLevelUp;

        // Đợi 1 frame: level được nạp từ save trong Start của script khác, có thể chạy sau script này
        yield return null;
        RefreshLock();
    }

    private void OnDestroy()
    {
        if (player == null) return;
        player.PerformAttack -= OnCooldownChanged;
        if (player.CurrentWeaponEquipped != null)
            player.CurrentWeaponEquipped.DoLevelUp -= OnLevelUp;
    }

    /// <summary>Chưa mở khoá: che kín (fill = 1). Đã mở: trống (fill = 0). Gọi lại khi level hoặc vũ khí đổi.</summary>
    public void RefreshLock()
    {
        fillImage.DOKill();
        fillImage.fillAmount = IsUnlocked ? 0f : 1f;
    }

    private void OnCooldownChanged(float cooldown, AttackType type)
    {
        if (type != ChosenType || !IsUnlocked || fillImage.fillAmount != 0) return;
        fillImage.DOKill();
        if (cooldown <= 0f) { fillImage.fillAmount = 0f; return; } // không có cooldown: không cần hiện

        fillImage.fillAmount = 1f;                                  // vừa tung chiêu: đầy ngay
        fillImage.DOFillAmount(0f, cooldown).SetEase(Ease.Linear);  // rút về 0 đúng lúc hồi xong
    }

    private void OnLevelUp(int level)
    {
        if (level >= UnlockLevel) RefreshLock();
    }
}