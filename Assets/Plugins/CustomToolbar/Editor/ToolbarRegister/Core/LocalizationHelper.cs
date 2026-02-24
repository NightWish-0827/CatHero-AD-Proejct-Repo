using UnityEngine;

namespace NightWish.CustomToolbar
{
    /// <summary>
    /// 다국어 지원을 위한 헬퍼 클래스
    /// </summary>
    public static class LocalizationHelper
    {
        // ToolbarSettings 에서 언어 설정을 강제로 지정할 수 있도록 하는 플래그
        private static bool? _forceKorean;

        public static bool IsKorean => _forceKorean ?? (Application.systemLanguage == SystemLanguage.Korean);

        // 공통 문구들
        public static string GetText(string englishText, string koreanText)
        {
            return IsKorean ? koreanText : englishText;
        }

        /// <summary>
        /// 에디터 설정(ToolbarSettings)의 언어 옵션을 적용합니다.
        /// </summary>
        /// <param name="useKorean">true면 한국어, false면 영어를 강제로 사용</param>
        public static void ApplyLanguageSetting(bool useKorean)
        {
            _forceKorean = useKorean;
        }

        // 자주 사용되는 문구들
        public static class Common
        {
            public static string PlayMode => GetText("Play Mode", "플레이 모드");
            public static string NotInPlayMode => GetText("Not currently in Play Mode", "현재 PlayMode가 아닙니다.");
            public static string FrameRate => GetText("Frame Rate", "프레임율");
            public static string TimeScale => GetText("Time Scale", "시간 배율");
            public static string TargetFrame => GetText("Target Frame", "프레임 목표치");
            public static string RestartScene => GetText("Restart Scene", "씬 재시작");
            public static string SceneSelect => GetText("Scene Select", "씬 선택 이동");
            public static string FMODDebug => GetText("FMOD Debug", "FMOD 디버그");
            public static string EnterPlayMode => GetText("Enter Play Mode", "Enter Play Mode");
            // Time 슬라이더용 라벨은 양쪽 모두 단순하게 "Time" 으로 표기
            public static string SpeedControl => GetText("TIME", "TIME");
        }

        public static class Messages
        {
            public static string SceneRestarted(string sceneName) => 
                GetText($"Scene <{sceneName}> has been restarted.", $"<{sceneName}> 재 시작 되었습니다.");
            
            public static string StartFromFirstScene => 
                GetText("Start from the first scene.", "최초 세팅 씬부터 게임을 진입합니다.");

            public static string EnterPlayModeLifecycleWarning =>
                GetText(
                    "Enter Play Mode Options is enabled. Because the normal scene lifecycle is changed, DI or service locators may not work correctly.",
                    "Enter Play Mode 옵션이 활성화되어 있습니다. 라이프사이클이 변경되어 DI나 서비스 로케이터가 정상 동작하지 않을 수 있습니다."
                );
        }

        public static class LogTags
        {
            public const string ModTag = "<color=yellow> CustomToolbar </color>";
            public const string Separator = "<color=white> : </color>";
            
            public static string Success(string message) => $"{ModTag} {Separator} <color=green> {message} </color>";
            public static string Warning(string message) => $"{ModTag} {Separator} <color=orange> {message} </color>";
            public static string Info(string message) => $"{ModTag} {Separator} <color=white> {message} </color>";
        }
    }
}
