using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public class ScreenFadeCanvasGroup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Raycast (Optional)")]
    [SerializeField] private bool blockRaycastsWhenVisible = true;

    private Tween _tween;

    private void Awake()
    {
        EnsureCanvasGroup();
    }

    private void OnDisable()
    {
        KillTween();
    }

    public void SetAlphaImmediate(float alpha)
    {
        EnsureCanvasGroup();
        if (canvasGroup == null) return;
        canvasGroup.alpha = Mathf.Clamp01(alpha);
        UpdateRaycastBlock();
    }

    public void FadeTo(float alpha, float duration, Ease ease)
    {
        EnsureCanvasGroup();
        if (canvasGroup == null) return;

        KillTween();

        float to = Mathf.Clamp01(alpha);
        float dur = Mathf.Max(0f, duration);

        if (Mathf.Approximately(dur, 0f))
        {
            canvasGroup.alpha = to;
            UpdateRaycastBlock();
            return;
        }

        _tween = canvasGroup
            .DOFade(to, dur)
            .SetEase(ease)
            .SetUpdate(true)
            .SetTarget(this)
            .OnUpdate(UpdateRaycastBlock)
            .OnComplete(UpdateRaycastBlock);
    }

    private void UpdateRaycastBlock()
    {
        if (canvasGroup == null) return;
        if (!blockRaycastsWhenVisible) return;

        bool visible = canvasGroup.alpha >= 0.001f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = false;
    }

    private void KillTween()
    {
        if (_tween != null && _tween.active) _tween.Kill();
        _tween = null;
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup != null) return;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }
}

