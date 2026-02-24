using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NightWish.CustomToolbar
{
    /// <summary>
    /// 툴바 확장 기능들을 동적으로 등록하고 관리하는 레지스트리 클래스
    /// </summary>
    public static class ToolbarExtensionRegistry
    {
        private static readonly List<ILeftToolbarExtension> _leftExtensions = new();
        private static readonly List<IRightToolbarExtension> _rightExtensions = new();
        private static readonly Dictionary<Type, bool> _extensionStates = new();

        public static IReadOnlyList<ILeftToolbarExtension> LeftExtensions => _leftExtensions;
        public static IReadOnlyList<IRightToolbarExtension> RightExtensions => _rightExtensions;

        /// <summary>
        /// 왼쪽 툴바 확장 기능을 등록합니다.
        /// </summary>
        /// <param name="extension">등록할 확장 기능</param>
        public static void RegisterLeftExtension(ILeftToolbarExtension extension)
        {
            if (extension == null)
                return;

            if (!_leftExtensions.Contains(extension))
            {
                _leftExtensions.Add(extension);
                _extensionStates[extension.GetType()] = true;
            }
        }

        /// <summary>
        /// 오른쪽 툴바 확장 기능을 등록합니다.
        /// </summary>
        /// <param name="extension">등록할 확장 기능</param>
        public static void RegisterRightExtension(IRightToolbarExtension extension)
        {
            if (extension == null)
                return;

            if (!_rightExtensions.Contains(extension))
            {
                _rightExtensions.Add(extension);
                _extensionStates[extension.GetType()] = true;
            }
        }

        /// <summary>
        /// 특정 확장 기능의 활성화 상태를 설정합니다.
        /// </summary>
        /// <param name="extensionType">확장 기능 타입</param>
        /// <param name="enabled">활성화 여부</param>
        public static void SetExtensionEnabled(Type extensionType, bool enabled)
        {
            // 설정이 먼저 적용되고, 이후 확장이 등록되는 상황까지 고려하여
            // 딕셔너리 존재 여부와 관계없이 상태를 기록해 둔다.
            _extensionStates[extensionType] = enabled;
        }

        /// <summary>
        /// 특정 확장 기능의 활성화 상태를 확인합니다.
        /// </summary>
        /// <param name="extensionType">확장 기능 타입</param>
        /// <returns>활성화 여부</returns>
        public static bool IsExtensionEnabled(Type extensionType)
        {
            return _extensionStates.TryGetValue(extensionType, out bool enabled) && enabled;
        }

        /// <summary>
        /// 모든 활성화된 왼쪽 툴바 확장 기능들의 GUI를 렌더링합니다.
        /// </summary>
        public static void RenderLeftExtensions()
        {
            foreach (var extension in _leftExtensions.Where(ext => IsExtensionEnabled(ext.GetType())))
            {
                try
                {
                    extension.OnToolbarGUI();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"왼쪽 툴바 확장 {extension.GetType().Name} 렌더링 중 오류: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 모든 활성화된 오른쪽 툴바 확장 기능들의 GUI를 렌더링합니다.
        /// </summary>
        public static void RenderRightExtensions()
        {
            foreach (var extension in _rightExtensions.Where(ext => IsExtensionEnabled(ext.GetType())))
            {
                try
                {
                    extension.OnToolbarGUI();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"오른쪽 툴바 확장 {extension.GetType().Name} 렌더링 중 오류: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 등록된 모든 확장 기능을 해제합니다.
        /// </summary>
        public static void ClearAll()
        {
            _leftExtensions.Clear();
            _rightExtensions.Clear();
            _extensionStates.Clear();
        }
    }
}
