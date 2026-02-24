using UnityEditor;
using UnityEngine;

namespace NightWish.CustomToolbar
{
    /// <summary>
    /// Enter Play Mode 옵션을 제어하는 툴바 확장
    /// </summary>
    public class EnterPlayModeOptionToolbar : ILeftToolbarExtension
    {
        private static EnterPlayModeOptionToolbar _instance;
        public static EnterPlayModeOptionToolbar Instance => _instance ??= new EnterPlayModeOptionToolbar();

        // 기본 아이콘 파일 이름 (폴더 구조가 변경되더라도 파일명 기준으로 검색)
        private const string IconFileName = "enter_playmode_icon.png";
        private static Texture2D _iconTexture;
        // Tooltip 텍스트는 LocalizationHelper 를 통해 언어에 따라 자동으로 변경
        private static readonly GUIContent _tooltipContent =
            new GUIContent(string.Empty, LocalizationHelper.Common.EnterPlayMode);

        private static void EnsureIconLoaded()
        {
            if (_iconTexture != null)
                return;

            // 기존 레거시 상대 경로: Extension/CustomToolbar/Editor/Icons/...
            _iconTexture = ToolbarIconLoader.LoadIcon(IconFileName, "Extension/CustomToolbar/Editor/Icons/" + IconFileName);
        }

        public void OnToolbarGUI()
        {
            bool currentOption = EditorSettings.enterPlayModeOptionsEnabled;

            GUILayout.Space(ToolbarStyleConstants.ElementSpacing);
            bool isEnabled = !EditorApplication.isPlaying;
            EditorGUI.BeginDisabledGroup(!isEnabled);

            EnsureIconLoaded();

            // 완전히 투명한 버튼 Rect 를 직접 만들고, X 오프셋까지 적용한 뒤 토글을 그린다
            Rect buttonRect = GUILayoutUtility.GetRect(
                ToolbarStyleConstants.PrimaryButtonWidth,
                22f,
                ToolbarStyleConstants.IconOnlyToolbarButtonStyle
            );
            buttonRect.x += ToolbarStyleConstants.LeftButtonOffsetX;

            bool nextOption = GUI.Toggle(buttonRect, currentOption, _tooltipContent, ToolbarStyleConstants.IconOnlyToolbarButtonStyle);

            // 마지막으로 그려진 버튼 Rect 위에 BG(31x18)를 먼저 그리고, 그 위에 아이콘을 중앙 정렬로 오버레이
            if (_iconTexture != null)
            {
                Rect rect = buttonRect;
                
                // 현재 상태/마우스 상태에 따라 BG 색상 및 아이콘 색상 결정
                bool isHover = rect.Contains(Event.current.mousePosition);
                bool isPressed = GUIUtility.hotControl != 0 && isHover;
                Color bgColor = ToolbarStyleConstants.GetEnterPlayBackgroundColor(nextOption, isHover, isPressed);

                // 1) BG 네모(31x18)를 세로 축 중앙에 렌더 (버튼 역할 X, 단순 배경)
                float bgW = ToolbarStyleConstants.IconBackgroundWidth;
                float bgH = ToolbarStyleConstants.IconBackgroundHeight;
                float bgX = rect.x + (rect.width - bgW) * 0.5f + ToolbarStyleConstants.IconBackgroundOffsetX;
                float bgY = rect.y + (rect.height - bgH) * 0.5f + ToolbarStyleConstants.IconBackgroundOffsetY;
                var bgRect = new Rect(bgX, bgY, bgW, bgH);

                EditorGUI.DrawRect(bgRect, bgColor);

                // 2) BG 위에 아이콘 렌더 (아이콘이 더 위 레이어에 위치) - BG를 기준으로 배치
                float size = ToolbarStyleConstants.IconSize;
                float x = bgRect.x + (bgRect.width - size) * 0.5f + ToolbarStyleConstants.IconOffsetX;
                float y = bgRect.y + (bgRect.height - size) * 0.5f + ToolbarStyleConstants.IconOffsetY;

                Color prevColor = GUI.color;
                Color iconColor = ToolbarStyleConstants.GetIconColor(isEnabled, isHover, nextOption, isPressed);
                GUI.color = iconColor;
                GUI.DrawTexture(new Rect(x, y, size, size), _iconTexture, ScaleMode.ScaleToFit, true);
                GUI.color = prevColor;
            }

            if (nextOption != currentOption)
            {
                EditorSettings.enterPlayModeOptionsEnabled = nextOption;
            }

            EditorGUI.EndDisabledGroup();
            GUILayout.Space(ToolbarStyleConstants.ElementSpacing);
        }
    }
}