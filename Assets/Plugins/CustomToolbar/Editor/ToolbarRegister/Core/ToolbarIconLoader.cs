using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NightWish.CustomToolbar
{
    /// <summary>
    /// 커스텀 툴바 아이콘을 폴더 구조 변경에 강인하게 로드하기 위한 헬퍼.
    /// - 기본 경로: Assets/Extension/CustomToolbar/Editor/Icons
    /// - 폴더가 이동/이름 변경되어도, "CustomToolbar/Editor/Icons" 를 포함하고 파일명이 일치하면 검색해서 로드.
    /// </summary>
    public static class ToolbarIconLoader
    {
        private const string DefaultRoot = "Assets";
        private const string PreferredFolderHint = "CustomToolbar";
        private const string PreferredSubFolder = "Editor/Icons";

        private static readonly Dictionary<string, Texture2D> _cache = new();

        public static Texture2D LoadIcon(string fileName, string legacyRelativePath)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            if (_cache.TryGetValue(fileName, out var cached) && cached != null)
                return cached;

            // 1) 우선 기존 하드코딩 경로 시도 (하위 호환)
            if (!string.IsNullOrEmpty(legacyRelativePath))
            {
                string legacyPath = Path.Combine(DefaultRoot, legacyRelativePath).Replace("\\", "/");
                var legacyIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(legacyPath);
                if (legacyIcon != null)
                {
                    _cache[fileName] = legacyIcon;
                    return legacyIcon;
                }
            }

            string fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrEmpty(fileNameNoExt))
                return null;

            // 2) CustomToolbar/Editor/Icons 를 포함하는 경로 위주로 검색
            string filter = $"{fileNameNoExt} t:Texture2D";
            string[] guids = AssetDatabase.FindAssets(filter);
            Texture2D bestMatch = null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                    continue;

                // CustomToolbar 및 Editor/Icons 를 포함하는 경로를 우선 사용
                bool hasToolbar = path.IndexOf(PreferredFolderHint, StringComparison.OrdinalIgnoreCase) >= 0;
                bool hasIconsSub = path.Replace("\\", "/")
                                       .IndexOf(PreferredSubFolder, StringComparison.OrdinalIgnoreCase) >= 0;

                if (hasToolbar && hasIconsSub)
                {
                    bestMatch = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (bestMatch != null)
                    {
                        _cache[fileName] = bestMatch;
                        return bestMatch;
                    }
                }

                // CustomToolbar 안에만 있어도 후보로 기억함함
                if (bestMatch == null && hasToolbar)
                {
                    bestMatch = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            }

            if (bestMatch != null)
            {
                _cache[fileName] = bestMatch;
                return bestMatch;
            }

            // 3) 마지막으로 Assets 전체에서 파일명 일치하는 텍스처 아무거나 뭐 어쩌자고
            guids = AssetDatabase.FindAssets($"t:Texture2D {fileNameNoExt}", new[] { DefaultRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                {
                    _cache[fileName] = tex;
                    return tex;
                }
            }

            return null;
        }
    }
}


