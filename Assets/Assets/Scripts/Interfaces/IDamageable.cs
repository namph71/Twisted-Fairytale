using UnityEngine;

/// <summary>Thứ gì nhận được đòn đánh (enemy, player, thùng gỗ...) thì implement interface này.</summary>
public interface IDamageable
{
    // blockable = false: đòn không đỡ được (AOE), chỉ né được
    void TakeDamage(float amount, GameObject source, bool blockable = true);
}