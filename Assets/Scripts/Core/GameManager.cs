using UnityEngine;
using R3;

[SceneReferral]
public class GameManager : MonoBehaviour
{
    [SceneInject] private CatHeroPlayer _player;
    [SceneInject] private EnemySpawner _enemySpawner;

    private CompositeDisposable _disposables;

    private void Awake()
    {
        _disposables = new CompositeDisposable();

        GameEvents.OnPlayerDeath
            .Subscribe(_ => OnGameOver())
            .AddTo(_disposables);
    }

    private void Start()
    {
        StartStage();
    }

    private void OnDestroy()
    {
        _disposables?.Dispose();
    }

    private void StartStage()
    {
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
}
