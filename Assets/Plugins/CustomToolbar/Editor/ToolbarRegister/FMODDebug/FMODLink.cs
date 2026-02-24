using UnityEditor;
using UnityEditor.Build;

namespace Saint.CustomToolbar
{
    /// <summary>
    /// FMOD 플러그인이 존재할 때 자동으로 USE_FMOD 심볼을 추가하는 클래스
    /// </summary>
    [InitializeOnLoad]
    public class FMODLink
    {
        private const string FMODFolderPath = "Assets/Plugins/FMOD";
        private const string FMODDefineSymbol = "USE_FMOD";

        static FMODLink()
        {
            if (AssetDatabase.IsValidFolder(FMODFolderPath))
            {
                AddFMODDefineSymbol();
            }
        }

        private static void AddFMODDefineSymbol()
        {
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            
#if UNITY_2023_1_OR_NEWER
            string symbols = PlayerSettings.GetScriptingDefineSymbols(
                NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));

            if (!symbols.Contains(FMODDefineSymbol))
            {
                string newSymbols = string.IsNullOrEmpty(symbols) 
                    ? FMODDefineSymbol 
                    : $"{symbols};{FMODDefineSymbol}";
                    
                PlayerSettings.SetScriptingDefineSymbols(
                    NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup), 
                    newSymbols);
            }
#else
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

            if (!symbols.Contains(FMODDefineSymbol))
            {
                string newSymbols = string.IsNullOrEmpty(symbols) 
                    ? FMODDefineSymbol 
                    : $"{symbols};{FMODDefineSymbol}";
                    
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newSymbols);
            }
#endif
        }
    }
}
