using UnityEditor;
using UnityEngine;

namespace NightWish.CustomToolbar
{
    /// <summary>
    /// 툴바 확장 기능 설정을 위한 에디터 윈도우
    /// </summary>
    public class ToolbarSettingsWindow : EditorWindow
    {
        private ToolbarSettings _settings;
        private Vector2 _scrollPosition;

        [MenuItem("Tools/CustomToolbar/Settings", priority = 1)]
        public static void ShowWindow()
        {
            ToolbarSettingsWindow window = GetWindow<ToolbarSettingsWindow>("커스텀 툴바 설정");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            _settings = ToolbarSettings.LoadFromFile();
        }

        private void OnGUI()
        {
            if (_settings == null)
            {
                LoadSettings();
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawTitle();
            DrawLeftToolbarSettings();
            DrawRightToolbarSettings();
            DrawStyleSettings();
            DrawActionButtons();
            DrawInfo();

            EditorGUILayout.EndScrollView();
        }

        private void DrawTitle()
        {
            EditorGUILayout.Space(10);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("커스텀 툴바 설정", titleStyle);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "각 툴바 확장 기능을 개별적으로 활성화/비활성화할 수 있습니다.\n" +
                "설정은 자동으로 JSON 파일에 저장됩니다.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);
        }

        private void DrawLeftToolbarSettings()
        {
            EditorGUILayout.LabelField("왼쪽 툴바 확장", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            _settings.enableEnterPlayModeOption = EditorGUILayout.Toggle(
                new GUIContent("Enter Play Mode 옵션", "플레이 모드 진입 옵션을 제어하는 툴바"),
                _settings.enableEnterPlayModeOption
            );

            _settings.enableFMODDebug = EditorGUILayout.Toggle(
                new GUIContent("FMOD 디버그", "FMOD 오디오 시스템 디버그 오버레이를 제어하는 툴바"),
                _settings.enableFMODDebug
            );

            _settings.enableFrameRate = EditorGUILayout.Toggle(
                new GUIContent("FPS 표시 및 설정", "실시간 FPS 표시 및 프리셋 FPS 설정 드롭다운"),
                _settings.enableFrameRate
            );

            _settings.enableTimeScale = EditorGUILayout.Toggle(
                new GUIContent("시간 배율 조절", "게임 속도를 조절하는 슬라이더 툴바"),
                _settings.enableTimeScale
            );

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);
        }

        private void DrawRightToolbarSettings()
        {
            EditorGUILayout.LabelField("오른쪽 툴바 확장", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            _settings.enableRestartScene = EditorGUILayout.Toggle(
                new GUIContent("씬 재시작", "현재 씬을 재시작하는 버튼"),
                _settings.enableRestartScene
            );

            _settings.enableSceneSwitcher = EditorGUILayout.Toggle(
                new GUIContent("씬 전환", "다른 씬으로 전환할 수 있는 드롭다운 메뉴"),
                _settings.enableSceneSwitcher
            );

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);
        }

        private void DrawStyleSettings()
        {
            EditorGUILayout.LabelField("스타일 설정", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            _settings.useKoreanLanguage = EditorGUILayout.Toggle(
                new GUIContent("한국어 사용", "UI 텍스트를 한국어로 표시합니다"),
                _settings.useKoreanLanguage
            );

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(15);
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("설정 저장 및 적용", GUILayout.Height(30)))
            {
                SaveAndApplySettings();
            }

            if (GUILayout.Button("기본값으로 리셋", GUILayout.Height(30)))
            {
                ResetToDefaults();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        private void DrawInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("정보", EditorStyles.boldLabel);

            string settingsPath = System.IO.Path.Combine(Application.dataPath, "..", "UserSettings", "CustomToolbarSettings.json");
            bool fileExists = ToolbarSettings.SettingsFileExists();

            EditorGUILayout.LabelField("설정 파일 위치:", EditorStyles.miniLabel);
            EditorGUILayout.SelectableLabel(settingsPath, EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));

            EditorGUILayout.LabelField($"설정 파일 존재: {(fileExists ? "예" : "아니오")}", EditorStyles.miniLabel);

            if (GUILayout.Button("설정 파일 위치 열기", GUILayout.Width(150)))
            {
                string directory = System.IO.Path.GetDirectoryName(settingsPath);
                if (System.IO.Directory.Exists(directory))
                {
                    System.Diagnostics.Process.Start("explorer.exe", directory);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void SaveAndApplySettings()
        {
            _settings.SaveToFile();
            _settings.ApplyToRegistry();

            ShowNotification(new GUIContent("설정이 저장되고 적용되었습니다!"));
        }

        private void ResetToDefaults()
        {
            if (EditorUtility.DisplayDialog(
                "기본값으로 리셋",
                "모든 설정을 기본값으로 되돌리시겠습니까?",
                "예",
                "아니오"))
            {
                _settings.ResetToDefaults();
                _settings.SaveToFile();
                _settings.ApplyToRegistry();

                ShowNotification(new GUIContent("설정이 기본값으로 리셋되었습니다!"));
            }
        }
    }
}


