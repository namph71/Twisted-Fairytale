using System;
using UnityEngine;

public enum StatType { Health, Stamina, Damage}

[Serializable]
public class Star
{
    public int AmountCollected;
    public StatType Type;
    public float CalculateMultiplier => 1 + AmountCollected * 0.1f;
}
