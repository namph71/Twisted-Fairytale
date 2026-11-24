using UnityEngine;

public class PlayerRunState : PlayerGroundedState
{
    private float stepTimer;

    public PlayerRunState(PlayerController p, PlayerStateMachine m) : base(p, m) { }

    public override void Enter()
    {
        base.Enter();
        player.UpdateFacingFromInput();
        player.PlayAnimation("Run");
        player.currentStateName = "Run";
        stepTimer = player.firstStepDelay; // bước đầu tiên phát sau một chút, khớp lúc chân chạm đất
    }

    protected override void OnStateUpdate()
    {
        player.UpdateFacingFromInput();
        player.PlayAnimation("Run");

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            SoundManager.Instance?.PlaySFX("footstep");
            stepTimer += player.footstepInterval;
        }
    }

    public override void PhysicsUpdate() => player.MoveInFacingOrInput(player.runSpeed);
}