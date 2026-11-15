using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn lên BiteRange, không gắn lên Enemy
/// </summary>
public class Bite : MonoBehaviour
{
    [SerializeField] private float BiteDamage;
    [Tooltip("Cắn xong chờ bao lâu mới cắn tiếp nếu player vẫn đứng trong tầm")]
    [SerializeField] private float BiteCooldown = 1f;
    [Tooltip("Báo trước bao lâu rồi mới cắn, để player kịp giữ F")]
    [SerializeField] private float BiteWindup = 0.4f;
    [Tooltip("Object báo hiệu (dấu !, vệt đỏ...) bật trong lúc báo trước. Để trống cũng được")]
    [SerializeField] private GameObject BiteTelegraph;

    private BaseEnemy enemy;
    private float nextBiteTime;
    private float lastInRangeTime = -1f;
    private Coroutine biteRoutine;

    private void Awake()
    {
        enemy = GetComponentInParent<BaseEnemy>();
        if (BiteTelegraph != null) BiteTelegraph.SetActive(false);
    }

    private void OnDisable()
    {
        // Enemy chết / bị tắt giữa lúc báo trước: huỷ lượt cắn, tắt báo hiệu
        biteRoutine = null;
        if (BiteTelegraph != null) BiteTelegraph.SetActive(false);
    }

    // Enter: player vừa bước vào tầm thì bắt đầu báo trước
    private void OnTriggerEnter2D(Collider2D collision) => TryBite(collision);

    // Stay: player đứng yên trong tầm thì cứ hết cooldown là bắt đầu lượt cắn kế
    private void OnTriggerStay2D(Collider2D collision) => TryBite(collision);

    private void TryBite(Collider2D collision)
    {
        // Collider chạm vào có thể là con của Player (hitbox, body...): lấy Player qua Rigidbody2D
        if (collision.attachedRigidbody == null ||
            !collision.attachedRigidbody.TryGetComponent(out PlayerController player)) return;
        if (player != enemy.TargetPlayer || player.IsDead) return;

        lastInRangeTime = Time.time; // để lúc cắn biết player còn trong tầm không

        if (biteRoutine != null || Time.time < nextBiteTime || enemy.IsDead) return;
        biteRoutine = StartCoroutine(BiteRoutine(player));
    }

    private IEnumerator BiteRoutine(PlayerController player)
    {
        enemy.StopToAttack();
        if (BiteTelegraph != null) BiteTelegraph.SetActive(true);

        yield return new WaitForSeconds(BiteWindup);

        if (BiteTelegraph != null) BiteTelegraph.SetActive(false);
        nextBiteTime = Time.time + BiteCooldown; // player có nhiều collider: chặn cắn nhiều lần trong 1 lượt
        biteRoutine = null;

        // Player đã chạy ra khỏi tầm trong lúc báo trước thì cắn trượt
        bool stillInRange = Time.time - lastInRangeTime <= 0.1f;
        if (!stillInRange || enemy.IsDead || player == null || player.IsDead) yield break;
        if (player.TryGetComponent(out IDamageable target))
            target.TakeDamage(BiteDamage, enemy.gameObject);
    }
}