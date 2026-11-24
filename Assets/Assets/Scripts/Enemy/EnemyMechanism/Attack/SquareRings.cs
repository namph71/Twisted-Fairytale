using System.Collections;
using DG.Tweening;
using UnityEngine;

public class SquareRings : BaseBossAbility
{
    [Tooltip("Các ring đặt sẵn làm con của boss, xếp từ trong ra ngoài")]
    [SerializeField] private HitDealer[] squareRings;
    [Tooltip("Khoảng cách giữa 2 ring liên tiếp")]
    [SerializeField] private float ringDuration = 1f;
    [Tooltip("Số lần bật ring ngẫu nhiên sau lượt từ trong ra ngoài")]
    [SerializeField] private int randomCount = 3;

    [Header("Pha cảnh báo (chưa có hitbox)")]
    [SerializeField] private float warnDuration = 0.8f;
    [Tooltip("Tỉ lệ thời gian cảnh báo dùng để hiện lên (0-1)")]
    [SerializeField, Range(0.1f, 1f)] private float appearPortion = 0.5f;
    [Tooltip("Ring bắt đầu thấp hơn vị trí gốc bao nhiêu đơn vị (theo chiều dọc màn hình) rồi trượt lên")]
    [SerializeField] private float appearRise = 0.4f;
    [Tooltip("Độ đậm khi cảnh báo (0-1)")]
    [SerializeField] private float warnAlpha = 0.4f;
    [Tooltip("Độ đậm thấp nhất khi nhấp nháy (0-1)")]
    [SerializeField] private float warnPulseAlpha = 0.18f;
    [SerializeField] private float warnPulseSpeed = 0.2f;

    [Header("Pha nổ (có hitbox)")]
    [SerializeField] private float activeDuration = 0.6f;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.15f;

    [Header("Pha tắt")]
    [SerializeField] private float fadeOutDuration = 0.3f;
    [Tooltip("Ring trượt tiếp lên bao nhiêu đơn vị khi tan biến")]
    [SerializeField] private float fadeOutRise = 0.25f;

    private WaitForSeconds ringWait;
    private SpriteRenderer[] renderers;
    private Collider2D[] hitboxes;
    private Color[] baseColors;
    private Vector3[] basePositions; // localPosition gốc
    private Coroutine[] cycles;

    protected override void Awake()
    {
        base.Awake(); // gán enemy
        ringWait = new WaitForSeconds(ringDuration);

        int n = squareRings.Length;
        renderers = new SpriteRenderer[n];
        hitboxes = new Collider2D[n];
        baseColors = new Color[n];
        basePositions = new Vector3[n];
        cycles = new Coroutine[n];

        for (int i = 0; i < n; i++)
        {
            HitDealer ring = squareRings[i];
            // Setup thay vì Init: ring dùng lại nhiều lần, không được hẹn giờ huỷ
            ring.Setup(enemy.gameObject, ring.damage, enemy.attackHitMask);
            renderers[i] = ring.GetComponent<SpriteRenderer>();
            hitboxes[i] = ring.GetComponent<Collider2D>();
            baseColors[i] = renderers[i].color;
            basePositions[i] = ring.transform.localPosition;
            HideRing(i);
        }
    }

    private void OnDisable()
    {
        // Boss bị tắt giữa chừng: không để sót ring nào đang bật
        if (squareRings == null || renderers == null) return;
        for (int i = 0; i < squareRings.Length; i++)
        {
            if (cycles[i] != null) StopCoroutine(cycles[i]);
            cycles[i] = null;
            if (squareRings[i] != null) HideRing(i);
        }
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
                    current = StartRing(i);
                    yield return ringWait;
                }

                // Lượt 2: ngẫu nhiên, không trùng ring vừa bật
                for (int n = 0; n < randomCount; n++)
                {
                    current = StartRing(RandomOther(current));
                    yield return ringWait;
                }

                // Đợi ring cuối chạy hết các pha rồi mới tính cooldown
                yield return new WaitForSeconds(warnDuration + activeDuration + fadeOutDuration);
                yield return new WaitForSeconds(Cooldown);
            }
            yield return null;
        }
    }

    // Bắt đầu 1 vòng cảnh báo -> nổ -> tắt cho ring index. Ring đang chạy dở thì chạy lại từ đầu
    private int StartRing(int index)
    {
        if (cycles[index] != null) StopCoroutine(cycles[index]);
        cycles[index] = StartCoroutine(RingCycle(index));
        return index;
    }

    private IEnumerator RingCycle(int i)
    {
        SpriteRenderer sr = renderers[i];
        Transform t = squareRings[i].transform;
        Color baseColor = baseColors[i];
        Vector3 basePos = basePositions[i];

        // ===== Cảnh báo: trượt từ dưới lên + hiện dần, rồi nhấp nháy nhẹ. Chưa có hitbox =====
        KillTweens(i);
        hitboxes[i].enabled = false;
        sr.color = WithAlpha(baseColor, 0f);
        t.localPosition = basePos - ScreenUp(t, appearRise);
        squareRings[i].gameObject.SetActive(true);

        float appear = warnDuration * appearPortion;
        sr.DOFade(warnAlpha, appear).SetEase(Ease.OutQuad).SetLink(sr.gameObject);
        t.DOLocalMove(basePos, appear).SetEase(Ease.OutCubic).SetLink(t.gameObject);
        sr.DOFade(warnPulseAlpha, warnPulseSpeed).SetDelay(appear)
            .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(sr.gameObject);
        yield return new WaitForSeconds(warnDuration);

        // ===== Nổ: loé sáng rồi chuyển mượt về màu gốc đặc, bật hitbox =====
        KillTweens(i);
        t.localPosition = basePos; // hitbox luôn đúng vị trí gốc khi bật
        sr.color = flashColor;
        sr.DOColor(WithAlpha(baseColor, 1f), flashDuration).SetEase(Ease.OutQuad).SetLink(sr.gameObject);
        hitboxes[i].enabled = true;
        yield return new WaitForSeconds(activeDuration);

        // ===== Tắt: tắt hitbox trước, rồi trượt tiếp lên và mờ dần =====
        hitboxes[i].enabled = false;
        sr.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad).SetLink(sr.gameObject);
        t.DOLocalMove(basePos + ScreenUp(t, fadeOutRise), fadeOutDuration).SetEase(Ease.OutQuad).SetLink(t.gameObject);
        yield return new WaitForSeconds(fadeOutDuration);

        HideRing(i);
        cycles[i] = null;
    }

    // Hướng "lên trên màn hình" quy về hệ toạ độ của cha (cha đang xoay 45 độ nên không dùng thẳng Vector3.up được)
    private static Vector3 ScreenUp(Transform t, float distance)
    {
        Vector3 worldUp = Vector3.up * distance;
        return t.parent != null ? t.parent.InverseTransformVector(worldUp) : worldUp;
    }

    private void KillTweens(int i)
    {
        renderers[i].DOKill();
        squareRings[i].transform.DOKill();
    }

    private void HideRing(int i)
    {
        KillTweens(i);
        renderers[i].color = baseColors[i];
        squareRings[i].transform.localPosition = basePositions[i];
        hitboxes[i].enabled = false;
        squareRings[i].gameObject.SetActive(false);
    }

    private static Color WithAlpha(Color c, float a) { c.a = a; return c; }

    // Chọn ngẫu nhiên 1 ring khác ring exclude
    private int RandomOther(int exclude)
    {
        if (exclude < 0 || squareRings.Length < 2) return Random.Range(0, squareRings.Length);
        int r = Random.Range(0, squareRings.Length - 1); // chừa ra 1 chỗ cho ring bị loại
        return r >= exclude ? r + 1 : r;                 // nhảy qua ring bị loại
    }
}