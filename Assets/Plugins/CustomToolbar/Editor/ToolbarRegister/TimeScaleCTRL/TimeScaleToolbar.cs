using UnityEditor;
using UnityEngine;

namespace NightWish.CustomToolbar
{
    /// <summary>
    /// 시간 배율을 조절하는 툴바 확장
    /// </summary>
    public class TimeScaleToolbar : ILeftToolbarExtension
    {
        private static TimeScaleToolbar _instance;
        public static TimeScaleToolbar Instance => _instance ??= new TimeScaleToolbar();

        private const float MinTimeScale = 0f;
        private const float MaxTimeScale = 5f;

        public void OnToolbarGUI()
        {
            GUILayout.Space(ToolbarStyleConstants.GroupSpacing);

            // "Time" 슬라이더 전체 블록의 너비 계산
            float totalWidth =
                ToolbarStyleConstants.SmallLabelWidth +
                ToolbarStyleConstants.LabelSliderGap +
                ToolbarStyleConstants.SliderWidth +
                ToolbarStyleConstants.SliderValueGap +
                ToolbarStyleConstants.ValueDisplayWidth;

            // 고정 크기 Rect를 얻어서, 그 안에 BG + 레이블 + 슬라이더 + 값 표시를 수동으로 배치
            Rect blockRect = GUILayoutUtility.GetRect(
                totalWidth,
                22f,
                ToolbarStyleConstants.IconOnlyToolbarButtonStyle
            );

            bool isHover = blockRect.Contains(Event.current.mousePosition);
            bool isPressed = Event.current.type == EventType.MouseDown && isHover;

            // BG: Time 블록 전용 인셋 + BG 자체 오프셋을 적용해 위치/폭을 조절
            float bgY = blockRect.y +
                        (blockRect.height - ToolbarStyleConstants.IconBackgroundHeight) * 0.5f +
                        ToolbarStyleConstants.IconBackgroundOffsetY +
                        ToolbarStyleConstants.TimeBlockBgOffsetY;
            float bgX = blockRect.x +
                        ToolbarStyleConstants.TimeBlockBgInsetLeft +
                        ToolbarStyleConstants.TimeBlockBgOffsetX;
            float bgWidth = totalWidth - ToolbarStyleConstants.TimeBlockBgInsetLeft - ToolbarStyleConstants.TimeBlockBgInsetRight;
            var bgRect = new Rect(
                bgX,
                bgY,
                bgWidth,
                ToolbarStyleConstants.IconBackgroundHeight
            );

            Color bgColor = ToolbarStyleConstants.GetButtonBackgroundColor(false, isHover, isPressed);
            EditorGUI.DrawRect(bgRect, bgColor);

            // 컨텐츠 배치 시작 X/Y (BG 기준)
            float x = bgRect.x;
            float y = blockRect.y;

            // 1) "Time" 라벨
            Rect labelRect = new Rect(
                x + ToolbarStyleConstants.TimeLabelInsetX,
                y + ToolbarStyleConstants.TimeLabelOffsetY,
                ToolbarStyleConstants.SmallLabelWidth,
                blockRect.height
            );
            GUI.Label(labelRect, LocalizationHelper.Common.SpeedControl, ToolbarStyleConstants.ToolbarLabelStyle);

            x += ToolbarStyleConstants.SmallLabelWidth + ToolbarStyleConstants.LabelSliderGap;

            // 2) 슬라이더
            float sliderHeight = 14f;
            Rect sliderRect = new Rect(
                x,
                y + (blockRect.height - sliderHeight) * 0.5f + ToolbarStyleConstants.TimeSliderOffsetY,
                ToolbarStyleConstants.SliderWidth,
                sliderHeight
            );

            Time.timeScale = GUI.HorizontalSlider(
                sliderRect,
                Time.timeScale,
                MinTimeScale,
                MaxTimeScale
            );

            x += ToolbarStyleConstants.SliderWidth + ToolbarStyleConstants.SliderValueGap;

            // 3) 값 표시
            Rect valueRect = new Rect(
                x,
                y + ToolbarStyleConstants.TimeValueOffsetY,
                ToolbarStyleConstants.ValueDisplayWidth,
                blockRect.height
            );
            GUI.Label(
                valueRect,
                Time.timeScale.ToString("0.00"),
                ToolbarStyleConstants.ToolbarLabelStyle
            );
        }
    }
}