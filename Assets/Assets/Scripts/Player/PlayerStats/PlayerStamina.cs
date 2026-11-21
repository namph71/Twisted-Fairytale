using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Stamina được xài cho dash / dodge, block
/// </summary>
[DefaultExecutionOrder(-80)]
public class PlayerStamina : MonoBehaviour
{
    public float Stamina { get; private set; }
    private PlayerInformation info;

    // Gốc lấy từ PlayerData, nhân hệ số sao (0 sao = x1 = đúng giá trị PlayerData)
    public float MaxStamina =>
        info.playerData.MaxStamina
        * (StarManager.Instance?.GetStarsMultiplier(StatType.Stamina) ?? 1f);

    [Tooltip("Tốc độ hồi stamina mỗi cooldown")]
    [SerializeField] private float RegainSpeed;
    [SerializeField] private float RegainCooldown;
    [SerializeField] private float regenDelay = 0.6f; // chờ sau lần dùng gần nhất
    public Action<float, float> StaminaChanged;
    private float lastUseTime = float.NegativeInfinity;

    private void Awake()
    {
        info = GetComponent<PlayerInformation>();
    }

    private void Start()
    {
        // Mặc định: đầy stamina. Nếu có save, SaveManager.ApplyToPlayer sẽ ghi đè sau
        SetStamina(MaxStamina);
        StartCoroutine(RegainLoop());
    }

    /// <summary>Đặt stamina về một giá trị cụ thể (load save, mặc định) và cập nhật UI.</summary>
    public void SetStamina(float value)
    {
        Stamina = Mathf.Clamp(value, 0f, MaxStamina);
        StaminaChanged?.Invoke(Stamina, MaxStamina);
    }

    // Chỉ trả lời "có đủ stamina không"
    public bool IsTired(float amount) => Stamina < amount;

    public void ConsumeStamina(float amount)
    {
        if (IsTired(amount)) return;
        Stamina -= amount;
        lastUseTime = Time.time;
        StaminaChanged?.Invoke(Stamina, MaxStamina);
    }

    private IEnumerator RegainLoop()
    {
        var wait = new WaitForSeconds(RegainCooldown);
        while (true)
        {
            // Vừa dùng xong thì chờ regenDelay mới hồi; đầy rồi thì thôi, không bắn event thừa cho thanh UI
            if (Stamina < MaxStamina && Time.time - lastUseTime >= regenDelay)
                RegainStamina(RegainSpeed);
            yield return wait;
        }
    }

    public void RegainStamina(float amount)
    {
        Stamina += amount;
        if (Stamina >= MaxStamina) Stamina = MaxStamina;
        StaminaChanged?.Invoke(Stamina, MaxStamina);
    }

    public void UseStaminaPotion()
    {
        if (info.CanUseStaminaPotion && Stamina < MaxStamina)
        {
            RegainStamina(50);
            info.OnUseStaminaPotion();
        }
    }

    public bool TryConsume(float amount)
    {
        if (Stamina < amount) return false;
        Stamina -= amount;
        lastUseTime = Time.time;
        StaminaChanged?.Invoke(Stamina, MaxStamina); // trước đây thiếu dòng này nên thanh UI không tụt
        return true;
    }
}