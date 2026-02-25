using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public class RouletteSpinPanel : MonoBehaviour
{
    [Serializable]
    public struct IndexToGrade
    {
        public int index;
        public ItemGrade grade;
    }

    [Header("References")]
    [SerializeField] private Roulette roulette;
    [SerializeField] private Button spinButton;

    [Header("Post-Spin Hold (Unscaled)")]
    [SerializeField, Min(0f)] private float rewardRevealHoldSeconds = 0.6f;

    [SerializeField] private IndexToGrade[] indexToGrade = new IndexToGrade[0];

    [Header("Animation Hooks (Optional)")]
    [SerializeField] private UnityEvent onPanelShown;
    [SerializeField] private UnityEvent onReadyToSpin;
    [SerializeField] private UnityEvent onSpinStarted;
    [SerializeField] private UnityEvent onSpinCompleted;
    [SerializeField] private UnityEvent onPanelHidden;
    [SerializeField] private UnityEvent onRewardRevealed;

    private bool _isSpinning;

    public float RewardRevealHoldSeconds => rewardRevealHoldSeconds;

    private void Awake()
    {
        roulette ??= GetComponentInChildren<Roulette>(true);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        GameEvents.OnRouletteSpinning.OnNext(false);
        onPanelShown?.Invoke();
        SetReady(true);
    }

    public void Hide()
    {
        SetReady(false);
        GameEvents.OnRouletteSpinning.OnNext(false);
        onPanelHidden?.Invoke();
        gameObject.SetActive(false);
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
}

