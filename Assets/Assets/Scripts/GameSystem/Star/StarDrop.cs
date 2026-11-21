using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vệt sáng duy nhất của game, do PlayerController sinh ra khi chết.
/// Giữ BẢN SAO số sao lúc chết. Nhặt: trả lại một nửa vào sao dư.
/// Prefab cần Collider2D bật Is Trigger.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StarDrop : MonoBehaviour
{
    private List<Star> StarsCollected = new List<Star>();
    private int HalfStarsLeft;
    private bool picked;

    /// <summary>Gọi lúc player chết: sao chép sao hiện tại vào vệt sáng, SaveManager đặt chỉ số về 0.</summary>
    public void Init()
    {
        if (SaveManager.Instance == null) return;
        UserData data = SaveManager.Instance.Data;

        StarsCollected = CopyStars(data.StarsCollected); // sao chép sâu, không gán thẳng danh sách
        HalfStarsLeft = data.HalfStarsLeft;
        StarManager.Instance.DropStars(transform.position, StarsCollected, HalfStarsLeft);
        if (ValueHalfStars == 0) Destroy(gameObject); // không có sao: không để lại vệt rỗng
    }

    /// <summary>Gọi lúc vào game mà save còn vệt chưa nhặt.</summary>
    public void InitFromSave()
    {
        UserData data = SaveManager.Instance.Data;
        StarsCollected = CopyStars(data.DroppedStars);
        HalfStarsLeft = data.DroppedHalfStarsLeft;
    }

    private int ValueHalfStars
    {
        get
        {
            int total = HalfStarsLeft;
            foreach (Star s in StarsCollected) total += s.AmountCollected * 2;
            return total;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (picked || SaveManager.Instance == null) return;
        if (collision.GetComponentInParent<PlayerController>() == null) return;
        if (PlayerController.Instance.IsDead) return;

        picked = true;
        StarManager.Instance.PickUpStars(StarsCollected, HalfStarsLeft);
        StarAnnouncement.Instance.SpawnAny("Lost pages return to you. Strength is recovered.");
        Destroy(gameObject);
    }

    public static List<Star> CopyStars(List<Star> source)
    {
        var copy = new List<Star>();
        foreach (Star s in source) copy.Add(new Star { Type = s.Type, AmountCollected = s.AmountCollected });
        return copy;
    }
}