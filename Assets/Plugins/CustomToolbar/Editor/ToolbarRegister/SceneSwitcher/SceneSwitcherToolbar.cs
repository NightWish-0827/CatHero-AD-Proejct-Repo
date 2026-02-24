using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace NightWish.CustomToolbar
{
    /// <summary>
    /// 씬 전환 기능을 제공하는 툴바 확장
    /// </summary>
    public class SceneSwitcherToolbar : IRightToolbarExtension
    {
        private static SceneSwitcherToolbar _instance;
        public static SceneSwitcherToolbar Instance => _instance ??= new SceneSwitcherToolbar();

        // 추후 사용자가 추가할 아이콘 파일 이름 (폴더 구조가 바뀌어도 파일명 기준으로 검색)
        private const string IconFileName = "scene_switch_icon.png";
        private static Texture2D _iconTexture;
        private static readonly GUIContent _tooltipContent =
            new GUIContent(string.Empty, LocalizationHelper.Common.SceneSelect);

        private static void EnsureIconLoaded()
        {
            if (_iconTexture != null)
                return;

            _iconTexture = ToolbarIconLoader.LoadIcon(IconFileName, "Extension/CustomToolbar/Editor/Icons/" + IconFileName);
        }

        private GenericMenu _sceneMenu;

        public void OnToolbarGUI()
        {
            GUILayout.Space(ToolbarStyleConstants.ElementSpacing);
            
            EnsureIconLoaded();
            
            // 완전히 투명한 버튼(배경 없음)을 직접 그린 뒤, 그 영역 안에 BG(31x18) + 아이콘을 수동으로 그립니다.
            // ToolbarExtender가 툴바 높이를 22로 고정하므로, 그에 맞춰 높이를 지정
            Rect buttonRect = GUILayoutUtility.GetRect(
                ToolbarStyleConstants.PrimaryButtonWidth,
                22f,
                ToolbarStyleConstants.IconOnlyToolbarButtonStyle
            );
            // Restart 버튼 기준 오프셋 + 추가 간격(SceneSwitcherOffsetFromRestartX)만큼 이동
            buttonRect.x += ToolbarStyleConstants.RightButtonOffsetX + ToolbarStyleConstants.SceneSwitcherOffsetFromRestartX;

            bool isHover = buttonRect.Contains(Event.current.mousePosition);
            bool isPressed = false;

            if (GUI.Button(buttonRect, _tooltipContent, ToolbarStyleConstants.IconOnlyToolbarButtonStyle))
            {
                ShowSceneSelectionMenu();
                isPressed = true;
            }

            // 버튼 Rect 위에 BG(31x18)를 먼저 그리고, 그 위에 아이콘을 중앙 정렬로 오버레이
            if (_iconTexture != null)
            {
                // 현재 상태/마우스 상태에 따라 BG 색상 결정 (Play/Pause 블록과 비슷한 느낌)
                Color bgColor = ToolbarStyleConstants.GetButtonBackgroundColor(false, isHover, isPressed);

                // 1) BG 네모(31x18)
                float bgW = ToolbarStyleConstants.IconBackgroundWidth;
                float bgH = ToolbarStyleConstants.IconBackgroundHeight;
                float bgX = buttonRect.x + (buttonRect.width - bgW) * 0.5f + ToolbarStyleConstants.IconBackgroundOffsetX;
                float bgY = buttonRect.y + (buttonRect.height - bgH) * 0.5f + ToolbarStyleConstants.IconBackgroundOffsetY;
                var bgRect = new Rect(bgX, bgY, bgW, bgH);

                EditorGUI.DrawRect(bgRect, bgColor);

                // 2) 아이콘 - BG를 기준으로 배치
                float size = ToolbarStyleConstants.IconSize;
                float x = bgRect.x + (bgRect.width - size) * 0.5f + ToolbarStyleConstants.IconOffsetX;
                float y = bgRect.y + (bgRect.height - size) * 0.5f + ToolbarStyleConstants.IconOffsetY;

                Color prevColor = GUI.color;
                Color iconColor = ToolbarStyleConstants.GetIconColor(true, isHover, false, isPressed);
                GUI.color = iconColor;
                GUI.DrawTexture(new Rect(x, y, size, size), _iconTexture, ScaleMode.ScaleToFit, true);
                GUI.color = prevColor;
            }

            GUILayout.Space(ToolbarStyleConstants.ElementSpacing);
        }

        private void ShowSceneSelectionMenu()
        {
            Dictionary<string, string> allScenes = SceneHelper.FindAllScenes();
            _sceneMenu = new GenericMenu();
            
            foreach ((string scenePath, string sceneAllPath) in allScenes)
            {
                _sceneMenu.AddItem(
                    new GUIContent(scenePath), 
                    false, 
                    OnSceneSelected, 
                    sceneAllPath
                );
            }
            
            _sceneMenu.ShowAsContext();
        }

        private void OnSceneSelected(object parameter)
        {
            string scenePath = (string)parameter;
            SceneHelper.StartScene(scenePath, false);
        }
    }

    /// <summary>
    /// 씬 관련 유틸리티 헬퍼 클래스
    /// </summary>
    internal static class SceneHelper
    {
        private static string _sceneToOpen;
        private static bool _isAutoPlay;
        private const string TargetSceneFolderPath = "Assets/Scenes";

        public static void StartScene(string sceneName, bool isPlay)
        {
            if (EditorApplication.isPlaying) 
                EditorApplication.isPlaying = false;
                
            _sceneToOpen = sceneName;
            _isAutoPlay = isPlay;
            EditorApplication.update += OnUpdate;
        }

        public static Dictionary<string, string> FindAllScenes()
        {
            Dictionary<string, string> result = new();
            string[] guids = AssetDatabase.FindAssets("t:scene", new[] { TargetSceneFolderPath });
            
            foreach (string guid in guids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                string filterSceneName = scenePath.Replace(".unity", "");
                
                try
                {
                    string resultPath = filterSceneName.Replace($"{TargetSceneFolderPath}/", "");
                    string resultSceneName = scenePath;
                    result.Add(resultPath, resultSceneName);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"씬 경로 처리 중 오류 발생: {ex.Message}");
                }
            }
            
            return result;
        }

        private static void OnUpdate()
        {
            if (_sceneToOpen == null ||
                EditorApplication.isPlaying || 
                EditorApplication.isPaused ||
                EditorApplication.isCompiling || 
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.update -= OnUpdate;
            
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(_sceneToOpen);
                EditorApplication.isPlaying = _isAutoPlay;
            }
            
            _isAutoPlay = false;
            _sceneToOpen = null;
        }
    }
}
