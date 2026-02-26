using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;

[SceneReferral]
public class GameManager : MonoBehaviour
{
    [SceneInject] private CatHeroPlayer _player;
    [SceneInject] private EnemySpawner _enemySpawner;

    [Header("Stage Start/End Fade")]
    [SerializeField] private ScreenFadeCanvasGroup screenFader;
    [SerializeField, Min(0f)] private float startFadeInDuration = 0.25f;
    [SerializeField] private Ease startFadeInEase = Ease.OutQuad;
    [SerializeField, Min(0f)] private float endFadeOutDelaySeconds = 1.0f;
    [SerializeField, Min(0f)] private float endFadeOutDuration = 0.35f;
    [SerializeField] private Ease endFadeOutEase = Ease.InQuad;

    [Header("Stage End Player Speed Boost")]
    [SerializeField, Min(0f)] private float endMoveSpeedMultiplier = 1.5f;

    private CompositeDisposable _disposables;
    private CancellationTokenSource _endCts;
    private bool _endSequenceStarted;
    private float _baseMoveSpeed;
    private bool _baseMoveSpeedCached;

    public PlayerMovement _playerMovement;

    private void Awake()
    {
        _disposables = new CompositeDisposable();

        GameEvents.OnPlayerDeath
            .Subscribe(_ => OnGameOver())
            .AddTo(_disposables);

        // 플레이어블 광고: 3번째(마지막) 아이템 연출까지 보여준 뒤에는 추가 웨이브 스폰을 중지합니다.
        GameEvents.OnThirdDrawFinalCinematicFinished
            .Subscribe(_ => OnPlayableAdFinished())
            .AddTo(_disposables);
    }

    private void Start()
    {
        StartStage();
    }

    private void OnDestroy()
    {
        _disposables?.Dispose();

        _endCts?.Cancel();
        _endCts?.Dispose();
        _endCts = null;
    }

    private void StartStage()
    {
        // Fallback (SceneInject 실패 시) - 개발 중 씬 세팅 누락을 방지
        if (_playerMovement == null) _playerMovement = FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (screenFader == null) screenFader = FindFirstObjectByType<ScreenFadeCanvasGroup>(FindObjectsInactive.Include);

        if (_playerMovement != null && !_baseMoveSpeedCached)
        {
            _baseMoveSpeed = _playerMovement.MoveSpeed;
            _baseMoveSpeedCached = true;
        }

        // 시작은 즉시 페이드 인(검정→투명)
        if (screenFader != null)
        {
            screenFader.SetAlphaImmediate(1f);
            screenFader.FadeTo(0f, startFadeInDuration, startFadeInEase);
        }

        if (_player != null && _enemySpawner != null)
        {
            _enemySpawner.StartSpawning(_player.Transform);
        }
    }

    private void OnGameOver()
    {
        if (_enemySpawner != null)
        {
            _enemySpawner.StopSpawning();
        }

        Debug.Log("[GameManager] 게임 오버");
        // TODO: 게임 오버 UI, 재시작 등
    }

    private void OnPlayableAdFinished()
    {
        if (_enemySpawner != null)
        {
            _enemySpawner.StopSpawning();
        }

        Debug.Log("[GameManager] 플레이어블 광고 종료(3번째 아이템 연출 완료)");

        if (_endSequenceStarted) return;
        _endSequenceStarted = true;

        _endCts?.Cancel();
        _endCts?.Dispose();
        _endCts = new CancellationTokenSource();

        StageEndSequenceAsync(_endCts.Token).Forget();
    }

    private async UniTaskVoid StageEndSequenceAsync(CancellationToken token)
    {
        try
        {
            // "마지막 적 해치움" = 스폰이 멈춘 상태에서 활성 적이 0이 되는 시점
            await UniTask.WaitUntil(() => EnemyRegistry.Enemies.Count == 0, cancellationToken: token);

            if (_playerMovement == null) _playerMovement = FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
            if (_playerMovement != null)
            {
                if (!_baseMoveSpeedCached)
                {
                    _baseMoveSpeed = _playerMovement.MoveSpeed;
                    _baseMoveSpeedCached = true;
                }

                _playerMovement.MoveSpeed = _baseMoveSpeed * Mathf.Max(0f, endMoveSpeedMultiplier);
            }

            if (endFadeOutDelaySeconds > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(endFadeOutDelaySeconds), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, token);
            }

            if (screenFader == null) screenFader = FindFirstObjectByType<ScreenFadeCanvasGroup>(FindObjectsInactive.Include);
            screenFader?.FadeTo(1f, endFadeOutDuration, endFadeOutEase);
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }
}
