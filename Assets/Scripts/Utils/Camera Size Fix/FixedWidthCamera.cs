using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class FixedWidthCamera : MonoBehaviour
{
    public enum ReferenceAxis { Width, Height, BothFit, BothFill }

    [Header("Settings")]
    public ReferenceAxis referenceAxis = ReferenceAxis.Height;
    public float targetWidth = 1080f;
    public float targetHeight = 1920f;
    public float pixelsPerUnit = 100f;

    private Camera _cam;
    private int _lastScreenWidth;
    private int _lastScreenHeight;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        Refresh();
    }

    // 에디터에서 값 변경 시 즉시 반영
    void OnValidate() => Refresh();

    // 성능 최적화: 매 프레임 체크 대신 필요할 때만 호출
    public void Refresh()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        float currentAspect = (float)_lastScreenWidth / _lastScreenHeight;
        _cam.orthographicSize = CalculateOrthographicSize(currentAspect);
    }

    public float CalculateOrthographicSize(float currentAspect)
    {
        float sizeByWidth = (targetWidth / pixelsPerUnit) / (2f * currentAspect);
        float sizeByHeight = (targetHeight / pixelsPerUnit) / 2f;

        return referenceAxis switch
        {
            ReferenceAxis.Width => sizeByWidth,
            ReferenceAxis.Height => sizeByHeight,
            ReferenceAxis.BothFit => Mathf.Max(sizeByWidth, sizeByHeight),
            ReferenceAxis.BothFill => Mathf.Min(sizeByWidth, sizeByHeight),
            _ => sizeByHeight
        };
    }

    // 캔버스 크기 변경 등 해상도 변화 감지 시 외부에서 호출 가능
    void OnRectTransformDimensionsChange()
    {
        if (_lastScreenWidth != Screen.width || _lastScreenHeight != Screen.height)
        {
            Refresh();
        }
    }
}