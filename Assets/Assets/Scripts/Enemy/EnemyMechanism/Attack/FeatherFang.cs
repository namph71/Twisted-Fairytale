using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class FeatherFang : BaseBossAbility
{
    public override IEnumerator PerformAbility()
    {
        while (true)
        {
            if (enemy.TargetPlayer != null)
            {
                Vector2 d = enemy.TargetPlayer.Rb.position - (Vector2)enemy.transform.position;
                float angle = Mathf.Atan2(d.y / 0.5f, d.x) * Mathf.Rad2Deg + 90f;
                HitDealer attackSpawned = Object.Instantiate(Prefab, enemy.transform.position, Quaternion.Euler(60f, 0f, angle));
                attackSpawned.Init(enemy.gameObject, Prefab.damage, enemy.attackHitMask, 1f);
                yield return new WaitForSeconds(Cooldown);
            }
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }
}
