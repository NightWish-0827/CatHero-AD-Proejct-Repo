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
}
