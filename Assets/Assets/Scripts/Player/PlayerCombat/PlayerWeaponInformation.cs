using System;
using UnityEngine;
[DefaultExecutionOrder(50)]
public class PlayerWeaponInformation : MonoBehaviour
{
    public PlayerWeaponData weaponData;
    public static event Action<float, float, int> ChargeExperienceBar;
    [Header("Experience")]
    [SerializeField] private float currentExperience;
    [SerializeField] private int level = 1;
    [SerializeField] private int maxLevel = 50;

    [Header("Đường cong XP")]
    [SerializeField] private float baseExp = 100f;   // XP cần cho level 1 -> 2
    [SerializeField] private float growth = 40f;     // mỗi level cần thêm bao nhiêu
    [SerializeField] private float exponent = 1f;    // 1 = tăng đều, >1 = càng về sau càng dốc

    [Header("Damage")]
    [SerializeField] private float damagePerLevel = 0.01f; // +1% mỗi level
    public event Action<int> DoLevelUp;
    public int Level => level;
    public float CurrentExperience => currentExperience;
    public bool IsMaxLevel => level >= maxLevel;

    // XP cần để lên từ level hiện tại sang level kế tiếp (chỉ phụ thuộc level)
    public float NextExperience =>
        Mathf.Round(baseExp + growth * Mathf.Pow(level - 1, exponent));

    // Hệ số damage hiển thị ở bảng Stats (Weapon ×1.3...)
    public float DamageMultiplier => 1f + (level - 1) * damagePerLevel;

    public void AddExperience(float amount)
    {
        if (IsMaxLevel) return;

        currentExperience += amount;

        // while để lên nhiều level một lúc nếu nhận lượng XP lớn (boss)
        while (!IsMaxLevel && currentExperience >= NextExperience)
        {
            currentExperience -= NextExperience; // giữ XP dư cho level sau
            level++;
            DoLevelUp?.Invoke(level);
        }

        if (IsMaxLevel) currentExperience = 0;
        SaveManager.Instance.SetWeaponProgress(weaponData.Type, level, currentExperience);
    }
    private void Start()
    {
        currentExperience = SaveManager.Instance.Data.WeaponLevels.Find(w => w.Type == weaponData.Type).Experience;
        level = SaveManager.Instance.Data.WeaponLevels.Find(w => w.Type == weaponData.Type).Level;
        ChargeExperienceBar?.Invoke(currentExperience, NextExperience, level);
    }
    private void OnEnable()
    {
        BaseEnemy.GiveWeaponExperience += OnExperienceGained;
    }
    private void OnDisable()
    {
        BaseEnemy.GiveWeaponExperience -= OnExperienceGained;
    }
    private void OnExperienceGained(float amount)
    {
        AddExperience(amount);
        ChargeExperienceBar?.Invoke(currentExperience, NextExperience, level);
    }
}