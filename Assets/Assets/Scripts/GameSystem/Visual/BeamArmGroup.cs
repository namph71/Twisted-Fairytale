using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn lên Spinner (cha của các Arm). Kích hoạt toàn bộ BeamArm, có thể lệch nhịp từng cái.
/// </summary>
public class BeamArmGroup : MonoBehaviour
{
    [SerializeField] private BeamArm[] arms;
    [Tooltip("0 = tất cả cùng lúc; > 0 = lần lượt từng arm, cách nhau chừng này giây")]
    [SerializeField] private float stagger = 0f;
    [Tooltip("Tự chạy khi bật object (tiện để test)")]
    [SerializeField] private bool playOnEnable;

    private void Reset() => arms = GetComponentsInChildren<BeamArm>();

    // Start thay vì OnEnable: lúc này mọi BeamArm con đã chạy Awake xong
    private void Start()
    {
        if (playOnEnable) PlayAll();
    }

    public void PlayAll()
    {
        StopAllCoroutines();
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        foreach (BeamArm arm in arms)
        {
            arm.Play();
            if (stagger > 0f) yield return new WaitForSeconds(stagger);
        }
    }
}