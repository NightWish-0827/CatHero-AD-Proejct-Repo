using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

[DisallowMultipleComponent]
public class ItemAcquiredPopupSpawner : MonoBehaviour
{
    [Header("Prefab/Parent")]
    [SerializeField] private ItemAcquiredPopupView popupPrefab;
    [SerializeField] private Transform popupParent;

    [Header("Queue")]
    [SerializeField] private bool overridePopupHoldDuration = true;
    [SerializeField, Min(0f)] private float popupHoldDurationSeconds = 0.6f;
    [SerializeField, Min(0f)] private float gapSecondsBetweenPopups = 0.05f;

    private readonly Queue<ItemAcquiredData> _queue = new Queue<ItemAcquiredData>();
    private bool _isPlaying;
    private CompositeDisposable _disposables;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        if (popupParent == null) popupParent = transform;
    }

    private void OnEnable()
    {
        _disposables = new CompositeDisposable();
        _cts = new CancellationTokenSource();

        GameEvents.OnItemAcquired
            .Subscribe(OnItemAcquired)
            .AddTo(_disposables);
    }

    private void OnDisable()
    {
        _disposables?.Dispose();
        _disposables = null;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        _queue.Clear();
        _isPlaying = false;
    }

    private void OnItemAcquired(ItemAcquiredData data)
    {
        _queue.Enqueue(data);
        if (_isPlaying) return;
        PlayQueueAsync(_cts.Token).Forget();
    }

    private async UniTaskVoid PlayQueueAsync(CancellationToken token)
    {
        _isPlaying = true;

        try
        {
            while (!token.IsCancellationRequested && _queue.Count > 0)
            {
                if (popupPrefab == null)
                {
                    Debug.LogWarning("[ItemAcquiredPopupSpawner] popupPrefab is null. Drop queued popups.");
                    _queue.Clear();
                    return;
                }

                ItemAcquiredData data = _queue.Dequeue();

                var view = Instantiate(popupPrefab, popupParent != null ? popupParent : transform);
                if (overridePopupHoldDuration)
                {
                    view.SetHoldDuration(popupHoldDurationSeconds);
                }
                view.Initialize(data);

                await view.PlayAndDisposeAsync(token);

                if (gapSecondsBetweenPopups > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(gapSecondsBetweenPopups), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, token);
                }
            }
        }
        finally
        {
            _isPlaying = false;
        }
    }
}

