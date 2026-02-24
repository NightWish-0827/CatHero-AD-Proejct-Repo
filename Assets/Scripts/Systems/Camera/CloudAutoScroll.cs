using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class CloudAutoScroll : MonoBehaviour
{
    [SerializeField] private bool autoUseThisSpriteRenderer = true;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private string offsetXProperty = "_OffsetX";

    [SerializeField] private bool speedIsWorldUnitsPerSecond = false;

    [SerializeField] private float speed = 0.15f;
    [SerializeField] private bool invertDirection = false;
    [SerializeField] private bool useUnscaledTime = false;

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

    private void Update()
    {
        if (spriteRenderer == null) return;
        if (Mathf.Approximately(speed, 0f)) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        float duPerSecond = speed;
        if (speedIsWorldUnitsPerSecond)
        {
            float w = spriteRenderer.bounds.size.x;
            if (w > 0.0001f) duPerSecond = speed / w;
            else return;
        }
        if (invertDirection) duPerSecond = -duPerSecond;

        _offsetX = Mathf.Repeat(_offsetX + (duPerSecond * dt), 1f);

        spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(_offsetXId, _offsetX);
        spriteRenderer.SetPropertyBlock(_mpb);
    }
}

