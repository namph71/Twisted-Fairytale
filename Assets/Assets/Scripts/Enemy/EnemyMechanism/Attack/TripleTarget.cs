using System.Collections;
using UnityEngine;

/// <summary>
/// tạo 3 delayed aoe dưới chân người chơi mỗi 1s
/// </summary>
public class TripleTarget : BaseBossAbility
{
    [SerializeField] private float TargetInterval = 1f;
    public override IEnumerator PerformAbility()
    {
        while (true)
        {
            if (enemy.TargetPlayer != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    HitDealer attackSpawned = Object.Instantiate(Prefab, enemy.TargetPlayer.AttackHitboxCenter, Prefab.transform.rotation);
                    attackSpawned.Init(enemy.gameObject, Prefab.damage, enemy.attackHitMask, 1f);
                    yield return new WaitForSeconds(TargetInterval);
                }
                yield return new WaitForSeconds(Cooldown);
            }
            yield return null;
        }
    }
}
