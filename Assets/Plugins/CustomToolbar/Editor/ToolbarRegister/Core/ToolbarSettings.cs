using System;
using System.IO;
using UnityEngine;

namespace NightWish.CustomToolbar
{
    /// <summary>
    /// 툴바 설정을 JSON으로 저장/로드하는 데이터 클래스
    /// </summary>
    [Serializable]
    public class ToolbarSettings
    {
        [Header("왼쪽 툴바 확장 설정")]
        public bool enableEnterPlayModeOption = true;
        public bool enableFMODDebug = true;
        public bool enableFrameRate = true;
        public bool enableTimeScale = true;
        public bool enableTargetFrame = true;

        [Header("오른쪽 툴바 확장 설정")]
        public bool enableRestartScene = true;
        public bool enableSceneSwitcher = true;

        [Header("스타일 설정")]
        public bool useKoreanLanguage = true;

        private static string SettingsFilePath => Path.Combine(Application.dataPath, "..", "UserSettings", "CustomToolbarSettings.json");

        /// <summary>
        /// 설정을 JSON 파일로 저장합니다.
        /// </summary>
        public void SaveToFile()
        {
            try
            {
                string directoryPath = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string json = JsonUtility.ToJson(this, true);
                File.WriteAllText(SettingsFilePath, json);
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"툴바 설정 저장 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// JSON 파일에서 설정을 로드합니다.
        /// </summary>
        /// <returns>로드된 설정 객체</returns>
        public static ToolbarSettings LoadFromFile()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    ToolbarSettings settings = JsonUtility.FromJson<ToolbarSettings>(json);
                    return settings;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"툴바 설정 로드 중 오류 발생: {ex.Message}");
            }

            return new ToolbarSettings();
        }

        /// <summary>
        /// 설정 파일이 존재하는지 확인합니다.
        /// </summary>
        /// <returns>설정 파일 존재 여부</returns>
        public static bool SettingsFileExists()
        {
            return File.Exists(SettingsFilePath);
        }

        /// <summary>
        /// 설정을 확장 레지스트리에 적용합니다.
        /// </summary>
        public void ApplyToRegistry()
        {
            ToolbarExtensionRegistry.SetExtensionEnabled(typeof(EnterPlayModeOptionToolbar), enableEnterPlayModeOption);
#if USE_FMOD
            ToolbarExtensionRegistry.SetExtensionEnabled(typeof(FMODDebugToolbar), enableFMODDebug);
#endif
            ToolbarExtensionRegistry.SetExtensionEnabled(typeof(FrameRateToolbar), enableFrameRate);
            ToolbarExtensionRegistry.SetExtensionEnabled(typeof(TimeScaleToolbar), enableTimeScale);
            // TargetFrameToolbar 기능은 FrameRateToolbar 드롭다운으로 통합되었으므로 더 이상 사용하지 않습니다.
            ToolbarExtensionRegistry.SetExtensionEnabled(typeof(RestartSceneToolbar), enableRestartScene);
            ToolbarExtensionRegistry.SetExtensionEnabled(typeof(SceneSwitcherToolbar), enableSceneSwitcher);

            // 언어 관련 에디터 전역 설정 반영
            LocalizationHelper.ApplyLanguageSetting(useKoreanLanguage);
        }

        /// <summary>
        /// 기본값으로 리셋합니다.
        /// </summary>
        public void ResetToDefaults()
        {
            enableEnterPlayModeOption = true;
            enableFMODDebug = true;
            enableFrameRate = true;
            enableTimeScale = true;
            enableTargetFrame = true; // 유지하지만 현재는 FrameRateToolbar에 통합됨
            enableRestartScene = true;
            enableSceneSwitcher = true;
            useKoreanLanguage = true;
        }
    }
}