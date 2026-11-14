using System.Collections;
using UnityEngine;

public class BaseBossAbility : MonoBehaviour
{
    [SerializeField] protected HitDealer Prefab;
    [SerializeField] protected float Cooldown;
    protected BaseEnemy enemy;
    private void Awake()
    {
        enemy = GetComponent<BaseEnemy>();
    }
    void OnEnable()
    {
        StartCoroutine(PerformAbility());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public virtual IEnumerator PerformAbility()
    {
        while (true)
        {
            if (enemy.TargetPlayer != null)
            {
                HitDealer attackSpawned = Object.Instantiate(Prefab, enemy.TargetPlayer.AttackHitboxCenter, Prefab.transform.rotation);
                attackSpawned.Init(enemy.gameObject, Prefab.damage, enemy.attackHitMask, 1f);
                yield return new WaitForSeconds(Cooldown);
            }
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }
}
