using UnityEngine;
using R3;

/// <summary>
/// 하프 시네마틱/디밍/팝업 연출을 빠르게 반복 테스트하기 위한 퀵 테스터.
/// 씬에 1개 배치 후, 키 입력 또는 OnGUI 버튼으로 GameEvents를 직접 발행합니다.
/// </summary>
[DisallowMultipleComponent]
public class SecondDrawHalfCinematicQuickTester : MonoBehaviour
{
    [Header("OnGUI")]
    [SerializeField] private bool showOnGui = true;
    [SerializeField] private Vector2 guiPos = new Vector2(20, 20);
    [SerializeField] private float guiWidth = 320;

    [Header("TimeScale")]
    [SerializeField] private bool setTimeScale0BeforeRequest = true;
    [SerializeField] private float timeScaleWhenResume = 1f;

    [Header("Hotkeys")]
    [SerializeField] private KeyCode keyRequestCinematic = KeyCode.F1;
    [SerializeField] private KeyCode keyDimCleared = KeyCode.F2;
    [SerializeField] private KeyCode keyImpact = KeyCode.F3;
    [SerializeField] private KeyCode keyFinished = KeyCode.F4;
    [SerializeField] private KeyCode keyTimeScale0 = KeyCode.F5;
    [SerializeField] private KeyCode keyTimeScale1 = KeyCode.F6;

    [Header("Extra (Optional)")]
    [SerializeField] private bool alsoFireRouletteSpinningForDim = true;
    [SerializeField] private bool rouletteSpinningValueOnRequest = true;

    [Header("Popup Test (Optional)")]
    [SerializeField] private Sprite testIcon;
    [SerializeField] private ItemGrade testGrade = ItemGrade.Grade2;
    [SerializeField] private int testIndex = 0;
    [SerializeField] private string testDescription = "TEST_ITEM";
    [SerializeField] private KeyCode keyPopup = KeyCode.F7;

    private void Update()
    {
        if (Input.GetKeyDown(keyRequestCinematic)) RequestCinematic();
        if (Input.GetKeyDown(keyDimCleared)) FireDimCleared();
        if (Input.GetKeyDown(keyImpact)) FireImpact();
        if (Input.GetKeyDown(keyFinished)) FireFinished();
        if (Input.GetKeyDown(keyTimeScale0)) SetTimeScale(0f);
        if (Input.GetKeyDown(keyTimeScale1)) SetTimeScale(timeScaleWhenResume);
        if (Input.GetKeyDown(keyPopup)) FirePopupTest();
    }

    private void OnGUI()
    {
        if (!showOnGui) return;

        GUILayout.BeginArea(new Rect(guiPos.x, guiPos.y, guiWidth, 600));
        GUILayout.Label("SecondDrawHalfCinematic Quick Tester");
        GUILayout.Space(6);

        if (GUILayout.Button($"Request Cinematic ({keyRequestCinematic})"))
        {
            RequestCinematic();
        }

        if (GUILayout.Button($"Fire DimCleared ({keyDimCleared})"))
        {
            FireDimCleared();
        }

        if (GUILayout.Button($"Fire Impact ({keyImpact})"))
        {
            FireImpact();
        }

        if (GUILayout.Button($"Fire Finished ({keyFinished})"))
        {
            FireFinished();
        }

        GUILayout.Space(8);
        if (GUILayout.Button($"TimeScale = 0 ({keyTimeScale0})"))
        {
            SetTimeScale(0f);
        }
        if (GUILayout.Button($"TimeScale = {timeScaleWhenResume:0.##} ({keyTimeScale1})"))
        {
            SetTimeScale(timeScaleWhenResume);
        }

        GUILayout.Space(8);
        if (GUILayout.Button($"Popup Test ({keyPopup})"))
        {
            FirePopupTest();
        }

        GUILayout.Space(8);
        GUILayout.Label($"Current Time.timeScale = {Time.timeScale:0.###}");
        GUILayout.EndArea();
    }

    private void SetTimeScale(float value)
    {
        Time.timeScale = Mathf.Max(0f, value);
    }

    private void RequestCinematic()
    {
        if (setTimeScale0BeforeRequest) SetTimeScale(0f);

        if (alsoFireRouletteSpinningForDim)
        {
            GameEvents.OnRouletteSpinning.OnNext(rouletteSpinningValueOnRequest);
        }

        GameEvents.OnSecondDrawHalfCinematicRequested.OnNext(R3.Unit.Default);
    }

    private void FireDimCleared()
    {
        GameEvents.OnBackgroundDimCleared.OnNext(R3.Unit.Default);
    }

    private void FireImpact()
    {
        GameEvents.OnSecondDrawHalfCinematicImpact.OnNext(R3.Unit.Default);
    }

    private void FireFinished()
    {
        GameEvents.OnSecondDrawHalfCinematicFinished.OnNext(R3.Unit.Default);
    }

    private void FirePopupTest()
    {
        GameEvents.OnItemAcquired.OnNext(new ItemAcquiredData(testIndex, testGrade, testIcon, testDescription));
    }
}

