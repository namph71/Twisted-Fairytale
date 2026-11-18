using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateOrbs : BaseBossAbility
{
    [SerializeField] private Orb[] OrbPositions;
    private HashSet<Orb> CurrentOrbs = new();
    protected override void OnEnable()
    {
        base.OnEnable();
        foreach (Orb orb in CurrentOrbs)
        {
            orb.OnDie += OnOrbDestroyed;
        }
    }
    private void OnDisable()
    {
        foreach (Orb orb in CurrentOrbs)
        {
            orb.OnDie -= OnOrbDestroyed;
        }
    }
    public override IEnumerator PerformAbility()
    {
        while (true)
        {
            if (enemy.TargetPlayer != null)
            {
                Orb orb = ActivateRandomOrb();
                if (orb != null) AddOrb(orb);
                yield return new WaitForSeconds(Cooldown);
            }
            yield return null;
        }
    }
    // Bật ngẫu nhiên 1 orb đang tắt. Trả về null nếu tất cả đã bật (đủ tối đa)
    private Orb ActivateRandomOrb()
    {
        int count = 0;
        foreach (Orb orb in OrbPositions)
            if (orb != null && !orb.gameObject.activeSelf) count++;
        if (count == 0) return null;
        int pick = Random.Range(0, count); // chọn thứ tự trong số orb đang tắt
        foreach (Orb orb in OrbPositions)
        {
            if (orb == null || orb.gameObject.activeSelf) continue;
            if (pick-- == 0)
            {
                Activate(orb);
                return orb;
            }
        }
        return null;
    }
    // Bật orb qua BaseEnemy: orb từng bị phá thì IsDead vẫn đang true và máu vẫn 0,
    // SetActive(true) thôi sẽ ra một orb "đã chết" không đánh vỡ được nữa
    private void Activate(Orb orb)
    {
        if (orb.TryGetComponent(out BaseEnemy orbEnemy))
        {
            orbEnemy.TargetPlayer = enemy.TargetPlayer; // orb bắn vào đúng mục tiêu của boss
            if (orbEnemy.IsDead)
            {
                orbEnemy.Respawn(orb.transform.position); // reset IsDead, hồi máu, bật lại tại chỗ
                return;
            }
        }
        orb.gameObject.SetActive(true);
    }
    private void AddOrb(Orb orb)
    {
        if (CurrentOrbs.Add(orb)) orb.OnDie += OnOrbDestroyed; // Add trả false nếu đã có sẵn
    }
    private void OnOrbDestroyed(Orb orb)
    {
        if (CurrentOrbs.Remove(orb)) orb.OnDie -= OnOrbDestroyed;
    }
}