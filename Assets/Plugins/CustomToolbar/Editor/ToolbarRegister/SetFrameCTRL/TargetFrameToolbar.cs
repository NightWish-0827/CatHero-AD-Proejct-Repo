using UnityEditor;
using UnityEngine;

namespace NightWish.CustomToolbar
{
    /// <summary>
    /// 타겟 프레임 레이트를 설정하는 툴바 확장
    /// </summary>
    public class TargetFrameToolbar : ILeftToolbarExtension
    {
        private static TargetFrameToolbar _instance;
        public static TargetFrameToolbar Instance => _instance ??= new TargetFrameToolbar();

        public void OnToolbarGUI()
        {
            GUILayout.Space(ToolbarStyleConstants.GroupSpacing);

            GUILayout.Label(
                LocalizationHelper.Common.TargetFrame, 
                ToolbarStyleConstants.ToolbarLabelStyle,
                GUILayout.Width(ToolbarStyleConstants.DefaultLabelWidth)
            );
            
            GUILayout.Space(ToolbarStyleConstants.HorizontalPadding);
            
            Application.targetFrameRate = EditorGUILayout.IntField(
                Application.targetFrameRate, 
                GUILayout.Width(ToolbarStyleConstants.SmallButtonWidth)
            );
        }
    }
}