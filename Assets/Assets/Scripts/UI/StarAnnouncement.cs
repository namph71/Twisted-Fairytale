using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Hiện banner khi nhận sao
/// cứ mỗi interval bật ra 1 banner, các banner xếp chồng xuống dưới và tự biến mất độc lập.
/// Gắn lên một object trong Canvas, banner được tạo làm con của object này.
/// </summary>
public class StarAnnouncement : MonoBehaviour
{
    [SerializeField] private GameObject Prefab;
    public static StarAnnouncement Instance;
    [Header("Nội dung")]
    [SerializeField] private string message = "A page of the tale returns to you";
    [Tooltip("Phần trăm mỗi sao, chỉ để hiển thị")]
    [SerializeField] private int percentPerStar = 10;
    [SerializeField] private Color AnnouncementColor;

    [Header("Xếp hàng")]
    [Tooltip("Mỗi bao nhiêu giây bật ra 1 banner")]
    [SerializeField] private float interval = 0.2f;
    [Tooltip("Khoảng cách dọc giữa các banner, pixel UI")]
    [SerializeField] private float spacing = 50f;

    [Header("Hiệu ứng (giây thật, không bị slow-motion hay pause kéo dài)")]
    [Tooltip("Khoảng rơi từ trên xuống, pixel UI")]
    [SerializeField] private float dropDistance = 60f;
    [SerializeField] private float dropDuration = 0.35f;
    [SerializeField] private float holdDuration = 2f;
    [SerializeField] private float fadeOutDuration = 0.35f;
    [Tooltip("Thời gian các banner còn lại trượt lên lấp chỗ trống")]
    [SerializeField] private float reflowDuration = 0.2f;

    private readonly Queue<StatType> queue = new Queue<StatType>();
    private readonly List<RectTransform> active = new List<RectTransform>();
    private Coroutine runner;
    private Vector2 basePos;
    private bool basePosReady;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    private void OnEnable() => GiveStar.GiveStatStar += Enqueue;

    private void OnDisable()
    {
        GiveStar.GiveStatStar -= Enqueue;
        queue.Clear();
        runner = null;
    }

    private void Enqueue(StatType type)
    {
        queue.Enqueue(type);
        if (runner == null) runner = StartCoroutine(SpawnLoop());
    }

    // Không đợi banner trước biến mất: cứ đúng interval là bật banner tiếp theo
    private IEnumerator SpawnLoop()
    {
        while (queue.Count > 0)
        {
            Spawn(queue.Dequeue());
            if (queue.Count > 0) yield return new WaitForSecondsRealtime(interval);
        }
        runner = null;
    }
    private void Spawn(StatType type)
    {
        SpawnAny($"{message} > [{type} +{percentPerStar}%]");
    }
    public void SpawnAny(string Message)
    {
        GameObject go = Instantiate(Prefab, transform);
        RectTransform rt = go.GetComponent<RectTransform>();
        TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();

        // Vị trí đặt trong prefab là vị trí của banner đầu tiên
        if (!basePosReady) { basePos = rt.anchoredPosition; basePosReady = true; }

        tmp.text = Message;

        Vector2 target = SlotPosition(active.Count);
        active.Add(rt);

        rt.anchoredPosition = target + Vector2.up * dropDistance;
        cg.alpha = 0f;

        // Di chuyển (target = rt) và mờ (target = cg) tách riêng, để reflow chỉ huỷ phần di chuyển
        rt.DOAnchorPos(target, dropDuration).SetEase(Ease.OutBack).SetUpdate(true).SetLink(go);

        DOTween.Sequence().SetUpdate(true).SetLink(go)
            .Append(cg.DOFade(1f, dropDuration * 0.6f))
            .AppendInterval(holdDuration)
            .Append(cg.DOFade(0f, fadeOutDuration))
            .OnComplete(() => Remove(rt));
    }

    private void Remove(RectTransform rt)
    {
        active.Remove(rt);
        Destroy(rt.gameObject);
        Reflow();
    }

    // Banner trên cùng biến mất thì các banner bên dưới trượt lên lấp chỗ
    private void Reflow()
    {
        for (int i = 0; i < active.Count; i++)
        {
            RectTransform rt = active[i];
            rt.DOKill(); // huỷ tween di chuyển đang dở (kể cả đang rơi), giữ nguyên tween mờ trên CanvasGroup
            rt.DOAnchorPos(SlotPosition(i), reflowDuration).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(rt.gameObject);
        }
    }

    private Vector2 SlotPosition(int index) => basePos + Vector2.down * spacing * index;
}