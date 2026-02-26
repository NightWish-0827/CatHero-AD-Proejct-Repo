using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class UIBottomResizer : MonoBehaviour
{
    [Header("Reference")]
    public FixedWidthCamera config;
    
    [Header("Target Dimension")]
    public float targetImageHeight = 960f;

    private RectTransform _rectTransform;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    void Start() => Resize();

    [ContextMenu("Execute Resize")]
    public void Resize()
    {
        if (config == null || _rectTransform == null) return;

        _rectTransform.anchorMin = new Vector2(0, 0);
        _rectTransform.anchorMax = new Vector2(1, 0);
        _rectTransform.pivot = new Vector2(0.5f, 0);

        _rectTransform.anchoredPosition = Vector2.zero;
        _rectTransform.sizeDelta = new Vector2(0, targetImageHeight);

    }

#if UNITY_EDITOR
    void Update()
    {
        if (!Application.isPlaying) Resize();
    }
#endif
}