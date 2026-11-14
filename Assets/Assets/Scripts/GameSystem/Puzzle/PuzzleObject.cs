using System;
using UnityEngine;

public class PuzzleObject : MonoBehaviour
{
    [SerializeField] protected string Keyword;
    public static event Action<string> AnyCollected; // 1 sự kiện chung cho mọi PuzzleObject
    public virtual void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            Complete();
            Destroy(this.gameObject);
        }
    }
    public void Complete()
    {
        AnyCollected?.Invoke(Keyword);
    }
}
