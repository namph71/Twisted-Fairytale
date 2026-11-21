using UnityEngine;

[CreateAssetMenu(menuName = "Player/PlayerData")]
public class PlayerData : ScriptableObject
{
    public int MaxHealth;
    public int MaxStamina;
    public int MaxHealthPotionAmount;
    public int MaxStaminaPotionAmount;
}
