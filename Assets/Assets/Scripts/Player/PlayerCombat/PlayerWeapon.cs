using System;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    public PlayerWeaponData[] WeaponEquipped;
    [field: SerializeField] public PlayerWeaponInformation CurrentWeaponEquipped { get; private set; }
    private float ultimateReadyTime = -1f;

    // Phát sự kiện cho cooldownbar
    public Action<float, AttackType> PerformAttack;
    private void Start()
    {
        DoChangeWeapon(0);
    }
    public void ChangeWeapon(PlayerWeaponData weaponData)
    {
        PlayerController.Instance.normalAttack = weaponData.NormalAttack;
        PlayerController.Instance.heavyAttack = weaponData.HeavyAttack;
        PlayerController.Instance.ultimateAttack = weaponData.Ultimate;
        CurrentWeaponEquipped.weaponData = weaponData;
    }
    public void DoChangeWeapon(int index)
    {
        if (WeaponEquipped.Length <= index) return;
        ChangeWeapon(WeaponEquipped[index]);
    }
    public void PerformUltimate()
    {
        if (Time.time < ultimateReadyTime || PlayerController.Instance.ultimateAttack == null || CurrentWeaponEquipped.Level < 10) return;

        // Chỉ tung ultimate khi đang đứng hoặc chạy: không cắt ngang dash, dodge, lúc chết...
        var cur = PlayerController.Instance.Machine.Current;
        if (cur != PlayerController.Instance.IdleState && cur != PlayerController.Instance.RunState) return;

        ultimateReadyTime = Time.time + CurrentWeaponEquipped.weaponData.UltimateCooldown;
        PlayerController.Instance.AttackState.SetAttack(PlayerController.Instance.ultimateAttack, ultimate: true);
        PlayerController.Instance.Machine.ChangeState(PlayerController.Instance.AttackState);
        PerformAttack?.Invoke(CurrentWeaponEquipped.weaponData.UltimateCooldown, AttackType.Ultimate);
    }
}
