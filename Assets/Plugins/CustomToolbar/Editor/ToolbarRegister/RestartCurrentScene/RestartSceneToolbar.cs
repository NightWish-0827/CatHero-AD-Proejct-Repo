using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NightWish.CustomToolbar
{
    /// <summary>
    /// 현재 씬을 재시작하는 툴바 확장
    /// </summary>
    public class RestartSceneToolbar : IRightToolbarExtension
    {
        private static RestartSceneToolbar _instance;
        public static RestartSceneToolbar Instance => _instance ??= new RestartSceneToolbar();

        // 재시작 버튼 전용 아이콘 파일 이름 (폴더 구조 변경 시에도 파일명으로 검색)
        private const string IconFileName = "restart_scene_icon.png";
        private static Texture2D _iconTexture;
        private static readonly GUIContent _tooltipContent =
            new GUIContent(string.Empty, LocalizationHelper.Common.RestartScene);

        private static void EnsureIconLoaded()
        {
            if (_iconTexture != null)
                return;

            _iconTexture = ToolbarIconLoader.LoadIcon(IconFileName, "Extension/CustomToolbar/Editor/Icons/" + IconFileName);
        }

        public void OnToolbarGUI()
        {
            GUILayout.Space(ToolbarStyleConstants.ElementSpacing);

            EnsureIconLoaded();

            Rect buttonRect = GUILayoutUtility.GetRect(
                ToolbarStyleConstants.PrimaryButtonWidth,
                22f,
                ToolbarStyleConstants.IconOnlyToolbarButtonStyle
            );
            buttonRect.x += ToolbarStyleConstants.RightButtonOffsetX;

            bool isPlayMode = EditorApplication.isPlaying;
            bool isHover = isPlayMode && buttonRect.Contains(Event.current.mousePosition);
            bool isPressed = false;

            EditorGUI.BeginDisabledGroup(!isPlayMode);
            if (GUI.Button(buttonRect, _tooltipContent, ToolbarStyleConstants.IconOnlyToolbarButtonStyle))
            {
                isPressed = true;
                RestartCurrentScene();
            }
            EditorGUI.EndDisabledGroup();

            if (_iconTexture != null)
            {
                Color bgColor;
                if (!isPlayMode)
                {
                    // 비활성 상태에서는 측정된 기본 Toolbar 비활성 BG 색상(0.1568628...)을 사용
                    bgColor = new Color(0.1568628f, 0.1568628f, 0.1568628f, 1f);
                }
                else
                {
                    bgColor = ToolbarStyleConstants.GetButtonBackgroundColor(false, isHover, isPressed);
                }

                float bgW = ToolbarStyleConstants.IconBackgroundWidth;
                float bgH = ToolbarStyleConstants.IconBackgroundHeight;
                float bgX = buttonRect.x + (buttonRect.width - bgW) * 0.5f + ToolbarStyleConstants.IconBackgroundOffsetX;
                float bgY = buttonRect.y + (buttonRect.height - bgH) * 0.5f + ToolbarStyleConstants.IconBackgroundOffsetY;
                var bgRect = new Rect(bgX, bgY, bgW, bgH);
                EditorGUI.DrawRect(bgRect, bgColor);

                float size = ToolbarStyleConstants.IconSize;
                float x = bgRect.x + (bgRect.width - size) * 0.5f + ToolbarStyleConstants.IconOffsetX;
                float y = bgRect.y + (bgRect.height - size) * 0.5f + ToolbarStyleConstants.IconOffsetY;

                Color prevColor = GUI.color;
                Color iconColor = ToolbarStyleConstants.GetIconColor(isPlayMode, isHover, false, isPressed);
                GUI.color = iconColor;
                GUI.DrawTexture(new Rect(x, y, size, size), _iconTexture, ScaleMode.ScaleToFit, true);
                GUI.color = prevColor;
            }

            GUILayout.Space(ToolbarStyleConstants.ElementSpacing);
        }

        private void RestartCurrentScene()
        {
            if (!Application.isPlaying)
            {
                string warningMessage = LocalizationHelper.Common.NotInPlayMode;
                Debug.LogWarning(LocalizationHelper.LogTags.Warning(warningMessage));
                return;
            }

            Scene currentScene = SceneManager.GetActiveScene();
            string sceneName = currentScene.name;

            SceneManager.LoadScene(sceneName);

            string message = LocalizationHelper.Messages.SceneRestarted(sceneName);
            Debug.Log(LocalizationHelper.LogTags.Success(message));
        }
    }
}