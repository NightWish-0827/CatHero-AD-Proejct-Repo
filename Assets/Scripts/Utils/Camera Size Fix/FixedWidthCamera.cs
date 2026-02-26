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

    void OnValidate() => Refresh();

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

    void OnRectTransformDimensionsChange()
    {
        if (_lastScreenWidth != Screen.width || _lastScreenHeight != Screen.height)
        {
            Refresh();
        }
    }
}