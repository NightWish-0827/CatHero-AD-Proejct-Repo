using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;

public class ItemAcquiredPopupView : MonoBehaviour
{
    [Serializable]
    public struct GradePopupPreset
    {
        public ItemGrade grade;
        public Sprite bgSpriteOverride;
        public Color bgColor;
        public Color iconColor;
        public Vector3 popupScale;

        [Header("Glow (Behind Icon)")]
        public Color glowColor;
        public Vector3 glowScale;
    }

    [Header("References")]
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image iconBgImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private RectTransform glowTransform;
    [SerializeField] private Graphic glowGraphic;

    [Header("Grade Presets (Optional)")]
    [SerializeField] private GradePopupPreset[] gradePresets = new GradePopupPreset[]
    {
        new GradePopupPreset{ grade = ItemGrade.Grade1, bgColor = Color.white, iconColor = Color.white, popupScale = Vector3.one, glowColor = new Color(0.9f,0.9f,0.9f,1f), glowScale = Vector3.one },
        new GradePopupPreset{ grade = ItemGrade.Grade2, bgColor = new Color(0.4f,0.9f,1f,1f), iconColor = Color.white, popupScale = Vector3.one * 1.05f, glowColor = new Color(0.4f,0.9f,1f,1f), glowScale = Vector3.one * 1.25f },
        new GradePopupPreset{ grade = ItemGrade.Grade3, bgColor = new Color(1f,0.75f,0.25f,1f), iconColor = Color.white, popupScale = Vector3.one * 1.1f, glowColor = new Color(1f,0.75f,0.25f,1f), glowScale = Vector3.one * 1.5f },
    };

    [Header("Animation (Unscaled)")]
    [SerializeField, Min(0f)] private float showDuration = 0.18f;
    [SerializeField, Min(0f)] private float holdDuration = 0.6f;
    [SerializeField, Min(0f)] private float hideDuration = 0.16f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InQuad;
    [SerializeField] private Vector3 showFromScale = Vector3.one * 0.85f;

    [Header("Glow Rotate (Unscaled)")]
    [SerializeField] private bool glowRotateEnabled = true;
    [SerializeField] private float glowDegreesPerSecond = 120f;

    private Tween _tween;
    private Tween _glowRotateTween;
    private Vector3 _baseScale = Vector3.one;
    private Vector3 _glowBaseScale = Vector3.one;
    private Color _glowBaseColor = Color.white;

    private void Awake()
    {
        popupRoot ??= transform as RectTransform;
        if (popupRoot != null) _baseScale = popupRoot.localScale;
        canvasGroup ??= GetComponentInChildren<CanvasGroup>(true);

        if (glowTransform != null) _glowBaseScale = glowTransform.localScale;
        if (glowGraphic != null) _glowBaseColor = glowGraphic.color;
    }

    private void OnDisable()
    {
        // SafeMode: 비활성/파괴 타이밍에 Tween이 남아있으면 "missing target/field" 경고가 날 수 있어 정리한다.
        KillTween();
    }

    public void Initialize(ItemAcquiredData data)
    {
        if (iconImage != null) iconImage.sprite = data.icon;
        ApplyGradePreset(data.grade);
    }

    public void SetHoldDuration(float seconds)
    {
        holdDuration = Mathf.Max(0f, seconds);
    }

    public async UniTask PlayAndDisposeAsync(CancellationToken token = default)
    {
        try
        {
            KillTween();

            if (popupRoot != null) popupRoot.localScale = showFromScale;
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            if (glowTransform != null) glowTransform.localRotation = Quaternion.identity;
            StartGlowRotate();

            Sequence seq = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            if (popupRoot != null) seq.Join(popupRoot.DOScale(_baseScale, showDuration).SetEase(showEase).SetUpdate(true));
            if (canvasGroup != null) seq.Join(canvasGroup.DOFade(1f, showDuration).SetEase(Ease.OutQuad).SetUpdate(true));

            _tween = seq;
            await seq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

            if (holdDuration > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, token);
            }

            Sequence outSeq = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            if (popupRoot != null) outSeq.Join(popupRoot.DOScale(showFromScale, hideDuration).SetEase(hideEase).SetUpdate(true));
            if (canvasGroup != null) outSeq.Join(canvasGroup.DOFade(0f, hideDuration).SetEase(hideEase).SetUpdate(true));
            _tween = outSeq;

            await outSeq.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);
        }
        catch (OperationCanceledException)
        {
            // Spawner/Scene disable 등으로 취소될 수 있음. 안전 정리 후 종료.
        }
        finally
        {
            KillTween();
            if (this != null && gameObject != null)
            {
                Destroy(gameObject);
            }
        }
    }

    private void ApplyGradePreset(ItemGrade grade)
    {
        if (gradePresets == null || gradePresets.Length == 0) return;

        for (int i = 0; i < gradePresets.Length; i++)
        {
            if (gradePresets[i].grade != grade) continue;

            var p = gradePresets[i];

            if (popupRoot != null && p.popupScale != Vector3.zero)
            {
                _baseScale = p.popupScale;
            }

            if (iconBgImage != null)
            {
                if (p.bgSpriteOverride != null) iconBgImage.sprite = p.bgSpriteOverride;
                iconBgImage.color = p.bgColor.a > 0f ? p.bgColor : iconBgImage.color;
            }

            if (iconImage != null)
            {
                iconImage.color = p.iconColor.a > 0f ? p.iconColor : iconImage.color;
            }

            if (glowTransform != null && p.glowScale != Vector3.zero)
            {
                glowTransform.localScale = p.glowScale;
            }
            else if (glowTransform != null)
            {
                glowTransform.localScale = _glowBaseScale;
            }

            if (glowGraphic != null)
            {
                glowGraphic.color = p.glowColor.a > 0f ? p.glowColor : _glowBaseColor;
            }

            return;
        }
    }

    private void StartGlowRotate()
    {
        if (!glowRotateEnabled) return;
        if (glowTransform == null) return;

        _glowRotateTween?.Kill();
        _glowRotateTween = null;

        float dps = Mathf.Abs(glowDegreesPerSecond);
        if (dps < 0.01f) dps = 120f;
        float duration = 360f / dps;

        _glowRotateTween = glowTransform
            .DORotate(new Vector3(0f, 0f, 360f), duration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1)
            .SetUpdate(true)
            .SetTarget(glowTransform);
    }

    private void KillTween()
    {
        if (_tween != null && _tween.active) _tween.Kill();
        _tween = null;

        if (_glowRotateTween != null && _glowRotateTween.active) _glowRotateTween.Kill();
        _glowRotateTween = null;
    }
}

