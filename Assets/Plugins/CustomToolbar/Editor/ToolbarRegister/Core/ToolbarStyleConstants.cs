using UnityEditor;
using UnityEngine;

namespace NightWish.CustomToolbar
{
    /// <summary>
    /// 툴바 관련 스타일과 상수 정의
    /// </summary>
    public static class ToolbarStyleConstants
    {
        // 레이아웃 상수 - UX 개선된 마진값 (일부는 설정값으로 동적으로 변경 가능)
        private static float _elementSpacing = 8f;        // 요소 간 기본 간격
        private static float _groupSpacing = 15f;         // 그룹 간 간격

        public static float ElementSpacing => _elementSpacing;
        public static float GroupSpacing => _groupSpacing;
        public const float DefaultLabelWidth = 85f;
        public const float SmallLabelWidth = 70f;
        public const float LargeLabelWidth = 100f;
        public const float DefaultButtonWidth = 120f;
        // 주요 액션 버튼들(EnterPlayMode, 씬 선택 이동 등)에 통일해 사용할 너비
        public const float PrimaryButtonWidth = 35f;
        public const float SmallButtonWidth = 55f;
        public const float ToggleWidth = 18f;
        public const float SliderWidth = 120f;
        public const float ValueDisplayWidth = 40f;
        // Time Scale 블록 내부 간격
        public const float LabelSliderGap = -36f;
        public const float SliderValueGap = 2f;
        
        // 패딩 상수
        public const float HorizontalPadding = 4f;

        // 아이콘 / 커스텀 버튼 BG 렌더링 관련 기본 값 (필요시 코드 레벨에서 보정 가능)
        public const float IconSize = 12f;          // 아이콘 한 변의 기본 픽셀 크기
        public const float IconOffsetX = 0f;        // BG 중앙 기준 X 오프셋
        public const float IconOffsetY = 0f;        // BG 중앙 기준 Y 오프셋

        // 커스텀 버튼용 BG(버튼 역할이 아닌 단순 배경) 사이즈 및 위치 오프셋
        public const float IconBackgroundWidth = 31f;
        public const float IconBackgroundHeight = 18f;
        public const float IconBackgroundOffsetX = 0f; // 버튼(22px 높이) 중앙 기준 BG X 오프셋
        public const float IconBackgroundOffsetY = -1f; // 버튼(22px 높이) 중앙 기준 BG Y 오프셋

        // Time Scale 블록 BG 좌우 인셋 (BG 폭/정렬 튜닝용)
        public const float TimeBlockBgInsetLeft = -0f;
        public const float TimeBlockBgInsetRight = 10f;
        // Time Scale 블록 BG 자체의 오프셋 (툴바 기준 위치 튜닝용)
        public const float TimeBlockBgOffsetX = 2f;
        public const float TimeBlockBgOffsetY = 0f;

        // 좌/우 커스텀 버튼 전체 블록의 X 오프셋 (인터랙션 영역 + BG + 아이콘을 한 번에 이동)
        public const float LeftButtonOffsetX = 14f;   // EnterPlayMode 버튼용
        public const float RightButtonOffsetX = -14f; // 우측 커스텀 세트(재시작 + 씬 전환) 기준 오프셋
        // 우측 세트 내에서 Restart 오른쪽에 SceneSwitcher 를 얼마나 띄울지에 대한 추가 오프셋
        public const float SceneSwitcherOffsetFromRestartX = -19.2f; // 기본: 딱 붙게

        // GUI 스타일
        private static GUIStyle _commandButtonStyle;
        private static GUIStyle _toolbarLabelStyle;
        private static GUIStyle _iconOnlyToolbarButtonStyle;

        // Time Scale 블록 내 요소들의 세로 오프셋 (상하 마진 튜닝용)
        public const float TimeLabelOffsetY = -1f;
        public const float TimeSliderOffsetY = -2f;
        public const float TimeValueOffsetY = -1f;
        // Time 텍스트와 BG 좌측 사이의 가로 마진
        public const float TimeLabelInsetX = 2f;

        public static GUIStyle CommandButtonStyle
        {
            get
            {
                if (_commandButtonStyle == null)
                {
                    // Unity 기본 툴바 버튼과 최대한 비슷한 외형을 사용
                    _commandButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        imagePosition = ImagePosition.ImageLeft,
                        fontStyle = FontStyle.Normal
                    };
                }
                return _commandButtonStyle;
            }
        }

        public static GUIStyle ToolbarLabelStyle
        {
            get
            {
                if (_toolbarLabelStyle == null)
                {
                    _toolbarLabelStyle = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontStyle = FontStyle.Normal
                    };
                }
                return _toolbarLabelStyle;
            }
        }

        /// <summary>
        /// 배경(프레임)을 그리지 않고, 사용자 정의 아이콘 텍스처만 수동으로 그릴 때 사용하는 툴바 버튼 스타일
        /// </summary>
        public static GUIStyle IconOnlyToolbarButtonStyle
        {
            get
            {
                if (_iconOnlyToolbarButtonStyle == null)
                {
                    // 완전히 투명한 버튼(배경 없음)을 만들기 위해 GUIStyle.none 기반으로 생성
                    _iconOnlyToolbarButtonStyle = new GUIStyle(GUIStyle.none)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        // 툴바 영역(22px) 안에서 정확히 중앙 정렬되도록 여백/패딩 제거
                        margin = new RectOffset(0, 0, 0, 0),
                        padding = new RectOffset(0, 0, 0, 0),
                        fixedHeight = 22f,
                        fixedWidth = PrimaryButtonWidth
                    };

                    // 인터렉션 영역이 레이아웃에 따라 늘어나지 않도록 고정 너비만 사용
                    _iconOnlyToolbarButtonStyle.stretchWidth = false;
                }

                return _iconOnlyToolbarButtonStyle;
            }
        }

        /// <summary>
        /// 커스텀 툴바 버튼 BG 색상을 상태(활성/호버/프레스)에 따라 반환합니다.
        /// Unity 기본 Play/Pause 블록과 비슷한 느낌이 나도록 톤을 맞춥니다.
        /// </summary>
        public static Color GetButtonBackgroundColor(bool isOn, bool isHover, bool isPressed)
        {
            bool pro = EditorGUIUtility.isProSkin;

            if (!isOn)
            {
                if (isPressed)
                    return pro ? new Color(0.20f, 0.20f, 0.20f, 1f) : new Color(0.65f, 0.65f, 0.65f, 1f);
                if (isHover)
                    return pro ? new Color(0.2588235f, 0.2588235f, 0.2588235f, 1f) : new Color(0.78f, 0.78f, 0.78f, 1f);

                // 기본 비활성 배경
                return pro ? new Color(0.2196079f, 0.2196079f, 0.2196079f, 1f) : new Color(0.72f, 0.72f, 0.72f, 1f);
            }
            else
            {
                // ON 상태일 때는 약간 더 밝고 강조된 톤 사용
                if (isPressed)
                    return pro ? new Color(0.32f, 0.32f, 0.32f, 1f) : new Color(0.82f, 0.82f, 0.82f, 1f);
                if (isHover)
                    return pro ? new Color(0.36f, 0.36f, 0.36f, 1f) : new Color(0.86f, 0.86f, 0.86f, 1f);

                return pro ? new Color(0.30f, 0.30f, 0.30f, 1f) : new Color(0.80f, 0.80f, 0.80f, 1f);
            }
        }

        /// <summary>
        /// Enter Play Mode 토글 전용 BG 색상.
        /// 기본 버튼보다 눈에 잘 띄도록 붉은 계통 강조 색을 사용합니다.
        /// </summary>
        public static Color GetEnterPlayBackgroundColor(bool isOn, bool isHover, bool isPressed)
        {
            bool pro = EditorGUIUtility.isProSkin;

            if (!isOn)
            {
                // 기본 상태는 일반 버튼과 동일한 그레이,
                // 마우스를 올렸을 때/누를 때만 붉은 기가 도는 톤으로 살짝 강조
                if (isPressed)
                    return pro ? new Color(0.52f, 0.18f, 0.18f, 1f) : new Color(0.92f, 0.55f, 0.55f, 1f);
                if (isHover)
                    return pro ? new Color(0.44f, 0.16f, 0.16f, 1f) : new Color(0.86f, 0.50f, 0.50f, 1f);

                // Hover/Pressed 가 아닐 때는 일반 버튼과 동일한 기본 BG 색
                return GetButtonBackgroundColor(false, false, false);
            }
            else
            {
                // 켜져 있을 때는 한눈에 보이도록 훨씬 밝고 선명한 레드 계열
                if (isPressed)
                    return pro ? new Color(0.90f, 0.30f, 0.30f, 1f) : new Color(1.00f, 0.65f, 0.65f, 1f);
                if (isHover)
                    return pro ? new Color(0.96f, 0.34f, 0.34f, 1f) : new Color(1.00f, 0.72f, 0.72f, 1f);

                // 기본 ON 상태는 Play 버튼보다도 한 단계 더 눈에 띄는 레드
                return pro ? new Color(0.86f, 0.26f, 0.26f, 1f) : new Color(0.98f, 0.60f, 0.60f, 1f);
            }
        }

        /// <summary>
        /// 툴바 아이콘 색상(텍스처 틴트)을 상태(활성/호버/토글/프레스)에 따라 반환합니다.
        /// 기본 툴바 아이콘처럼 비활성일 때는 약간 회색, Hover/On 일 때는 더 밝게 보이도록 조정합니다.
        /// </summary>
        public static Color GetIconColor(bool isEnabled, bool isHover, bool isOn, bool isPressed)
        {
            bool pro = EditorGUIUtility.isProSkin;

            // Pro 스킨 기준 값
            Color baseColor = pro ? new Color(0.7686275f, 0.7686275f, 0.7686275f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
            Color disabledColor = pro ? new Color(0.6156863f, 0.6156863f, 0.6156863f, 1f) : new Color(0.615f, 0.615f, 0.615f, 1f);
            Color hoverColor = pro ? new Color(0.7686275f, 0.7686275f, 0.7686275f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
            Color onColor = pro ? Color.white : new Color(0.12f, 0.12f, 0.12f, 1f);
            Color pressedColor = pro ? new Color(0.92f, 0.92f, 0.92f, 1f) : new Color(0.20f, 0.20f, 0.20f, 1f);

            if (!isEnabled)
                return disabledColor;

            if (isPressed)
                return pressedColor;

            if (isOn)
                return onColor;

            if (isHover)
                return hoverColor;

            return baseColor;
        }
    }
}
