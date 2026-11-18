using System.Collections.Generic;
using UnityEngine;

public class HitDealer : MonoBehaviour
{
    protected GameObject owner;
    [field: SerializeField] public float damage { get; protected set; }
    protected LayerMask hitMask;
    private HashSet<IDamageable> alreadyHit = new();
    private readonly HashSet<IDamageable> ignored = new();
    // Bật lại (SetActive true) = một lượt đánh mới: quên ai đã trúng, ai đã né ở lượt trước
    protected virtual void OnEnable()
    {
        alreadyHit.Clear();
        ignored.Clear();
    }
    public void Setup(GameObject owner, float damage, LayerMask hitMask)
    {
        this.owner = owner; this.damage = damage; this.hitMask = hitMask;
    }

    /// <summary>Đòn có đang gây damage không. DelayedAOEDealer: chỉ sau khi nổ.</summary>
    public virtual bool IsDangerous => true;

    /// <summary>Mục tiêu đã né đòn này (perfect dodge): đòn không trúng nó nữa.</summary>
    public void Ignore(IDamageable target)
    {
        if (target != null) ignored.Add(target);
    }

    public virtual void Init(GameObject owner, float damage, LayerMask hitMask, float lifetime)
    {
        this.owner = owner; this.damage = damage; this.hitMask = hitMask;
        Destroy(gameObject, lifetime);
    }

    protected virtual void OnTriggerEnter2D(Collider2D col) => TryHit(col);

    /// <summary>
    /// Kiểm tra layer và không tự đánh mình. Trả về mục tiêu hợp lệ, hoặc null.
    /// Không kiểm tra "đã trúng chưa", để class con (vd: gây damage theo tick) dùng lại.
    /// </summary>
    protected IDamageable GetTarget(Collider2D col)
    {
        if ((hitMask.value & (1 << col.gameObject.layer)) == 0) return null;
        // owner có thể đã bị huỷ (enemy chết trước khi AOE nổ)
        if (owner != null && col.transform.IsChildOf(owner.transform)) return null;

        var target = col.GetComponentInParent<IDamageable>();
        if (target == null || ignored.Contains(target)) return null;

        // Player đang trong i-frame của cú né và đòn này né được: không trúng, kích hoạt perfect dodge
        if (col.attachedRigidbody != null && col.attachedRigidbody.TryGetComponent(out PlayerController player) &&
            player.ActiveIFrame != null && player.ActiveIFrame.TryDodge(this)) return null;

        return target;
    }

    /// <summary>
    /// Mỗi mục tiêu chỉ trúng 1 lần trong suốt vòng đời, rồi gây damage.
    /// Class con gọi được (vd: AOE quét vùng lúc nổ). Trả về true nếu trúng.
    /// </summary>
    protected bool TryHit(Collider2D col)
    {
        var target = GetTarget(col);
        if (target == null || !alreadyHit.Add(target)) return false;
        OnHit(target);
        return true;
    }
    // Đòn ở layer Dodgeable (AOE) chỉ né được, không đỡ được
    public virtual bool Blockable => gameObject.layer != LayerMask.NameToLayer("Dodgeable");

    protected virtual void OnHit(IDamageable target) => target.TakeDamage(damage, owner, Blockable);
}