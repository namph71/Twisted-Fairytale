using System;
using UnityEngine;

/// <summary>
/// trúng idamageable TỪ PHÍA PLAYER thì sẽ bị destroy
/// </summary>
public class VulnerableObject : PuzzleObject, IDamageable
{
    public override void OnTriggerEnter2D(Collider2D collision)
    {
        return;
    }
    public void TakeDamage(float amount, GameObject source)
    {
        if (source.GetComponent<PlayerController>() != null)
        {
            Complete();
            Destroy(this.gameObject);
        }
    }
}
