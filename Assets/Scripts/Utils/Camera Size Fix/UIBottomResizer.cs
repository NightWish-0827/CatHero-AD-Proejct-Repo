using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class UIBottomResizer : MonoBehaviour
{
    [Header("Reference")]
    public FixedWidthCamera config; // 카메라 스크립트 참조
    
    [Header("Target Dimension")]
    public float targetImageHeight = 960f; // 채워야 하는 높이 (px)

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

        // 1. Anchor 설정 (하단 Stretch)
        _rectTransform.anchorMin = new Vector2(0, 0);
        _rectTransform.anchorMax = new Vector2(1, 0);
        _rectTransform.pivot = new Vector2(0.5f, 0);

        // 2. 좌표 초기화
        _rectTransform.anchoredPosition = Vector2.zero;
        _rectTransform.sizeDelta = new Vector2(0, targetImageHeight);

        // 3. (선택적) 만약 Aspect에 따라 스케일 보정이 필요하다면 여기서 추가 로직 작성
        // 현재는 Canvas Scaler가 Match Height(1)로 되어있다는 가정하에 
        // 960px이 고정된 비율로 보입니다.
    }

#if UNITY_EDITOR
    void Update()
    {
        if (!Application.isPlaying) Resize();
    }
#endif
}