using UnityEditor;
using UnityToolbarExtender;
using UnityEngine;

namespace NightWish.CustomToolbar
{
    /// <summary>
    /// 커스텀 툴바 확장 기능들을 등록하고 관리하는 메인 클래스
    /// </summary>
    [InitializeOnLoad]
    public class ToolbarRegister
    {
        private static ToolbarSettings _currentSettings;

        static ToolbarRegister()
        {
            InitializeExtensions();
            LoadSettings();

            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUILeft);
            ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUIRight);

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void LoadSettings()
        {
            _currentSettings = ToolbarSettings.LoadFromFile();
            _currentSettings.ApplyToRegistry();
        }

        private static void InitializeExtensions()
        {
#if USE_FMOD
            ToolbarExtensionRegistry.RegisterLeftExtension(FMODDebugToolbar.Instance);
#endif
            ToolbarExtensionRegistry.RegisterLeftExtension(FrameRateToolbar.Instance);
            ToolbarExtensionRegistry.RegisterLeftExtension(TimeScaleToolbar.Instance);
            ToolbarExtensionRegistry.RegisterLeftExtension(EnterPlayModeOptionToolbar.Instance);

            ToolbarExtensionRegistry.RegisterRightExtension(RestartSceneToolbar.Instance);
            ToolbarExtensionRegistry.RegisterRightExtension(SceneSwitcherToolbar.Instance);
        }

        private static void OnToolbarGUILeft()
        {
            foreach (var extension in ToolbarExtensionRegistry.LeftExtensions)
            {
                if (extension is EnterPlayModeOptionToolbar)
                    continue;

                if (!ToolbarExtensionRegistry.IsExtensionEnabled(extension.GetType()))
                    continue;

                try
                {
                    extension.OnToolbarGUI();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"왼쪽 툴바 확장 {extension.GetType().Name} 렌더링 중 오류: {ex.Message}");
                }
            }

            GUILayout.FlexibleSpace();

            if (ToolbarExtensionRegistry.IsExtensionEnabled(typeof(EnterPlayModeOptionToolbar)))
            {
                EnterPlayModeOptionToolbar.Instance.OnToolbarGUI();
            }
        }

        private static void OnToolbarGUIRight()
        {
            if (ToolbarExtensionRegistry.IsExtensionEnabled(typeof(RestartSceneToolbar)))
            {
                RestartSceneToolbar.Instance.OnToolbarGUI();
            }

            if (ToolbarExtensionRegistry.IsExtensionEnabled(typeof(SceneSwitcherToolbar)))
            {
                SceneSwitcherToolbar.Instance.OnToolbarGUI();
            }

            foreach (var extension in ToolbarExtensionRegistry.RightExtensions)
            {
                if (extension is RestartSceneToolbar || extension is SceneSwitcherToolbar)
                    continue;

                if (!ToolbarExtensionRegistry.IsExtensionEnabled(extension.GetType()))
                    continue;

                try
                {
                    extension.OnToolbarGUI();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"오른쪽 툴바 확장 {extension.GetType().Name} 렌더링 중 오류: {ex.Message}");
                }
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            if (EditorSettings.enterPlayModeOptionsEnabled)
            {
                string warning = LocalizationHelper.Messages.EnterPlayModeLifecycleWarning;
                // 경고 아이콘 대신 일반 로그 아이콘 + 주황색 텍스트로 표시
                Debug.Log(LocalizationHelper.LogTags.Warning(warning));
            }
        }

        [MenuItem("Tools/CustomToolbar/Reload Settings", priority = 2)]
        public static void ReloadSettings()
        {
            LoadSettings();
            Debug.Log("커스텀 툴바 설정이 다시 로드되었습니다.");
        }
    }
}


