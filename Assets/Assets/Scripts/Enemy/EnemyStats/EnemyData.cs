using UnityEngine;

[CreateAssetMenu(menuName="Enemy/EnemyData")]
public class EnemyData : ScriptableObject
{
    public float MaxHP;
    public float MoveSpeed;
    public bool canRespawn = true;
    public float RespawnCooldown;
    public float WeaponExperience;
}
