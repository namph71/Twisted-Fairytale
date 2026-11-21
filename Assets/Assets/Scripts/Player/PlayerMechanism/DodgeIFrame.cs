using UnityEngine;

/// <summary>
/// i-frame của cú né: sinh ra lúc bắt đầu dodge (làm con của Player), sống rất ngắn.
/// Trong lúc nó còn sống, đòn thuộc layer dodgeable (AOE...) không trúng player và kích hoạt perfect dodge.
/// Đòn không thuộc layer dodgeable (bite, đòn nhắm thẳng) vẫn trúng như thường.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DodgeIFrame : MonoBehaviour
{
    [Tooltip("Layer của các đòn né được")]
    [SerializeField] private LayerMask dodgeableMask;
    [Tooltip("Thời gian sống = khoảng né chuẩn (giây)")]
    [SerializeField] private float lifetime = 0.2f;
    private IDamageable playerTarget;
    private bool triggered;

    private void Awake()
    {
        if (PlayerController.Instance != null)
        {
            playerTarget = PlayerController.Instance.GetComponent<IDamageable>();
            PlayerController.Instance.ActiveIFrame = this;
        }
        GetComponent<Collider2D>().isTrigger = true;
        Destroy(gameObject, lifetime);
    }

    private void OnDestroy()
    {
        if (PlayerController.Instance != null && PlayerController.Instance.ActiveIFrame == this) PlayerController.Instance.ActiveIFrame = null;
    }

    // Vùng i-frame chạm vào đòn đang gây damage: né sát sạt cũng tính là né chuẩn
    private void OnTriggerEnter2D(Collider2D col) => CheckDealer(col);
    private void OnTriggerStay2D(Collider2D col) => CheckDealer(col);

    private void CheckDealer(Collider2D col)
    {
        HitDealer dealer = col.GetComponentInParent<HitDealer>();
        if (dealer != null && dealer.IsDangerous) TryDodge(dealer);
    }

    /// <summary>
    /// Đòn này né được không. Được: đòn bỏ qua player, và kích hoạt perfect dodge (1 lần mỗi cú né).
    /// HitDealer cũng gọi hàm này ngay lúc sắp trúng player, nên AOE nổ rồi biến mất trong 1 frame vẫn bắt được.
    /// </summary>
    public bool TryDodge(HitDealer dealer)
    {
        if (PlayerController.Instance == null || (dodgeableMask.value & (1 << dealer.gameObject.layer)) == 0) return false;

        dealer.Ignore(playerTarget);
        if (!triggered)
        {
            triggered = true;
            PlayerController.Instance.TriggerPerfectDodge();
        }
        return true;
    }
}