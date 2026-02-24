using UnityEngine;

namespace NightWish.CustomToolbar
{
    /// <summary>
    /// 툴바 확장 기능들의 설정을 관리하는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "ToolbarExtensionConfig", menuName = "CustomToolbar/Extension Config")]
    public class ToolbarExtensionConfig : ScriptableObject
    {
        [Header("왼쪽 툴바 확장 설정")]
        [SerializeField] private bool _enableEnterPlayModeOption = true;
        [SerializeField] private bool _enableFMODDebug = true;
        [SerializeField] private bool _enableFrameRate = true;
        [SerializeField] private bool _enableTimeScale = true;
        [SerializeField] private bool _enableTargetFrame = true;

        [Header("오른쪽 툴바 확장 설정")]
        [SerializeField] private bool _enableRestartScene = true;
        [SerializeField] private bool _enableSceneSwitcher = true;

        [Header("스타일 설정")]
        [SerializeField] private bool _useKoreanLanguage = true;
        [SerializeField] private float _customSpacing = 2f;

        // Properties
        public bool EnableEnterPlayModeOption => _enableEnterPlayModeOption;
        public bool EnableFMODDebug => _enableFMODDebug;
        public bool EnableFrameRate => _enableFrameRate;
        public bool EnableTimeScale => _enableTimeScale;
        public bool EnableTargetFrame => _enableTargetFrame;
        public bool EnableRestartScene => _enableRestartScene;
        public bool EnableSceneSwitcher => _enableSceneSwitcher;
        public bool UseKoreanLanguage => _useKoreanLanguage;
        public float CustomSpacing => _customSpacing;

        /// <summary>
        /// 기본 설정으로 초기화합니다.
        /// </summary>
        public void ResetToDefaults()
        {
            _enableEnterPlayModeOption = true;
            _enableFMODDebug = true;
            _enableFrameRate = true;
            _enableTimeScale = true;
            _enableTargetFrame = true;
            _enableRestartScene = true;
            _enableSceneSwitcher = true;
            _useKoreanLanguage = true;
            _customSpacing = 2f;
        }

        /// <summary>
        /// 설정에 따라 확장 기능들을 활성화/비활성화합니다.
        /// </summary>
        public void ApplySettings()
        {
            ToolbarExtensionRegistry.SetExtensionEnabled(typeof(EnterPlayModeOptionToolbar), _enableEnterPlayModeOption);
#if USE_FMOD
            ToolbarExtensionRegistry.SetExtensionEnabled(typeof(FMODDebugToolbar), _enableFMODDebug);
#endif
            ToolbarExtensionRegistry.SetExtensionEnabled(typeof(FrameRateToolbar), _enableFrameRate);
            ToolbarExtensionRegistry.SetExtensionEnabled(typeof(TimeScaleToolbar), _enableTimeScale);
            // TargetFrameToolbar 기능은 FrameRateToolbar 드롭다운으로 통합되었으므로 더 이상 사용하지 않습니다.
            ToolbarExtensionRegistry.SetExtensionEnabled(typeof(RestartSceneToolbar), _enableRestartScene);
            ToolbarExtensionRegistry.SetExtensionEnabled(typeof(SceneSwitcherToolbar), _enableSceneSwitcher);
        }
    }
}
