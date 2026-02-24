using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NightWish.CustomToolbar
{
    /// <summary>
    /// 실시간 프레임 레이트를 표시하는 툴바 확장
    /// </summary>
    public class FrameRateToolbar : ILeftToolbarExtension
    {
        private static FrameRateToolbar _instance;
        public static FrameRateToolbar Instance => _instance ??= new FrameRateToolbar();

#pragma warning disable  CS0414
        private float _deltaTime = 0.0f;
#pragma warning restore  CS0414

        private bool _isPlaying = false;

        private float _smoothedFrameMs = -1f;
        private const float FrameSmoothing = 0.1f; // Stats 체감과 거의 동일/*  */

        // 모바일 ~ PC에서 자주 사용하는 정규화된 FPS 프리셋들
        private static readonly int[] _presetFrameRates = new[]
        {
            -1,   // 제한 없음 / 엔진 기본
            30,
            45,
            60,
            90,
            120,
            144,
            165,
            240
        };

        private FrameRateToolbar()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    _isPlaying = true;
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    _isPlaying = false;
                    _deltaTime = 0.0f;
                    break;
            }
        }

        private void OnActiveSceneChanged(Scene current, Scene next)
        {
            _deltaTime = 0.0f;
        }

        private float CalculateEditorFPS()
        {
            // Native frame timing 캡처
            FrameTimingManager.CaptureFrameTimings();

            FrameTiming[] timings = new FrameTiming[1];
            if (FrameTimingManager.GetLatestTimings(1, timings) == 0)
                return 0f;

            float frameMs = (float)timings[0].cpuFrameTime;
            if (frameMs <= 0f)
                return 0f;

            // EMA 기반 smoothing (Stats 느낌)
            if (_smoothedFrameMs < 0f)
                _smoothedFrameMs = frameMs;
            else
                _smoothedFrameMs = Mathf.Lerp(_smoothedFrameMs, frameMs, FrameSmoothing);

            return 1000f / _smoothedFrameMs;
        }

        public void OnToolbarGUI()
        {
            GUILayout.Space(ToolbarStyleConstants.GroupSpacing);

            string label;
            if (_isPlaying)
            {
                float fps = CalculateEditorFPS();
                label = fps > 0f ? $"FPS: {fps:0.}" : "FPS: --";
            }
            else
            {
                label = "FPS: --";
            }

            GUIContent content = new GUIContent(label, LocalizationHelper.Common.FrameRate);

            Rect rect = GUILayoutUtility.GetRect(
                content,
                ToolbarStyleConstants.CommandButtonStyle,
                GUILayout.Height(22f)
            );

            if (GUI.Button(rect, content, ToolbarStyleConstants.CommandButtonStyle))
            {
                ShowFrameRateMenu(rect);
            }
        }

        /// <summary>
        /// 정규화된 FPS 프리셋을 선택할 수 있는 드롭다운 메뉴를 표시합니다.
        /// </summary>
        private static void ShowFrameRateMenu(Rect buttonRect)
        {
            GenericMenu menu = new GenericMenu();

            int currentTarget = Application.targetFrameRate;

            // 프리셋 항목 생성
            foreach (int fps in _presetFrameRates)
            {
                string label = fps <= 0
                    ? "제한 없음 (-1)"
                    : $"{fps} FPS";

                bool isOn = fps <= 0
                    ? currentTarget <= 0
                    : currentTarget == fps;

                menu.AddItem(
                    new GUIContent(label),
                    isOn,
                    obj =>
                    {
                        int selected = (int)obj;
                        Application.targetFrameRate = selected;
                    },
                    fps
                );
            }

            menu.DropDown(buttonRect);
        }
    }
}
