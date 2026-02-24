#if USE_FMOD
using System.Reflection;
using FMODUnity;
using UnityEditor;
using UnityEngine;

namespace NightWish.CustomToolbar
{
    /// <summary>
    /// FMOD 디버그 오버레이를 제어하는 툴바 확장
    /// </summary>
    public class FMODDebugToolbar : ILeftToolbarExtension
    {
        private static FMODDebugToolbar _instance;
        public static FMODDebugToolbar Instance => _instance ??= new FMODDebugToolbar();

        public void OnToolbarGUI()
        {
            GUILayout.Space(ToolbarStyleConstants.GroupSpacing);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            
            EditorGUILayout.LabelField(
                LocalizationHelper.Common.FMODDebug, 
                ToolbarStyleConstants.ToolbarLabelStyle,
                GUILayout.Width(ToolbarStyleConstants.DefaultLabelWidth)
            );
            
            GUILayout.Space(ToolbarStyleConstants.HorizontalPadding);
            
            bool currentDebugEnabled = GetDebugOverlay() == TriStateBool.Enabled;
            bool debugToggle = EditorGUILayout.Toggle(
                currentDebugEnabled, 
                GUILayout.Width(ToolbarStyleConstants.ToggleWidth)
            );
            
            TriStateBool nextOverlayState = debugToggle ? TriStateBool.Enabled : TriStateBool.Disabled;
            SetDebugOverlay(nextOverlayState);
            
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(ToolbarStyleConstants.ElementSpacing);
        }

        private void SetDebugOverlay(TriStateBool value)
        {
            try
            {
                var overlayProperty = FMODUnity.Settings.Instance.PlayInEditorPlatform.GetType()
                    .GetField("Properties", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var overlayValue = overlayProperty?.GetValue(FMODUnity.Settings.Instance.PlayInEditorPlatform);

                var overlayFieldProperty = overlayValue?.GetType()
                    .GetField("Overlay", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var overlayFieldValue = overlayFieldProperty?.GetValue(overlayValue);

                var valueProperty = overlayFieldValue?.GetType()
                    .GetField("Value", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                valueProperty?.SetValue(overlayFieldValue, value);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"FMOD Debug Overlay 설정 중 오류 발생: {ex.Message}");
            }
        }

        private TriStateBool GetDebugOverlay()
        {
            try
            {
                return FMODUnity.Settings.Instance.PlayInEditorPlatform.Overlay;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"FMOD Debug Overlay 상태 확인 중 오류 발생: {ex.Message}");
                return TriStateBool.Disabled;
            }
        }
    }
}
#endif