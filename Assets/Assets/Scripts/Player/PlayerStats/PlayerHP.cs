using System;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-80)]
public class PlayerHP : MonoBehaviour, IDamageable, IHealth
{
    private float HP;
    private PlayerInformation info;

    public float CurrentHealth => HP;

    // Gốc lấy từ PlayerData, nhân hệ số sao (0 sao = x1 = đúng giá trị PlayerData)
    public float MaxHealth =>
        info.playerData.MaxHealth
        * (StarManager.Instance?.GetStarsMultiplier(StatType.Health) ?? 1f);

    [SerializeField] private Color32 HurtColor;
    public bool IsPlayer => true;
    public event Action<float, float, bool> HealthChanged;
    [field: SerializeField] public SpriteRenderer PlayerSpriteRenderer { get; set; }
    private Coroutine hurtRoutine;
    private WaitForSeconds hurtWait = new WaitForSeconds(0.15f);

    private void Awake()
    {
        // Lấy trực tiếp trên cùng object, không phụ thuộc PlayerController.Instance đã sẵn sàng hay chưa
        info = GetComponent<PlayerInformation>();
        if (PlayerSpriteRenderer == null) PlayerSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Mặc định: đầy máu. Nếu có save, SaveManager.ApplyToPlayer sẽ ghi đè sau
        SetHealth(MaxHealth);
    }

    /// <summary>Đặt máu về một giá trị cụ thể (load save, mặc định) và cập nhật UI.</summary>
    public void SetHealth(float value)
    {
        HP = Mathf.Clamp(value, 0f, MaxHealth);
        HealthChanged?.Invoke(HP, MaxHealth, true);
    }

    public void TakeDamage(float amount, GameObject source, bool blockable = true)
    {
        if (source == this.gameObject) return;
        if (PlayerController.Instance.IsDead || PlayerController.Instance.IsInvulnerable) return;
        // Đang block: đỡ được thì dừng ở đây, không trừ máu. Hết stamina thì TryBlockHit trả false, đòn trúng như thường
        if (blockable && PlayerController.Instance.Machine.Current == PlayerController.Instance.BlockState && PlayerController.Instance.BlockState.TryBlockHit()) return;
        HP -= amount;
        if (HP <= 0f)
        {
            HP = 0f;
            PlayerController.Instance.Die();
        }
        HealthChanged?.Invoke(HP, MaxHealth, true);
        if (hurtRoutine != null) StopCoroutine(hurtRoutine);
        hurtRoutine = StartCoroutine(HurtEffect());
    }

    public void RegainHP(float amount)
    {
        if (PlayerController.Instance.IsDead || PlayerController.Instance.IsInvulnerable) return;
        HP += amount;
        if (HP > MaxHealth) HP = MaxHealth;
        HealthChanged?.Invoke(HP, MaxHealth, true);
    }

    public void UseHealthPotion()
    {
        if (info.CanUseHealthPotion && HP < MaxHealth)
        {
            RegainHP(50);
            info.OnUseHealthPotion();
        }
    }

    private IEnumerator HurtEffect()
    {
        PlayerSpriteRenderer.color = HurtColor;
        yield return hurtWait;
        PlayerSpriteRenderer.color = Color.white;
        hurtRoutine = null;
    }
}