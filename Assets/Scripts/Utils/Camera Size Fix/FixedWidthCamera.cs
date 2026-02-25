using UnityEngine;

[ExecuteInEditMode]
public class FixedWidthCamera : MonoBehaviour
{
    public enum ReferenceAxis
    {
        Width,
        Height
    }

    [SerializeField]
    public ReferenceAxis referenceAxis = ReferenceAxis.Height;

    public float targetWidth = 1080f;
    public float targetHeight = 1920f;

    public float pixelsPerUnit = 100f;

    private int _lastScreenWidth;
    private int _lastScreenHeight;

    void Start()
    {
        SetCameraSize();
    }

    void OnValidate()
    {
        SetCameraSize();
    }

    void Update()
    {
        if (_lastScreenWidth != Screen.width || _lastScreenHeight != Screen.height)
        {
            SetCameraSize();
        }
    }

    public void SetCameraSize()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null) return;

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        float currentAspect = (float)Screen.width / (float)Screen.height;

        float desiredSize;
        if (referenceAxis == ReferenceAxis.Width)
        {
            desiredSize = (targetWidth / pixelsPerUnit) / (2f * currentAspect);
        }
        else
        {
            desiredSize = (targetHeight / pixelsPerUnit) / 2f;
        }

        cam.orthographicSize = desiredSize;
    }
}