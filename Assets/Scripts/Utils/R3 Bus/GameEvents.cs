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

public static class GameEvents
{
    public static readonly Subject<float> OnPlayerHit = new Subject<float>();

    public static readonly Subject<R3.Unit> OnPlayerDeath = new Subject<R3.Unit>();

    public static readonly Subject<IEnemy> OnEnemyKilled = new Subject<IEnemy>();

    public static readonly Subject<int> OnWaveStarted = new Subject<int>();

    public static readonly Subject<int> OnWaveCleared = new Subject<int>();

    public static readonly Subject<bool> OnRouletteSpinReady = new Subject<bool>();

    public static readonly Subject<bool> OnRouletteSpinning = new Subject<bool>();

    public static readonly Subject<R3.Unit> OnRouletteSpinButtonClicked = new Subject<R3.Unit>();

    public static readonly Subject<R3.Unit> OnPlayerWeaponUnlocked = new Subject<R3.Unit>();

    public static readonly Subject<ItemAcquiredData> OnItemAcquired = new Subject<ItemAcquiredData>();

    public static readonly Subject<R3.Unit> OnBackgroundDimCleared = new Subject<R3.Unit>();

    public static readonly Subject<R3.Unit> OnSecondDrawHalfCinematicRequested = new Subject<R3.Unit>();

    public static readonly Subject<R3.Unit> OnSecondDrawHalfCinematicFinished = new Subject<R3.Unit>();

    public static readonly Subject<R3.Unit> OnSecondDrawHalfCinematicImpact = new Subject<R3.Unit>();

    public static readonly Subject<R3.Unit> OnThirdDrawFinalCinematicRequested = new Subject<R3.Unit>();

    public static readonly Subject<R3.Unit> OnThirdDrawFinalCinematicImpact = new Subject<R3.Unit>();

    public static readonly Subject<R3.Unit> OnThirdDrawFinalCinematicFinished = new Subject<R3.Unit>();
}
