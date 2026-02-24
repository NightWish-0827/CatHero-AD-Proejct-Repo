using UnityEngine;

/// <summary>플레이어 전진 이동. moveTarget이 있으면 해당 Transform 이동.</summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Vector3 moveDirection = Vector3.right;

    [Header("Target")]
    [Tooltip("이동시킬 Transform. null이면 자기 자신. 자식에 두고 부모를 넣으면 Root 이동.")]
    [SerializeField] private Transform moveTarget;

    private bool _isActive = true;

    public bool IsActive { get => _isActive; set => _isActive = value; }
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }

    private Transform _effectiveTarget;

    private void Awake()
    {
        _effectiveTarget = moveTarget != null ? moveTarget : transform;
    }

    private void Update()
    {
        if (!_isActive) return;

        _effectiveTarget.position += moveDirection.normalized * (moveSpeed * Time.deltaTime);
    }
}
