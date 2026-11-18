using System;
using UnityEngine;

public class Orb : MonoBehaviour
{
    public event Action<Orb> OnDie;
    private void OnDisable()
    {
        OnDie?.Invoke(this);
    }
}
