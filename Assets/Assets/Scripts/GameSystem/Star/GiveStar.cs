using System;
using UnityEngine;

public class GiveStar : MonoBehaviour
{
    [Tooltip("Số sao rơi ra: Guardian 1, Boss 2")]
    [SerializeField] private int Amount = 1;

    public static event Action<StatType> GiveStatStar;

    private static readonly StatType[] StatPool = { StatType.Health, StatType.Stamina, StatType.Damage };
    private static bool isQuitting;

    private void Awake()
    {
        Application.quitting += () => isQuitting = true;
    }

    private void OnDisable()
    {
        // OnDisable còn chạy khi thoát game hoặc đổi scene: không phải boss chết, không cho sao
        if (isQuitting || !gameObject.scene.isLoaded) return;

        for (int i = 0; i < Amount; i++)
        {
            // Giới hạn tổng nằm ở SaveManager.MaxStars
            if (StarManager.Instance != null && StarManager.Instance.IsStarsFull) return;

            StatType type = StatPool[UnityEngine.Random.Range(0, StatPool.Length)];

            // Cộng vào save trước, chỉ báo sự kiện khi sao thực sự được cộng (không bị MaxStars chặn)
            int added = StarManager.Instance != null ? StarManager.Instance.AddStar(type) : 1;
            if (added > 0) GiveStatStar?.Invoke(type);
        }
    }
}