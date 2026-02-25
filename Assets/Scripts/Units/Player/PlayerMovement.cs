using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Vector3 moveDirection = Vector3.right;

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
