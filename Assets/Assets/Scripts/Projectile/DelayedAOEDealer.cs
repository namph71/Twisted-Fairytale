using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DelayedAOEDealer : HitDealer
{
    [Header("Delayed AOE")]
    [Tooltip("Sau bao nhiêu giây kể từ lúc spawn thì nổ")]
    [SerializeField] private float delay = 1f;
    [Tooltip("Sau khi nổ, ai bước vào vùng trong khoảng này vẫn trúng. 0 = chỉ nổ 1 lần rồi biến mất")]
    [SerializeField] private float activeDuration = 0f;
    // Vùng nổ = tất cả collider trên object này và object con (vd: từng lưỡi của FeatherFang)
    private Collider2D[] areas;
    private float timer;
    private bool detonated;
    private readonly List<Collider2D> overlaps = new();
    public override bool IsDangerous => detonated; // đang cảnh báo thì chưa tính là đòn nguy hiểm
    private void Awake()
    {
        areas = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D a in areas) a.isTrigger = true;
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        timer = 0f;
        detonated = false;
    }
    public override void Init(GameObject owner, float damage, LayerMask hitMask, float lifetime)
    {
        // Không để object bị huỷ trước khi kịp nổ xong
        base.Init(owner, damage, hitMask, Mathf.Max(lifetime, delay + activeDuration));
    }

    private void Update()
    {
        if (detonated) return;
        timer += Time.deltaTime;
        if (timer >= delay) Detonate();
    }

    private void Detonate()
    {
        detonated = true;
        // OnTriggerEnter2D chỉ chạy lúc BƯỚC VÀO, nên ai đã đứng sẵn trong vùng từ lúc cảnh báo
        // sẽ không có sự kiện nào lúc nổ. Phải chủ động quét vùng.
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(hitMask);
        foreach (Collider2D a in areas)
        {
            overlaps.Clear();
            a.Overlap(filter, overlaps);
            // Đứng ở chỗ 2 lưỡi chồng nhau vẫn chỉ trúng 1 lần: TryHit đã nhớ ai trúng rồi
            foreach (var col in overlaps) TryHit(col);
        }
        Destroy(gameObject, activeDuration); // 0 = huỷ cuối frame này
    }
    protected override void OnTriggerEnter2D(Collider2D col)
    {
        if (!detonated) return;      // đang cảnh báo: bước vào chưa bị gì
        base.OnTriggerEnter2D(col);  // đã nổ và còn activeDuration: ai bước vào thì trúng
    }
}