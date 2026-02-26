using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    private enum FollowUpdateMode
    {
        LateUpdate = 0,
        FixedUpdate = 1,
        PreCull = 2,
    }

    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayer = true;

    [SerializeField] private FollowUpdateMode updateMode = FollowUpdateMode.PreCull;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    public Transform Target { get => target; set => target = value; }

    [SerializeField] private bool isActive = true;
    public bool IsActive { get => isActive; set => isActive = value; }

    private int _lastUpdatedFrame = -1;

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
        if (updateMode != FollowUpdateMode.LateUpdate) return;
        Follow(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (updateMode != FollowUpdateMode.FixedUpdate) return;
        Follow(Time.fixedDeltaTime);
    }

    private void OnPreCull()
    {
        if (updateMode != FollowUpdateMode.PreCull) return;
        if (_lastUpdatedFrame == Time.frameCount) return;
        _lastUpdatedFrame = Time.frameCount;
        Follow(Time.deltaTime);
    }

    private void Follow(float deltaTime)
    {
        if (!isActive) return;
        if (target == null) return;

        Vector3 desired = target.position + offset;
        float t = 1f - Mathf.Exp(-smoothSpeed * Mathf.Max(0f, deltaTime));
        transform.position = Vector3.Lerp(transform.position, desired, t);
    }
}
