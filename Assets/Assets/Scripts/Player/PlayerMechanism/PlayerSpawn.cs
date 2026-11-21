using UnityEngine;

/// <summary>
/// Gắn lên Player. Vào scene -> đặt player ở spawn mặc định.
/// Chết xong (hết animation) -> hồi sinh ở spawn hiện tại.
/// </summary>
public class PlayerSpawn : MonoBehaviour
{
    [Tooltip("Đặt player vào spawn mặc định khi bắt đầu scene")]
    [SerializeField] private bool spawnAtStart = true;

    private void OnEnable() => PlayerController.Instance.DeathSequenceFinished += Respawn;
    private void OnDisable() => PlayerController.Instance.DeathSequenceFinished -= Respawn;

    private void Start()
    {
        if (spawnAtStart && SpawnerManager.Instance != null)
            Teleport(SpawnerManager.Instance.GetSpawnPosition(PlayerController.Instance.Rb.position));
    }

    public void Respawn()
    {
        Vector2 pos = SpawnerManager.Instance != null
            ? SpawnerManager.Instance.GetSpawnPosition(PlayerController.Instance.Rb.position)
            : PlayerController.Instance.Rb.position;

        PlayerController.Instance.Revive(pos);

        // TODO: đổi theo tên hàm trong class của bạn
        // player.HP.ResetHP();
        // player.Stamina.ResetStamina();
    }

    private void Teleport(Vector2 pos)
    {
        PlayerController.Instance.Rb.position = pos;
        transform.position = pos;
        PlayerController.Instance.StopMovement();
    }
}