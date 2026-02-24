using UnityEngine;

/// <summary>타겟(플레이어)을 따라가는 카메라.</summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Follow")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    public Transform Target { get => target; set => target = value; }

    private void Awake()
    {
        if (target == null && autoFindPlayer)
        {
            var player = FindObjectOfType<CatHeroPlayer>();
            if (player != null) target = player.Transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}
