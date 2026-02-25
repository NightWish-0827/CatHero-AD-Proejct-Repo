using R3;
using UnityEngine;

public readonly struct ItemAcquiredData
{
    public readonly int index;
    public readonly ItemGrade grade;
    public readonly Sprite icon;
    public readonly string description;

    public ItemAcquiredData(int index, ItemGrade grade, Sprite icon, string description)
    {
        this.index = index;
        this.grade = grade;
        this.icon = icon;
        this.description = description;
    }
}

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

    /// <summary>
    /// 룰렛이 "지금 스핀을 돌릴 수 있는 상태(Ready)"인지 여부.
    /// UI 연출(버튼 강조/배경 포커스 아웃 등)을 위해 사용합니다.
    /// </summary>
    public static readonly Subject<bool> OnRouletteSpinReady = new Subject<bool>();

    /// <summary>
    /// 룰렛이 실제로 회전 중인지 여부. (Ready 단계보다 더 강한 포커스 아웃/연출에 사용)
    /// </summary>
    public static readonly Subject<bool> OnRouletteSpinning = new Subject<bool>();

    /// <summary>
    /// 플레이어가 공격 수단(투사체/무기)을 획득했음을 알리는 이벤트.
    /// 룰렛 연출 종료와 함께 배경 디밍 해제 등에 사용합니다.
    /// </summary>
    public static readonly Subject<R3.Unit> OnPlayerWeaponUnlocked = new Subject<R3.Unit>();

    /// <summary>
    /// 아이템(투사체/강화 등) 획득 알림. 팝업/UI 연출용 payload 포함.
    /// </summary>
    public static readonly Subject<ItemAcquiredData> OnItemAcquired = new Subject<ItemAcquiredData>();

    /// <summary>
    /// 배경 디밍(포커스 아웃)이 완전히 복구(클리어)되었음을 알립니다.
    /// 하프 시네마틱 등에서 "디밍 복구 후 다음 연출"을 정확히 맞추기 위해 사용합니다.
    /// </summary>
    public static readonly Subject<R3.Unit> OnBackgroundDimCleared = new Subject<R3.Unit>();

    /// <summary>
    /// 2번째 뽑기(하프 시네마틱) 시퀀스 시작 요청.
    /// 룰렛 결과가 나온 뒤(디밍은 복구되지만 timeScale=0 유지)부터의 연출은 이 이벤트로 분리합니다.
    /// </summary>
    public static readonly Subject<R3.Unit> OnSecondDrawHalfCinematicRequested = new Subject<R3.Unit>();

    /// <summary>
    /// 2번째 뽑기(하프 시네마틱) 시퀀스 종료 알림.
    /// 수신 측(예: CatHeroPlayer)이 timeScale 재개/상태 해제를 수행할 수 있습니다.
    /// </summary>
    public static readonly Subject<R3.Unit> OnSecondDrawHalfCinematicFinished = new Subject<R3.Unit>();

    /// <summary>
    /// 2번째 뽑기(하프 시네마틱) 임팩트 지점(별 착지 직전/직후).
    /// timeScale 재개와 같은 "충격 순간" 동작을 맞추기 위해 사용합니다.
    /// </summary>
    public static readonly Subject<R3.Unit> OnSecondDrawHalfCinematicImpact = new Subject<R3.Unit>();
}
