using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class ParallaxBackground : MonoBehaviour
{
    [SerializeField] private bool autoUseThisTransform = true;
    [SerializeField] private Transform followTransform;
    [SerializeField] private bool autoFindLayersInChildren = true;
    [SerializeField] private ParallaxRepeatingLayer[] layers;

    private Vector3 _lastCameraPos;

    private void Awake()
    {
        if (followTransform == null)
        {
            var cam = GetComponentInParent<Camera>();
            if (cam != null) followTransform = cam.transform;
        }

        if (followTransform == null && autoUseThisTransform)
        {
            followTransform = transform;
        }

        if ((layers == null || layers.Length == 0) && autoFindLayersInChildren)
        {
            layers = GetComponentsInChildren<ParallaxRepeatingLayer>(includeInactive: true);
        }
    }

    private void OnEnable()
    {
        if (followTransform == null && autoUseThisTransform) followTransform = transform;
        _lastCameraPos = followTransform != null ? followTransform.position : transform.position;
    }

    private void LateUpdate()
    {
        ApplyDeltaX();
    }

    private void OnPreRender()
    {
        ApplyDeltaX();
    }

    private void ApplyDeltaX()
    {
        if (followTransform == null) return;

        Vector3 pos = followTransform.position;
        float dx = pos.x - _lastCameraPos.x;

        if (!Mathf.Approximately(dx, 0f) && layers != null)
        {
            for (int i = 0; i < layers.Length; i++)
            {
                var layer = layers[i];
                if (layer == null) continue;
                layer.ApplyParallaxDeltaX(dx);
            }
        }

        _lastCameraPos = pos;
    }
}

