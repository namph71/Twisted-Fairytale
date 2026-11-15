using System.Collections;
using UnityEngine;

public class FireHomingProjectile : BaseBossAbility
{
    [SerializeField] private HomingProjectile HomingPrefab;
    public override IEnumerator PerformAbility()
    {
        while (true)
        {
            if (enemy.TargetPlayer != null)
            {
                HomingProjectile attackSpawned = Object.Instantiate(HomingPrefab, enemy.transform.position, HomingPrefab.transform.rotation);
                attackSpawned.Init(enemy.gameObject, enemy.TargetPlayer.GetComponent<IDamageable>());
                yield return new WaitForSeconds(Cooldown);
            }
            yield return null;
        }
    }
}
