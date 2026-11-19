using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Stamina được xài cho dash / dodge, block
/// </summary>
public class PlayerStamina : MonoBehaviour
{
    private PlayerController player;
    [SerializeField] private float Stamina;
    [SerializeField] private float MaxStamina;
    [Tooltip("Tốc độ hồi stamina mỗi cooldown")]
    [SerializeField] private float RegainSpeed;
    [SerializeField] private float RegainCooldown;
    [SerializeField] private float regenDelay = 0.6f; // chờ sau lần dùng gần nhất
    public Action<float, float> StaminaChanged;
    private float lastUseTime = float.NegativeInfinity;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }
    void Start()
    {
        StartCoroutine(RegainLoop());
    }

    // Update is called once per frame
    void Update()
    {

    }
    public float GetMaxStamina => MaxStamina;
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
        if (player.PlayerInformation.CanUseStaminaPotion && Stamina < MaxStamina)
        {
            RegainStamina(50);
            player.PlayerInformation.OnUseStaminaPotion();
        }
    }
    public bool TryConsume(float amount)
    {
        if (Stamina < amount) return false;
        Stamina -= amount;
        lastUseTime = Time.time;
        return true;
    }
}
