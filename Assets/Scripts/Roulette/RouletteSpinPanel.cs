using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using DG.Tweening;

public class RouletteSpinPanel : MonoBehaviour
{
    [Serializable]
    public struct GradeColorPreset
    {
        public ItemGrade grade;
        public Color glowColor;
    }

    [Serializable]
    public struct IndexToGrade
    {
        public int index;
        public ItemGrade grade;
    }

    [Header("References")]
    [SerializeField] private Roulette roulette;
    [SerializeField] private Button spinButton;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Spin Color Targets (tint to grade)")]
    [SerializeField] private Image lineImage;
    [SerializeField] private Image bgOutlineImage;
    [SerializeField] private Image centerOutSideImage;

    [Header("Spin Color Presets (Grade -> Glow Color)")]
    [SerializeField] private GradeColorPreset[] gradeColors = new GradeColorPreset[]
    {
        new GradeColorPreset{ grade = ItemGrade.Grade1, glowColor = new Color(0.9f,0.9f,0.9f,1f) },
        new GradeColorPreset{ grade = ItemGrade.Grade2, glowColor = new Color(0.4f,0.9f,1f,1f) },
        new GradeColorPreset{ grade = ItemGrade.Grade3, glowColor = new Color(1f,0.75f,0.25f,1f) },
    };

    [Header("Post-Spin Hold (Unscaled)")]
    [SerializeField, Min(0f)] private float rewardRevealHoldSeconds = 0.6f;

    [Header("Panel Fade (Unscaled)")]
    [SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0f;
    [SerializeField, Min(0f)] private float showFadeDuration = 0.12f;
    [SerializeField, Min(0f)] private float hideFadeDuration = 0.18f;
    [SerializeField] private Ease showFadeEase = Ease.OutQuad;
    [SerializeField] private Ease hideFadeEase = Ease.OutQuad;

    [Header("Spin Color Tween (Unscaled)")]
    [SerializeField, Min(0f)] private float spinColorDuration = 0.35f;
    [SerializeField] private Ease spinColorEase = Ease.OutQuad;

    [SerializeField] private IndexToGrade[] indexToGrade = new IndexToGrade[0];

    [Header("Animation Hooks (Optional)")]
    [SerializeField] private UnityEvent onPanelShown;
    [SerializeField] private UnityEvent onReadyToSpin;
    [SerializeField] private UnityEvent onSpinStarted;
    [SerializeField] private UnityEvent onSpinCompleted;
    [SerializeField] private UnityEvent onPanelHidden;
    [SerializeField] private UnityEvent onRewardRevealed;

    private bool _isSpinning;

    private Tween _panelFadeTween;
    private Tween _spinColorTween;
    private Color _lineBaseColor = Color.white;
    private Color _bgOutlineBaseColor = Color.white;
    private Color _centerOutSideBaseColor = Color.white;

    public float RewardRevealHoldSeconds => rewardRevealHoldSeconds;

    private void Awake()
    {
        roulette ??= GetComponentInChildren<Roulette>(true);
        canvasGroup ??= GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        CacheBaseColors();
        ApplyBaseColors();
        SetPanelVisibleImmediate(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        GameEvents.OnRouletteSpinning.OnNext(false);
        KillPanelTweens();
        ApplyBaseColors();
        SetPanelVisible(true);
        onPanelShown?.Invoke();
        SetReady(true);
    }

    public void Hide()
    {
        SetReady(false);
        GameEvents.OnRouletteSpinning.OnNext(false);
        KillPanelTweens();
        ResetSpinColorsOnHide();
        SetPanelVisible(false, invokeHiddenEvent: true);
    }

    private void SetReady(bool ready)
    {
        if (spinButton != null)
        {
            spinButton.interactable = ready && !_isSpinning;
        }

        GameEvents.OnRouletteSpinReady.OnNext(ready && !_isSpinning);

        if (ready)
        {
            onReadyToSpin?.Invoke();
        }
    }

    public async UniTask<RoulettePieceData> WaitForClickAndSpinFixedAsync(int fixedIndex, int angleRoll, CancellationToken token = default)
    {
        if (roulette == null)
        {
            roulette = GetComponentInChildren<Roulette>(true);
        }

        if (roulette == null || spinButton == null)
        {
            Debug.LogWarning("[RouletteSpinPanel] Missing roulette or spinButton reference. Falling back to auto spin.");
            return roulette != null ? await roulette.SpinFixedAsync(fixedIndex, angleRoll) : null;
        }

        _isSpinning = false;
        SetReady(true);

        var tcs = new UniTaskCompletionSource();

        void OnClick()
        {
            tcs.TrySetResult();
        }

        spinButton.onClick.AddListener(OnClick);
        try
        {
            await tcs.Task.AttachExternalCancellation(token);
        }
        finally
        {
            spinButton.onClick.RemoveListener(OnClick);
        }

        _isSpinning = true;
        GameEvents.OnRouletteSpinning.OnNext(true);
        SetReady(false); // Ready 해제(=디밍 1단계 해제) 후, Spinning으로 2단계 디밍 진입

        // 스핀 시작과 동시에 "획득 등급" 색상으로 3개 이미지를 서서히 틴트
        PlaySpinColorTweenForFixedIndex(fixedIndex);

        onSpinStarted?.Invoke();

        try
        {
            RoulettePieceData result = await roulette.SpinFixedAsync(fixedIndex, angleRoll);

            onSpinCompleted?.Invoke();

            // 보상(무기) 획득 연출: 등급에 따라 Glow 색/크기 교체 후 회전
            if (result != null)
            {
                ItemGrade grade = GetGradeForIndex(result.index);
                GameEvents.OnItemAcquired.OnNext(new ItemAcquiredData(result.index, grade, result.icon, result.description));
                onRewardRevealed?.Invoke();

                // 리빌이 충분히 보이도록 홀드하는 동안에도 2단계 디밍을 유지한다.
                if (rewardRevealHoldSeconds > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(rewardRevealHoldSeconds), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, token);
                }
            }

            return result;
        }
        finally
        {
            _isSpinning = false;
            GameEvents.OnRouletteSpinning.OnNext(false);
        }
    }

    private ItemGrade GetGradeForIndex(int index)
    {
        if (indexToGrade != null)
        {
            for (int i = 0; i < indexToGrade.Length; i++)
            {
                if (indexToGrade[i].index == index) return indexToGrade[i].grade;
            }
        }
        return ItemGrade.Grade1;
    }

    private void CacheBaseColors()
    {
        if (lineImage != null) _lineBaseColor = lineImage.color;
        if (bgOutlineImage != null) _bgOutlineBaseColor = bgOutlineImage.color;
        if (centerOutSideImage != null) _centerOutSideBaseColor = centerOutSideImage.color;
    }

    private void ApplyBaseColors()
    {
        _spinColorTween?.Kill();
        _spinColorTween = null;

        if (lineImage != null) lineImage.color = _lineBaseColor;
        if (bgOutlineImage != null) bgOutlineImage.color = _bgOutlineBaseColor;
        if (centerOutSideImage != null) centerOutSideImage.color = _centerOutSideBaseColor;
    }

    private void ResetSpinColorsOnHide()
    {
        // 요구사항: 패널 페이드 아웃 시 이미지 색상은 반드시 원복되어야 함.
        _spinColorTween?.Kill();
        _spinColorTween = null;

        if (lineImage == null && bgOutlineImage == null && centerOutSideImage == null) return;

        float dur = Mathf.Max(0f, hideFadeDuration);
        if (Mathf.Approximately(dur, 0f))
        {
            ApplyBaseColors();
            return;
        }

        var seq = DOTween.Sequence().SetUpdate(true).SetTarget(this);
        if (lineImage != null) seq.Join(lineImage.DOColor(_lineBaseColor, dur).SetEase(hideFadeEase).SetUpdate(true));
        if (bgOutlineImage != null) seq.Join(bgOutlineImage.DOColor(_bgOutlineBaseColor, dur).SetEase(hideFadeEase).SetUpdate(true));
        if (centerOutSideImage != null) seq.Join(centerOutSideImage.DOColor(_centerOutSideBaseColor, dur).SetEase(hideFadeEase).SetUpdate(true));
        _spinColorTween = seq;
    }

    private Color GetGlowColorForGrade(ItemGrade grade)
    {
        if (gradeColors != null)
        {
            for (int i = 0; i < gradeColors.Length; i++)
            {
                if (gradeColors[i].grade == grade) return gradeColors[i].glowColor;
            }
        }
        return Color.white;
    }

    private void PlaySpinColorTweenForFixedIndex(int fixedIndex)
    {
        Color target = GetGlowColorForGrade(GetGradeForIndex(fixedIndex));

        _spinColorTween?.Kill();
        _spinColorTween = null;

        // 대상이 연결되지 않은 경우는 무시 (의존성 없는 세팅 허용)
        if (lineImage == null && bgOutlineImage == null && centerOutSideImage == null) return;

        var seq = DOTween.Sequence().SetUpdate(true).SetTarget(this);
        if (lineImage != null) seq.Join(lineImage.DOColor(target, spinColorDuration).SetEase(spinColorEase).SetUpdate(true));
        if (bgOutlineImage != null) seq.Join(bgOutlineImage.DOColor(target, spinColorDuration).SetEase(spinColorEase).SetUpdate(true));
        if (centerOutSideImage != null) seq.Join(centerOutSideImage.DOColor(target, spinColorDuration).SetEase(spinColorEase).SetUpdate(true));
        _spinColorTween = seq;
    }

    private void KillPanelTweens()
    {
        _panelFadeTween?.Kill();
        _panelFadeTween = null;
    }

    private void SetPanelVisibleImmediate(bool visible)
    {
        if (canvasGroup == null) return;

        float a = visible ? 1f : hiddenAlpha;
        canvasGroup.alpha = a;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void SetPanelVisible(bool visible, bool invokeHiddenEvent = false)
    {
        if (canvasGroup == null)
        {
            if (!visible && invokeHiddenEvent) onPanelHidden?.Invoke();
            return;
        }

        float to = visible ? 1f : hiddenAlpha;
        float dur = visible ? showFadeDuration : hideFadeDuration;
        Ease ease = visible ? showFadeEase : hideFadeEase;

        // 클릭 차단은 페이드 완료 시점에 맞추는 게 자연스러움
        if (visible)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        float from = canvasGroup.alpha;
        _panelFadeTween = DOTween.To(() => from, v =>
            {
                from = v;
                canvasGroup.alpha = v;
            }, to, dur)
            .SetEase(ease)
            .SetUpdate(true)
            .SetTarget(canvasGroup)
            .OnComplete(() =>
            {
                if (!visible)
                {
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;

                    // 페이드 완료 시점에 색상을 강제 원복(잔여 틴트/부동소수 누적 방지)
                    ApplyBaseColors();

                    if (invokeHiddenEvent) onPanelHidden?.Invoke();
                }
            });
    }
}

