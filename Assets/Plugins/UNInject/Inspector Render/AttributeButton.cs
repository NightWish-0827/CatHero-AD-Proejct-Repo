#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

[CustomEditor(typeof(ObjectInstaller))]
public class ObjectInstallerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ObjectInstaller installer = (ObjectInstaller)target;

        // ---------------- Bake 버튼 ----------------
        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("🍩 Bake Dependencies", GUILayout.Height(20)))
        {
            installer.BakeDependencies();
        }
        GUI.backgroundColor = Color.white;

        // ---------------- 참조 상태 시각화 ----------------
        DrawReferenceOverview(installer);

        EditorGUILayout.Space(8);

        // m_Script를 숨기고 나머지 필드만 표시
        serializedObject.Update();
        var prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (prop.name == "m_Script")
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawReferenceOverview(ObjectInstaller installer)
    {
        var monoBehaviours = installer.GetComponentsInChildren<MonoBehaviour>(true);

        int injectFieldCount = 0;
        int injectWiredCount = 0;
        int sceneInjectFieldCount = 0;
        int globalInjectFieldCount = 0;

        var localLines = new List<(bool linked, string text)>();
        var sceneLines = new List<(bool inSceneRegistry, string text)>();
        var globalLines = new List<(bool inRegistry, string text)>();

        foreach (var mb in monoBehaviours)
        {
            if (mb == null) continue;

            var fields = mb.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var so = new SerializedObject(mb);

            foreach (var field in fields)
            {
                if (System.Attribute.IsDefined(field, typeof(InjectAttribute)))
                {
                    injectFieldCount++;

                    Object value = null;
                    var sp = so.FindProperty(field.Name);
                    if (sp != null && sp.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        value = sp.objectReferenceValue;
                    }
                    else
                    {
                        // fallback
                        value = field.GetValue(mb) as Object;
                    }

                    if (value != null) injectWiredCount++;

                    string owner = mb.GetType().Name;
                    string targetName = value != null ? value.GetType().Name : "None";
                    string line = $"{owner}.{field.Name} → {targetName}";
                    localLines.Add((value != null, line));
                }
                else if (System.Attribute.IsDefined(field, typeof(SceneInjectAttribute)))
                {
                    sceneInjectFieldCount++;

                    bool inSceneRegistry = IsTypeRegisteredInScene(field.FieldType);

                    string owner = mb.GetType().Name;
                    string line = $"{owner}.{field.Name} : {field.FieldType.Name}";
                    sceneLines.Add((inSceneRegistry, line));
                }
                else if (System.Attribute.IsDefined(field, typeof(GlobalInjectAttribute)))
                {
                    globalInjectFieldCount++;

                    bool inRegistry = IsTypeRegisteredInMaster(field.FieldType);

                    string owner = mb.GetType().Name;
                    string line = $"{owner}.{field.Name} : {field.FieldType.Name}";
                    globalLines.Add((inRegistry, line));
                }
            }
        }

        // ----- Local [Inject] -----
        var boxStyle = new GUIStyle("HelpBox")
        {
            richText = true,
            padding = new RectOffset(8, 8, 6, 6)
        };

        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Local Dependencies [Inject]", EditorStyles.boldLabel);

        if (injectFieldCount == 0)
        {
            EditorGUILayout.LabelField(
                "<i>이 루트 계층에서 [Inject] 필드를 가진 컴포넌트가 없습니다.</i>",
                new GUIStyle(EditorStyles.label) { richText = true });
        }
        else
        {
            EditorGUILayout.LabelField(
                $"<color=#7BD88F><b>{injectWiredCount}</b></color> / {injectFieldCount} fields linked",
                new GUIStyle(EditorStyles.label) { richText = true });

            int displayLimit = 6;
            foreach (var (linked, text) in localLines.Take(displayLimit))
            {
                string bulletColor = linked ? "#7BD88F" : "#FFB374";
                string status = linked ? "<color=#7BD88F>[OK]</color>" : "<color=#FF6B6B>[Missing]</color>";
                EditorGUILayout.LabelField(
                    $"<color={bulletColor}>●</color> {text}  {status}",
                    new GUIStyle(EditorStyles.label) { richText = true });
            }

            if (localLines.Count > displayLimit)
            {
                EditorGUILayout.LabelField(
                    $"… 그리고 <b>{localLines.Count - displayLimit}</b> 개 더",
                    new GUIStyle(EditorStyles.miniLabel) { richText = true });
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // ----- Scene [SceneInject] -----
        var boxStyleScene = new GUIStyle("HelpBox")
        {
            richText = true,
            padding = new RectOffset(8, 8, 6, 6)
        };

        EditorGUILayout.BeginVertical(boxStyleScene);
        EditorGUILayout.LabelField("Scene Dependencies [SceneInject]", EditorStyles.boldLabel);

        if (sceneInjectFieldCount == 0)
        {
            EditorGUILayout.LabelField(
                "<i>이 루트 계층에서 [SceneInject] 필드를 가진 컴포넌트가 없습니다.</i>",
                new GUIStyle(EditorStyles.label) { richText = true });
        }
        else
        {
            EditorGUILayout.LabelField(
                $"{sceneInjectFieldCount} fields require SceneInstaller managers",
                new GUIStyle(EditorStyles.label) { richText = true });

            int displayLimit = 6;
            foreach (var (inSceneRegistry, text) in sceneLines.Take(displayLimit))
            {
                string bulletColor = inSceneRegistry ? "#a6e3ff" : "#FFB374";
                string status = inSceneRegistry
                    ? "<color=#7BD88F>[Registered]</color>"
                    : "<color=#FF6B6B>[Not in scene registry]</color>";
                EditorGUILayout.LabelField(
                    $"<color={bulletColor}>●</color> {text}  {status}",
                    new GUIStyle(EditorStyles.label) { richText = true });
            }

            if (sceneLines.Count > displayLimit)
            {
                EditorGUILayout.LabelField(
                    $"… 그리고 <b>{sceneLines.Count - displayLimit}</b> 개 더",
                    new GUIStyle(EditorStyles.miniLabel) { richText = true });
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // ----- Global [GlobalInject] -----
        var boxStyleGlobal = new GUIStyle("HelpBox")
        {
            richText = true,
            padding = new RectOffset(8, 8, 6, 6)
        };

        EditorGUILayout.BeginVertical(boxStyleGlobal);
        EditorGUILayout.LabelField("Global Dependencies [GlobalInject]", EditorStyles.boldLabel);

        if (globalInjectFieldCount == 0)
        {
            EditorGUILayout.LabelField(
                "<i>이 루트 계층에서 [GlobalInject] 필드를 가진 컴포넌트가 없습니다.</i>",
                new GUIStyle(EditorStyles.label) { richText = true });
        }
        else
        {
            EditorGUILayout.LabelField(
                $"{globalInjectFieldCount} fields require Manager Layer from MasterInstaller",
                new GUIStyle(EditorStyles.label) { richText = true });

            int displayLimit = 6;
            foreach (var (inRegistry, text) in globalLines.Take(displayLimit))
            {
                string bulletColor = inRegistry ? "#7BB4FF" : "#FFB374";
                string status = inRegistry
                    ? "<color=#7BD88F>[Registered]</color>"
                    : "<color=#FF6B6B>[Not in registry]</color>";
                EditorGUILayout.LabelField(
                    $"<color={bulletColor}>●</color> {text}  {status}",
                    new GUIStyle(EditorStyles.label) { richText = true });
            }

            if (globalLines.Count > displayLimit)
            {
                EditorGUILayout.LabelField(
                    $"… 그리고 <b>{globalLines.Count - displayLimit}</b> 개 더",
                    new GUIStyle(EditorStyles.miniLabel) { richText = true });
            }
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Master Installer 런 타임 검증 
    /// </summary>
    private bool IsTypeRegisteredInMaster(System.Type fieldType)
    {
        if (fieldType == null) return false;

        var master = Object.FindObjectOfType<MasterInstaller>();
        if (master == null) return false;

        var so = new SerializedObject(master);
        var prop = so.FindProperty("_globalReferrals");
        if (prop == null) return false;

        for (int i = 0; i < prop.arraySize; i++)
        {
            var element = prop.GetArrayElementAtIndex(i);
            var comp = element.objectReferenceValue as Component;
            if (comp == null) continue;

            if (fieldType.IsAssignableFrom(comp.GetType()))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Scene Installer 런 타임 검증 
    /// </summary>
    private bool IsTypeRegisteredInScene(System.Type fieldType)
    {
        if (fieldType == null) return false;

        var sceneInstaller = Object.FindObjectOfType<SceneInstaller>();
        if (sceneInstaller == null) return false;

        var so = new SerializedObject(sceneInstaller);
        var prop = so.FindProperty("_sceneReferrals");
        if (prop == null) return false;

        for (int i = 0; i < prop.arraySize; i++)
        {
            var element = prop.GetArrayElementAtIndex(i);
            var comp = element.objectReferenceValue as Component;
            if (comp == null) continue;

            if (fieldType.IsAssignableFrom(comp.GetType()))
                return true;
        }

        return false;
    }
}

[CustomEditor(typeof(MasterInstaller))]
public class MasterInstallerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MasterInstaller installer = (MasterInstaller)target;

        // ---------------- Refresh ----------------
        GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
        if (GUILayout.Button("🔄 Refresh Global Registry", GUILayout.Height(20)))
        {
            installer.RefreshRegistry();
        }
        GUI.backgroundColor = Color.white;

        // ---------------- 레지스트리 시각화 ----------------
        DrawRegistryOverview();

        EditorGUILayout.Space(8);

        // m_Script 필드는 유니티에서 Mono 스크립트 표기하는 ReadOnly 필드입니다. 딱히 필요 없으니 숨깁니다.
        // 따로 빼시려면 주석 추가해주세요.
        serializedObject.Update();
        var prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (prop.name == "m_Script")
                continue;

            // _globalReferrals 는 읽기 전용으로 표시
            bool isRegistry = prop.name == "_globalReferrals";
            if (isRegistry) GUI.enabled = false;

            EditorGUILayout.PropertyField(prop, true);

            if (isRegistry) GUI.enabled = true;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRegistryOverview()
    {
        var registryProp = serializedObject.FindProperty("_globalReferrals");
        int count = registryProp != null ? registryProp.arraySize : 0;

        var boxStyle = new GUIStyle("HelpBox")
        {
            richText = true,
            padding = new RectOffset(8, 8, 6, 6)
        };

        EditorGUILayout.BeginVertical(boxStyle);

        string headerColor = count > 0 ? "#7BB4FF" : "#FFB374";
        EditorGUILayout.LabelField(
            $"<color={headerColor}><b>Global Registry</b></color>  ({count} managers)",
            new GUIStyle(EditorStyles.label) { richText = true });

        if (count == 0)
        {
            EditorGUILayout.LabelField(
                "<i>등록된 Manager 가 없습니다. 'Refresh Global Registry' 버튼을 눌러 씬의 [Referral] 컴포넌트를 스캔하세요.</i>",
                new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true });
        }
        else
        {
            int displayLimit = 8;
            for (int i = 0; i < count && i < displayLimit; i++)
            {
                var element = registryProp.GetArrayElementAtIndex(i);
                var obj = element.objectReferenceValue as Component;
                if (obj == null) continue;

                string line = $"• <b>{obj.GetType().Name}</b>  <color=#888888>({obj.name})</color>";
                EditorGUILayout.LabelField(line, new GUIStyle(EditorStyles.label) { richText = true });
            }

            if (count > displayLimit)
            {
                EditorGUILayout.LabelField(
                    $"… 그리고 <b>{count - displayLimit}</b> 개 더",
                    new GUIStyle(EditorStyles.miniLabel) { richText = true });
            }
        }

        EditorGUILayout.EndVertical();
    }
}

[CustomEditor(typeof(SceneInstaller))]
public class SceneInstallerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SceneInstaller installer = (SceneInstaller)target;

        // ---------------- Refresh ----------------
        GUI.backgroundColor = new Color(0.6f, 1f, 0.9f);
        if (GUILayout.Button("🔄 Refresh Scene Registry", GUILayout.Height(20)))
        {
            installer.RefreshSceneRegistry();
        }
        GUI.backgroundColor = Color.white;

        // ---------------- 레지스트리 시각화 ----------------
        DrawSceneRegistryOverview();

        EditorGUILayout.Space(8);

        // m_Script를 숨기고 나머지 필드만 표시
        serializedObject.Update();
        var prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (prop.name == "m_Script")
                continue;

            // _sceneReferrals 는 읽기 전용으로 표시
            bool isRegistry = prop.name == "_sceneReferrals";
            if (isRegistry) GUI.enabled = false;

            EditorGUILayout.PropertyField(prop, true);

            if (isRegistry) GUI.enabled = true;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSceneRegistryOverview()
    {
        var registryProp = serializedObject.FindProperty("_sceneReferrals");
        int count = registryProp != null ? registryProp.arraySize : 0;

        var boxStyle = new GUIStyle("HelpBox")
        {
            richText = true,
            padding = new RectOffset(8, 8, 6, 6)
        };

        EditorGUILayout.BeginVertical(boxStyle);

        string headerColor = count > 0 ? "#a6e3ff" : "#FFB374";
        EditorGUILayout.LabelField(
            $"<color={headerColor}><b>Scene Registry</b></color>  ({count} managers)",
            new GUIStyle(EditorStyles.label) { richText = true });

        if (count == 0)
        {
            EditorGUILayout.LabelField(
                "<i>등록된 Scene 매니저가 없습니다. 'Refresh Scene Registry' 버튼을 눌러 씬의 [SceneReferral] 컴포넌트를 스캔하세요.</i>",
                new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true });
        }
        else
        {
            int displayLimit = 8;
            for (int i = 0; i < count && i < displayLimit; i++)
            {
                var element = registryProp.GetArrayElementAtIndex(i);
                var obj = element.objectReferenceValue as Component;
                if (obj == null) continue;

                string line = $"• <b>{obj.GetType().Name}</b>  <color=#888888>({obj.name})</color>";
                EditorGUILayout.LabelField(line, new GUIStyle(EditorStyles.label) { richText = true });
            }

            if (count > displayLimit)
            {
                EditorGUILayout.LabelField(
                    $"… 그리고 <b>{count - displayLimit}</b> 개 더",
                    new GUIStyle(EditorStyles.miniLabel) { richText = true });
            }
        }

        EditorGUILayout.EndVertical();
    }
}
#endif