using System.Collections;
using UnityEngine;

public class FiveFireBullets : BaseBossAbility
{
    [SerializeField] private HomingProjectile HomingPrefab;
    [SerializeField] private float Interval;
    [SerializeField] private float BulletNumbers;
    public override IEnumerator PerformAbility()
{
    while (true)
    {
        if (enemy.TargetPlayer != null)
        {
            for (int i = 0; i < BulletNumbers; i++)
            {
                HomingProjectile attackSpawned = Object.Instantiate(HomingPrefab, enemy.transform.position, HomingPrefab.transform.rotation);
                attackSpawned.Init(enemy.gameObject, enemy.TargetPlayer.GetComponent<IDamageable>());
                yield return new WaitForSeconds(Interval);
            }
            yield return new WaitForSeconds(Cooldown);
        }
        yield return null;
    }
}
}
