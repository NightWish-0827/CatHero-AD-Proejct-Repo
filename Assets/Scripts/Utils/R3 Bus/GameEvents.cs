using R3;

/// <summary>캣 히어로 전역 이벤트 버스. R3 Subject 기반. 추후 확장 시 동일 패턴 유지.</summary>
public static class GameEvents
{
    /// <summary>플레이어(고양이/주인) 피격. (damage)</summary>
    public static readonly Subject<float> OnPlayerHit = new Subject<float>();

    /// <summary>플레이어 사망.</summary>
    public static readonly Subject<R3.Unit> OnPlayerDeath = new Subject<R3.Unit>();

    /// <summary>적 처치. (IEnemy)</summary>
    public static readonly Subject<IEnemy> OnEnemyKilled = new Subject<IEnemy>();

    /// <summary>웨이브 시작. (waveIndex)</summary>
    public static readonly Subject<int> OnWaveStarted = new Subject<int>();

    /// <summary>웨이브 클리어. (waveIndex)</summary>
    public static readonly Subject<int> OnWaveCleared = new Subject<int>();
}
