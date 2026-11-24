using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Gắn lên mỗi Arm. Hai pha:
/// 1. Telegraph (warnDuration): dải chữ nhật đỏ trong suốt nhấp nháy, thanh nạp chạy dọc dải.
/// 2. Beam (activeDuration): LineRenderer phủ đúng dải, texture chạy dọc beam, hitbox bật.
/// Beam bám theo dải mỗi frame nên Spinner quay thì beam quay theo.
/// </summary>
public class BeamArm : MonoBehaviour
{
    [Header("Tham chiếu")]
    [Tooltip("Dải cảnh báo (object Square)")]
    [SerializeField] private SpriteRenderer telegraph;
    [Tooltip("Hitbox gây sát thương, chỉ bật khi beam đang chạy")]
    [SerializeField] private Collider2D hitbox;
    [SerializeField] private LineRenderer beam;

    [Header("Thời gian (theo Time.timeScale: perfect dodge làm chậm cả beam)")]
    [SerializeField] private float warnDuration = 1.2f;
    [SerializeField] private float activeDuration = 0.6f;

    [Header("Telegraph")]
    [SerializeField] private Color areaColor = new Color(1f, 0.25f, 0.2f, 0.25f);
    [SerializeField] private Color fillColor = new Color(1f, 0.35f, 0.2f, 0.55f);
    [Tooltip("Độ trong suốt thấp nhất khi nhấp nháy")]
    [SerializeField] private float pulseAlpha = 0.12f;
    [SerializeField] private float pulseSpeed = 0.25f;

    [Header("Beam")]
    [Tooltip("Tên texture trong shader: _MainTex (Sprite, Hovl), _BaseMap (URP Particles)")]
    [SerializeField] private string textureProperty = "_BaseMap";
    [Tooltip("Số vòng texture chạy qua mỗi giây")]
    [SerializeField] private float scrollSpeed = 3f;
    [Tooltip("Texture lặp lại bao nhiêu lần trên mỗi đơn vị chiều dài")]
    [SerializeField] private float tilesPerUnit = 0.25f;
    [Tooltip("Đảo chiều: beam bắt đầu từ đầu kia của dải và texture chạy ngược lại")]
    [SerializeField] private bool reverseDirection;
    [Tooltip("Thời gian beam bung ra và thu lại")]
    [SerializeField] private float popDuration = 0.08f;

    public bool IsPlaying => routine != null;

    private SpriteRenderer fill;
    private Material beamMat;
    private Coroutine routine;
    private Tween pulseTween, widthTween;
    private float top, bottom;
    private float baseWidth;

    private bool initialized;

    private void Awake() => Init();

    // Có thể bị gọi trước Awake (vd: object cha gọi Play trong OnEnable), nên Play cũng gọi Init
    private void Init()
    {
        if (initialized) return;
        initialized = true;

        Bounds b = telegraph.sprite.bounds;
        top = reverseDirection ? b.min.y : b.max.y;
        bottom = reverseDirection ? b.max.y : b.min.y;

        CreateFill();
        beamMat = beam.material; // instance riêng cho mỗi arm để cuộn texture độc lập
        beam.useWorldSpace = true;
        beam.positionCount = 2;
        beam.textureMode = LineTextureMode.Stretch;
        baseWidth = beam.widthMultiplier; // Width đặt trên Line Renderer ở Inspector
        beam.sortingLayerID = telegraph.sortingLayerID;
        beam.sortingOrder = telegraph.sortingOrder + 2;

        ResetVisuals();
    }

    private void OnDisable()
    {
        if (!initialized) return;
        if (routine != null) StopCoroutine(routine);
        routine = null;
        ResetVisuals();
    }

    private void OnDestroy()
    {
        if (beamMat != null) Destroy(beamMat);
    }

    /// <summary>Bắt đầu 1 lượt: cảnh báo rồi bắn beam.</summary>
    public void Play()
    {
        Init();
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        ResetVisuals();

        // ===== Pha 1: cảnh báo =====
        telegraph.enabled = true;
        fill.enabled = true;
        pulseTween = telegraph.DOFade(pulseAlpha, pulseSpeed).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);

        for (float t = 0f; t < warnDuration; t += Time.deltaTime)
        {
            SetFill(t / warnDuration);
            yield return null;
        }

        pulseTween.Kill();
        telegraph.enabled = false;
        fill.enabled = false;

        // ===== Pha 2: beam =====
        UpdateBeamPositions();
        SetBeamWidth(0f);
        SetBeamEnabled(true);
        widthTween = DOTween.To(() => beam.widthMultiplier, SetBeamWidth, baseWidth, popDuration)
            .SetEase(Ease.OutBack).SetLink(gameObject);

        if (hitbox != null) hitbox.enabled = true;

        float offset = 0f;
        for (float t = 0f; t < activeDuration; t += Time.deltaTime)
        {
            UpdateBeamPositions();
            offset -= scrollSpeed * Time.deltaTime; // offset giảm = texture trôi từ điểm đầu về điểm cuối
            beamMat.SetTextureOffset(textureProperty, new Vector2(offset, 0f));
            yield return null;
        }

        if (hitbox != null) hitbox.enabled = false;

        widthTween = DOTween.To(() => beam.widthMultiplier, SetBeamWidth, 0f, popDuration)
            .SetEase(Ease.InCubic).SetLink(gameObject);
        for (float t = 0f; t < popDuration; t += Time.deltaTime)
        {
            UpdateBeamPositions();
            yield return null;
        }

        SetBeamEnabled(false);
        routine = null;
    }

    // Beam phủ đúng 2 đầu dải cảnh báo, tính lại mỗi frame để bám theo Spinner đang quay
    private void UpdateBeamPositions()
    {
        Vector3 a = telegraph.transform.TransformPoint(0f, top, 0f);
        Vector3 b = telegraph.transform.TransformPoint(0f, bottom, 0f);
        beam.SetPosition(0, a);
        beam.SetPosition(1, b);
        beamMat.SetTextureScale(textureProperty, new Vector2(Vector3.Distance(a, b) * tilesPerUnit, 1f));
    }

    private void SetBeamWidth(float w) => beam.widthMultiplier = w;

    private void SetBeamEnabled(bool on) => beam.enabled = on;

    // Thanh nạp: con của dải cảnh báo, dài dần từ đầu "top" về đầu "bottom"
    private void CreateFill()
    {
        var go = new GameObject("Fill");
        go.transform.SetParent(telegraph.transform, false);
        fill = go.AddComponent<SpriteRenderer>();
        fill.sprite = telegraph.sprite;
        fill.sharedMaterial = telegraph.sharedMaterial;
        fill.color = fillColor;
        fill.sortingLayerID = telegraph.sortingLayerID;
        fill.sortingOrder = telegraph.sortingOrder + 1;
    }

    private void SetFill(float t)
    {
        t = Mathf.Clamp01(t);
        fill.transform.localScale = new Vector3(1f, t, 1f);
        // Giữ mép "top" của thanh nạp trùng mép "top" của dải
        fill.transform.localPosition = new Vector3(0f, top * (1f - t), 0f);
    }

    private void ResetVisuals()
    {
        pulseTween?.Kill();
        widthTween?.Kill();
        telegraph.color = areaColor;
        telegraph.enabled = false;
        if (fill != null) { fill.enabled = false; SetFill(0f); }
        if (beam != null) { SetBeamEnabled(false); SetBeamWidth(0f); }
        if (hitbox != null) hitbox.enabled = false;
    }
}