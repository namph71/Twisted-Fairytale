using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý các Spawner trong scene, nhớ checkpoint hiện tại, và hẹn giờ hồi sinh
/// cho mọi IRespawnable (player lẫn enemy).
/// Hẹn giờ ở đây vì manager luôn active: coroutine chạy trên enemy sẽ bị dừng
/// ngay khi enemy tự SetActive(false) lúc chết.
/// Mỗi scene 1 cái (không DontDestroyOnLoad vì spawner thuộc về scene).
/// </summary>
[DefaultExecutionOrder(-50)]
public class SpawnerManager : MonoBehaviour
{
    public static SpawnerManager Instance { get; private set; }

    [Header("Spawn ngẫu nhiên trong vùng")]
    [Tooltip("Điểm chọn được mà chạm các layer này thì chọn lại. Để trống = dùng ObstacleMask của PathGrid")]
    [SerializeField] private LayerMask blockedMask;
    [Tooltip("Khoảng trống cần có quanh điểm spawn (cỡ bán kính thân enemy)")]
    [SerializeField] private float clearRadius = 0.3f;
    [Tooltip("Số lần thử chọn điểm trống trước khi lùi về điểm spawn cố định của Spawner")]
    [SerializeField] private int maxTries = 20;

    /// <summary>Checkpoint hiện tại của những thứ dùng checkpoint (player).</summary>
    public Spawner Current { get; private set; }
    public IReadOnlyList<Spawner> Spawners => spawners;
    /// <summary>(cũ, mới)</summary>
    public event Action<Spawner, Spawner> CurrentChanged;

    private readonly List<Spawner> spawners = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        spawners.AddRange(FindObjectsByType<Spawner>(FindObjectsSortMode.None));

        Spawner start = spawners.Find(s => s.IsDefault);
        if (start == null)
        {
            start = spawners.Find(s => s.IsCheckpoint);
            if (start != null)
                Debug.LogWarning("[SpawnerManager] Không có checkpoint nào tick isDefault, dùng tạm " + start.name, start);
        }
        if (start != null) SetCurrent(start);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetCurrent(Spawner spawner)
    {
        if (spawner == null || spawner == Current || !spawner.IsCheckpoint) return;

        Spawner old = Current;
        Current = spawner;

        if (old != null) old.onDeactivated?.Invoke();
        Current.onActivated?.Invoke();
        CurrentChanged?.Invoke(old, Current);
    }

    /// <summary>
    /// Vị trí hồi sinh của một đối tượng:
    ///   dùng checkpoint -> đúng điểm spawn của checkpoint hiện tại (player)
    ///   không dùng      -> điểm ngẫu nhiên trong Collider2D của HomeSpawner (enemy)
    ///   không có gì     -> vị trí lúc vào scene
    /// </summary>
    public Vector2 GetSpawnPosition(IRespawnable who)
    {
        if (who.UsesCheckpoint && Current != null) return Current.SpawnPosition;
        if (who.HomeSpawner != null) return RandomPointInSpawner(who.HomeSpawner);
        return who.InitialPosition;
    }

    /// <summary>Giữ lại cho code cũ.</summary>
    public Vector2 GetSpawnPosition(Vector2 fallback) => Current ? Current.SpawnPosition : fallback;

    // Điểm ngẫu nhiên trong vùng collider của spawner, tránh chỗ đang có vật cản.
    // Chọn trong khung bao rồi hỏi chính collider "điểm này có nằm trong không", nên chạy với mọi hình
    // (Box, Circle, Capsule, Polygon). Collider phải đang bật.
    // Spawner không có Collider2D, hoặc thử mãi không ra chỗ trống: dùng điểm spawn cố định như cũ.
    private Vector2 RandomPointInSpawner(Spawner spawner)
    {
        if (!spawner.TryGetComponent(out Collider2D area) || !area.enabled) return spawner.SpawnPosition;

        LayerMask mask = blockedMask.value != 0 ? blockedMask
                       : PathGrid.Instance != null ? PathGrid.Instance.ObstacleMask : default;
        Bounds b = area.bounds;

        for (int i = 0; i < maxTries; i++)
        {
            Vector2 p = new(UnityEngine.Random.Range(b.min.x, b.max.x), UnityEngine.Random.Range(b.min.y, b.max.y));
            if (!area.OverlapPoint(p)) continue; // rơi vào góc khung bao nhưng ngoài hình thật
            if (mask.value == 0 || Physics2D.OverlapCircle(p, clearRadius, mask) == null) return p;
        }
        return spawner.SpawnPosition;
    }

    /// <summary>Hồi sinh sau delay giây. Gọi được cả khi đối tượng đã SetActive(false).</summary>
    public void ScheduleRespawn(IRespawnable who, float delay)
    {
        if (who == null) return;
        StartCoroutine(RespawnAfter(who, delay));
    }

    private IEnumerator RespawnAfter(IRespawnable who, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // Đối tượng bị Destroy trong lúc chờ (vd: đổi scene, boss bị xoá) thì thôi
        if (who is UnityEngine.Object obj && obj == null) yield break;

        who.Respawn(GetSpawnPosition(who));
    }
}