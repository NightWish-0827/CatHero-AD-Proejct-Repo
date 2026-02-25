using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class ProjectileLineTrail : MonoBehaviour
{
    [Header("Sampling")]
    [SerializeField] private int maxPoints = 18;
    [SerializeField] private float minVertexDistance = 0.06f;
    [SerializeField] private float maxAgeSeconds = 0.18f;

    [Header("Pop In")]
    [SerializeField] private float popInSeconds = 0.05f;
    [SerializeField] private AnimationCurve popInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private LineRenderer _lr;
    private readonly List<Vector3> _points = new List<Vector3>(32);
    private readonly List<float> _times = new List<float>(32);
    private Vector3[] _buffer;
    private bool _active;
    private float _enabledAt;

    private void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        if (_lr != null) _lr.enabled = false;
    }

    private void OnDisable()
    {
        DisableTrail();
    }

    public void EnableTrail()
    {
        if (_lr == null) _lr = GetComponent<LineRenderer>();
        if (_lr == null) return;

        _active = true;
        _enabledAt = Time.time;

        _points.Clear();
        _times.Clear();

        _lr.positionCount = 0;
        _lr.enabled = true;
        PushPoint(transform.position);
    }

    public void DisableTrail()
    {
        _active = false;
        if (_lr == null) return;
        _lr.enabled = false;
        _lr.positionCount = 0;
        _points.Clear();
        _times.Clear();
    }

    private void LateUpdate()
    {
        if (!_active || _lr == null || !_lr.enabled) return;

        Vector3 p = transform.position;
        if (_points.Count == 0 || (p - _points[_points.Count - 1]).sqrMagnitude >= (minVertexDistance * minVertexDistance))
        {
            PushPoint(p);
        }

        float now = Time.time;
        float cutoff = now - Mathf.Max(0.01f, maxAgeSeconds);
        while (_times.Count > 0 && _times[0] < cutoff)
        {
            _times.RemoveAt(0);
            _points.RemoveAt(0);
        }

        while (_points.Count > Mathf.Max(2, maxPoints))
        {
            _times.RemoveAt(0);
            _points.RemoveAt(0);
        }

        if (_points.Count < 2)
        {
            _lr.positionCount = 0;
            return;
        }

        float alpha = 1f;
        if (popInSeconds > 0f)
        {
            float t01 = Mathf.Clamp01((now - _enabledAt) / popInSeconds);
            alpha = popInCurve != null ? Mathf.Clamp01(popInCurve.Evaluate(t01)) : t01;
        }

        // alpha를 위해 width를 살짝 스케일(머티리얼 알파 컨트롤이 없을 수도 있으니 안전하게)
        float widthMul = Mathf.Lerp(0.15f, 1f, alpha);
        _lr.widthMultiplier = widthMul;

        int count = _points.Count;
        _lr.positionCount = count;
        if (_buffer == null || _buffer.Length < count) _buffer = new Vector3[Mathf.Max(32, count)];
        for (int i = 0; i < count; i++) _buffer[i] = _points[i];
        _lr.SetPositions(_buffer);
    }

    private void PushPoint(Vector3 p)
    {
        _points.Add(p);
        _times.Add(Time.time);
    }
}

