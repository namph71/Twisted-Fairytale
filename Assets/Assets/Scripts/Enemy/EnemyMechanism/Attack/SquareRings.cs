using System.Collections;
using UnityEngine;

public class SquareRings : BaseBossAbility
{
    [Tooltip("Các ring đặt sẵn làm con của boss, xếp từ trong ra ngoài")]
    [SerializeField] private HitDealer[] squareRings;
    [Tooltip("Mỗi ring sáng bao lâu rồi chuyển sang ring kế")]
    [SerializeField] private float ringDuration = 1f;
    [Tooltip("Số lần bật ring ngẫu nhiên sau lượt từ trong ra ngoài")]
    [SerializeField] private int randomCount = 3;

    private WaitForSeconds ringWait;

    protected override void Awake()
    {
        base.Awake(); // gán enemy
        ringWait = new WaitForSeconds(ringDuration);
        foreach (HitDealer ring in squareRings)
        {
            // Setup thay vì Init: ring dùng lại nhiều lần, không được hẹn giờ huỷ
            ring.Setup(enemy.gameObject, ring.damage, enemy.attackHitMask);
            ring.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // Boss bị tắt giữa chừng: không để sót ring nào đang bật
        foreach (HitDealer ring in squareRings)
            if (ring != null) ring.gameObject.SetActive(false);
    }

    public override IEnumerator PerformAbility()
    {
        while (true)
        {
            if (enemy.TargetPlayer != null)
            {
                int current = -1;

                // Lượt 1: lần lượt từ trong ra ngoài
                for (int i = 0; i < squareRings.Length; i++)
                {
                    current = Switch(current, i);
                    yield return ringWait;
                }

                // Lượt 2: ngẫu nhiên, không trùng ring vừa bật
                for (int n = 0; n < randomCount; n++)
                {
                    current = Switch(current, RandomOther(current));
                    yield return ringWait;
                }

                Switch(current, -1); // tắt ring cuối cùng
                yield return new WaitForSeconds(Cooldown);
            }
            yield return null;
        }
    }

    // Tắt ring cũ, bật ring mới trong cùng 1 frame. -1 = không có ring
    private int Switch(int prev, int next)
    {
        if (prev >= 0) squareRings[prev].gameObject.SetActive(false);
        if (next >= 0) squareRings[next].gameObject.SetActive(true);
        return next;
    }

    // Chọn ngẫu nhiên 1 ring khác ring exclude
    private int RandomOther(int exclude)
    {
        if (exclude < 0 || squareRings.Length < 2) return Random.Range(0, squareRings.Length);
        int r = Random.Range(0, squareRings.Length - 1); // chừa ra 1 chỗ cho ring bị loại
        return r >= exclude ? r + 1 : r;                 // nhảy qua ring bị loại
    }
}