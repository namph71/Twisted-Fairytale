using UnityEngine;

/// <summary>
/// Đỡ đòn (F). Đứng yên, tạo collider chặn đạn trong 1 giây.
/// Mặc định trừ 10 stamina khi ấn F, mỗi đòn đỡ được trừ 5 stamina. Khi hết stamina thì không được đỡ
/// </summary>
public class PlayerBlockState : PlayerState
{
    public PlayerBlockState(PlayerController p, PlayerStateMachine m) : base(p, m) { }

    // Đủ stamina để bắt đầu đỡ VÀ đỡ được ít nhất 1 đòn. Kiểm tra trước khi ChangeState vào đây
    public bool CanStart => !player.PlayerStamina.IsTired(player.blockStartCost + player.blockHitCost);

    public override void Enter()
    {
        base.Enter();
        player.StopMovement();
        player.PlayerStamina.ConsumeStamina(player.blockStartCost);
        if (player.blockShield != null) player.blockShield.SetActive(true); // collider chặn đạn
        player.PlayAnimation("Block", restart: true);
        player.currentStateName = "Block";
        player.Weapon.PerformAttack?.Invoke(player.blockDuration, AttackType.Block);
    }

    // PlayerHP gọi khi bị trúng đòn lúc đang đỡ. true = đỡ được, không mất máu
    public bool TryBlockHit()
    {
        Debug.Log($"[Block] đỡ đòn | IsTired({player.blockHitCost}) = {player.PlayerStamina.IsTired(player.blockHitCost)}");
        if (player.PlayerStamina.IsTired(player.blockHitCost))
        {
            machine.ChangeState(player.GetLocomotionState()); // hết sức: vỡ thế, đòn này trúng
            return false;
        }

        player.PlayerStamina.ConsumeStamina(player.blockHitCost);
        if (player.blockHitVFX != null) // đỡ trúng chỉ sinh thêm VFX quanh player
            Object.Destroy(Object.Instantiate(player.blockHitVFX, player.transform.position, Quaternion.identity), 1f);
        return true;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        // Bấm nhả nhanh vẫn giữ khiên đủ blockMinTime. Sau đó thả F, hoặc giữ quá blockDuration: hạ khiên
        bool released = StateTime >= player.blockMinTime && !player.Input.BlockHeld;
        if (released || StateTime >= player.blockDuration)
            machine.ChangeState(player.GetLocomotionState());
    }

    public override void PhysicsUpdate() => player.StopMovement();

    public override void Exit()
    {
        base.Exit();
        if (player.blockShield != null) player.blockShield.SetActive(false);
        // Đặt ở Exit để mọi đường thoát (thả F, hết giờ, vỡ thế) đều reset thanh Block
        player.Weapon.PerformAttack?.Invoke(0, AttackType.Block);
    }
}