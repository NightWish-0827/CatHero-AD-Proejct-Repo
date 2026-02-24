using UnityEngine;
using R3;

/// <summary>캣 히어로 기본 사이클 오케스트레이션. 스테이지 시작 → 스폰 → 플레이어 사망 시 게임 오버.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CatHeroPlayer player;
    [SerializeField] private EnemySpawner enemySpawner;

    private CompositeDisposable disposables;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        disposables = new CompositeDisposable();

        GameEvents.OnPlayerDeath
            .Subscribe(_ => OnGameOver())
            .AddTo(disposables);
    }

    private void Start()
    {
        StartStage();
    }

    private void OnDestroy()
    {
        disposables?.Dispose();
        if (Instance == this) Instance = null;
    }

    /// <summary>스테이지 시작. 플레이어를 타겟으로 적 스폰 시작.</summary>
    private void StartStage()
    {
        if (player == null)
        {
            player = FindObjectOfType<CatHeroPlayer>();
        }

        if (enemySpawner == null)
        {
            enemySpawner = FindObjectOfType<EnemySpawner>();
        }

        if (player != null && enemySpawner != null)
        {
            enemySpawner.StartSpawning(player.Transform);
        }
        else
        {
            Debug.LogWarning("[GameManager] CatHeroPlayer 또는 EnemySpawner가 없습니다.");
        }
    }

    private void OnGameOver()
    {
        if (enemySpawner != null)
        {
            enemySpawner.StopSpawning();
        }

        Debug.Log("[GameManager] 게임 오버");
        // TODO: 게임 오버 UI, 재시작 등
    }
}
