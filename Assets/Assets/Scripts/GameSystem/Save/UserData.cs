using System;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType { Sword, Greatsword, Spear }

[Serializable]
public class WeaponLevel
{
    public int Level;
    public float Experience;
    public WeaponType Type;
}

[Serializable]
public class UserData
{
    public List<Star> StarsCollected = new List<Star>();
    public List<WeaponLevel> WeaponLevels = new List<WeaponLevel>();
    public float CurrentHealth;
    public float CurrentStamina;
    public int CurrentHealthPotion;
    public int CurrentStaminaPotion;
    public bool HasPlayerState; // true khi đã từng lưu trạng thái player
    public int HalfStarsLeft;
    public bool HasStarDrop;
    public Vector2 StarDropPosition;
    public List<Star> DroppedStars = new List<Star>();
    public int DroppedHalfStarsLeft;
}
