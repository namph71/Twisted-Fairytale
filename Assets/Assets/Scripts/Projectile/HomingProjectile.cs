using UnityEngine;

public class HomingProjectile : MonoBehaviour
{
    private GameObject Source;
    private IDamageable Target;
    [SerializeField] private float Damage;

    [Header("Bay")]
    [SerializeField] private float Speed = 5f;
    [Tooltip("Tốc độ bẻ hướng (độ/giây). Lớn: bám sát khó né. Nhỏ: bay vòng cung, lách người là né được")]
    [SerializeField] private float TurnSpeed = 180f;
    [Tooltip("Phải khớp isoYScale của PlayerMovement để tốc độ trên màn hình giống nhau")]
    [SerializeField] private float isoYScale = 0.5f;
    [Tooltip("Bay quá lâu không trúng thì tự huỷ")]
    [SerializeField] private float MaxLifetime = 5f;

    [Header("Xoay sprite theo hướng bay")]
    [SerializeField] private bool RotateToVelocity = true;
    [Tooltip("Sprite vẽ chĩa lên (trục Y) thì -90, chĩa sang phải (trục X) thì 0")]
    [SerializeField] private float SpriteAngleOffset = -90f;

    private Transform targetTransform;
    private Vector2 dir = Vector2.up; // hướng bay trên màn hình (trước khi ép iso), độ dài 1

    void Start()
    {
        // Target được gán ngay sau Instantiate, nên tới Start đã có
        Component c = Target as Component;
        if (c != null) targetTransform = c.transform;

        // Ban đầu chĩa thẳng về phía mục tiêu
        if (targetTransform != null) dir = ToScreenDir(targetTransform.position - transform.position, dir);
        Destroy(gameObject, MaxLifetime);
    }

    void Update()
    {
        // Mục tiêu còn sống: bẻ dần hướng bay về phía nó, tối đa TurnSpeed độ mỗi giây.
        // Mục tiêu biến mất (chết, bị tắt): bay thẳng tiếp theo hướng cuối cùng
        if (targetTransform != null && targetTransform.gameObject.activeInHierarchy)
        {
            Vector2 want = ToScreenDir(targetTransform.position - transform.position, dir);
            dir = Vector3.RotateTowards(dir, want, TurnSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f);
        }

        Vector2 velocity = IsoDirection.ToIso(dir, isoYScale) * Speed;
        transform.position += (Vector3)(velocity * Time.deltaTime);

        if (RotateToVelocity && velocity.sqrMagnitude > 0.000001f)
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg + SpriteAngleOffset);
    }
    public void Init(GameObject source, IDamageable target)
    {
        Source = source;
        Target = target;
    }
    // Đổi khoảng cách world sang hướng màn hình (bỏ phần ép dọc của iso), để bẻ hướng đều mọi phía
    private Vector2 ToScreenDir(Vector3 worldDelta, Vector2 fallback)
    {
        Vector2 d = new Vector2(worldDelta.x, worldDelta.y / isoYScale);
        return d.sqrMagnitude > 0.000001f ? d.normalized : fallback;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (targetTransform == null) return;
        // Collider chạm vào có thể là con của mục tiêu (hitbox, body...)
        PlayerController p = collision.GetComponentInParent<PlayerController>();
        Debug.Log($"[Homing] trúng {collision.name} | state: {(p != null ? p.currentStateName : "không phải player")} | " +
                  $"Target là {(Target as Component)?.GetType().Name}", collision);
        if (collision.GetComponentInParent<IDamageable>() == Target)
        {
            Target.TakeDamage(Damage, Source);
            Destroy(gameObject);
        }
    }
}