using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxRepeatingLayer : MonoBehaviour
{
    [SerializeField] private bool autoUseThisSpriteRenderer = true;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private string offsetXProperty = "_OffsetX";

    [FormerlySerializedAs("scrollFactor")]
    [SerializeField, Min(0f)] private float scrollFactor = 0.35f;

    [FormerlySerializedAs("invertDirection")]
    [SerializeField] private bool invertDirection = false;

    [SerializeField] private bool normalizeByRendererWorldWidth = true;

    private MaterialPropertyBlock _mpb;
    private int _offsetXId;
    private float _offsetX;

    private void Awake()
    {
        if (spriteRenderer == null && autoUseThisSpriteRenderer)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        _mpb = new MaterialPropertyBlock();
        _offsetXId = Shader.PropertyToID(string.IsNullOrWhiteSpace(offsetXProperty) ? "_OffsetX" : offsetXProperty);
    }

    public void ApplyParallaxDeltaX(float cameraDeltaX)
    {
        if (Mathf.Approximately(cameraDeltaX, 0f)) return;
        if (spriteRenderer == null) return;
        if (Mathf.Approximately(scrollFactor, 0f)) return;

        float dx = invertDirection ? -cameraDeltaX : cameraDeltaX;

        float du = dx * scrollFactor;
        if (normalizeByRendererWorldWidth)
        {
            float w = spriteRenderer.bounds.size.x;
            if (w > 0.0001f) du /= w;
            else return;
        }
        if (Mathf.Approximately(du, 0f)) return;

        _offsetX = Mathf.Repeat(_offsetX + du, 1f);

        spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(_offsetXId, _offsetX);
        spriteRenderer.SetPropertyBlock(_mpb);
    }
}

