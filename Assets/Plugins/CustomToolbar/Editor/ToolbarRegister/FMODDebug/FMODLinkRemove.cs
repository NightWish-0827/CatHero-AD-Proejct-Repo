using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Saint.CustomToolbar
{
    /// <summary>
    /// FMOD Define Symbol을 수동으로 제거하는 유틸리티 클래스
    /// </summary>
    public static class FMODLinkRemove
    {
        private const string FMODDefineSymbol = "USE_FMOD";

        [MenuItem("Tools/CustomToolbar/FMOD/Remove Define Symbol")]
        private static void RemoveDefine()
        {
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            
#if UNITY_2023_1_OR_NEWER
            string symbols = PlayerSettings.GetScriptingDefineSymbols(
                NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));

            if (symbols.Contains(FMODDefineSymbol))
            {
                string newSymbols = RemoveSymbolFromString(symbols, FMODDefineSymbol);
                PlayerSettings.SetScriptingDefineSymbols(
                    NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup), 
                    newSymbols);
                    
                Debug.Log($"USE_FMOD 심볼이 제거되었습니다.");
            }
            else
            {
                Debug.LogWarning("USE_FMOD 심볼이 존재하지 않습니다.");
            }
#else
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

            if (symbols.Contains(FMODDefineSymbol))
            {
                string newSymbols = RemoveSymbolFromString(symbols, FMODDefineSymbol);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newSymbols);
                
                Debug.Log($"USE_FMOD 심볼이 제거되었습니다.");
            }
            else
            {
                Debug.LogWarning("USE_FMOD 심볼이 존재하지 않습니다.");
            }
#endif
        }

        /// <summary>
        /// 심볼 문자열에서 특정 심볼을 제거합니다.
        /// </summary>
        /// <param name="symbols">원본 심볼 문자열</param>
        /// <param name="symbolToRemove">제거할 심볼</param>
        /// <returns>심볼이 제거된 새로운 문자열</returns>
        private static string RemoveSymbolFromString(string symbols, string symbolToRemove)
        {
            if (string.IsNullOrEmpty(symbols))
                return string.Empty;

            string[] symbolArray = symbols.Split(';');
            var filteredSymbols = new System.Collections.Generic.List<string>();

            foreach (string symbol in symbolArray)
            {
                string trimmedSymbol = symbol.Trim();
                if (!string.IsNullOrEmpty(trimmedSymbol) && trimmedSymbol != symbolToRemove)
                {
                    filteredSymbols.Add(trimmedSymbol);
                }
            }

            return string.Join(";", filteredSymbols);
        }
    }
}
