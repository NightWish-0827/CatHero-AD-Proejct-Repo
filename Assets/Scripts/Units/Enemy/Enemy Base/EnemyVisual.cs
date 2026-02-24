using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    public SpriteRenderer SpriteRenderer => spriteRenderer;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }
}
