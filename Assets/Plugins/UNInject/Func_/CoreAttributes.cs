using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

// ------------------- Attributes -------------------

/// <summary>
/// [Local]
/// - 같은 ObjectInstaller 루트 계층 안에서
///   타입이 일치하는 컴포넌트를 찾아 에디터에서 베이크하는 필드입니다.
/// - 반드시 [SerializeField] 와 함께 사용하고,
///   값은 ObjectInstaller.BakeDependencies 에 의해 자동 설정됩니다.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class InjectAttribute : PropertyAttribute { }

/// <summary>
/// [Global]
/// - MasterInstaller 가 인덱싱한 Manager Layer 컴포넌트 중
///   타입이 일치하는 대상을 런타임에 주입받기 위한 마커입니다.
/// - [SerializeField] 를 사용하지 않는 private 필드에 붙여 사용합니다.
/// - 실제 값 설정은 ObjectInstaller.Awake 에서 MasterInstaller.Resolve 를 통해 이루어집니다.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class GlobalInjectAttribute : Attribute { }

/// <summary>
/// [Scene]
/// - SceneInstaller 가 인덱싱한 씬 전용 Manager Layer 컴포넌트 중
///   타입이 일치하는 대상을 런타임에 주입받기 위한 마커입니다.
/// - [SerializeField] 를 사용하지 않는 private 필드에 붙여 사용합니다.
/// - 실제 값 설정은 ObjectInstaller.Awake 에서 SceneInstaller.Resolve 를 통해 이루어집니다.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SceneInjectAttribute : Attribute { }

/// <summary>
/// 이 Attribute 가 붙은 MonoBehaviour 는
/// MasterInstaller 가 관리하는 전역 Manager Layer 레지스트리에 포함됩니다.
/// (게임 전체에서 공유되는 전역 매니저)
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ReferralAttribute : Attribute { }

/// <summary>
/// 이 Attribute 가 붙은 MonoBehaviour 는
/// SceneInstaller 가 관리하는 씬 전용 Manager Layer 레지스트리에 포함됩니다.
/// (해당 씬 안에서만 의미가 있는 매니저)
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SceneReferralAttribute : Attribute { }


// ------------------- Editor Drawers -------------------
#if UNITY_EDITOR

/// <summary>
/// [Inject] 필드를 인스펙터에서 읽기 전용으로 표시하는 Drawer 입니다.
/// (GlobalInject 는 런타임 주입 전용이므로 인스펙터에 노출하지 않습니다)
/// </summary>
[CustomPropertyDrawer(typeof(InjectAttribute))]
public class InjectDrawer : PropertyDrawer
{
    // true면 아예 숨김, false면 'Read Only'로 보여줌 (디버깅용)
    // 일단은 ReadOnly 로 표시
    // 추후에 ReadOnly 표기 말고 좋은 렌더링 방식이 생각나면 변경 예정입니다.
    private const bool HIDE_COMPLETELY = false;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
#pragma warning disable CS0162
        if (HIDE_COMPLETELY) return;
#pragma warning restore CS0162

        GUI.enabled = false;

        string prefix = "[Local]";
        label.text = $"{prefix} {label.text}";
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return HIDE_COMPLETELY ? 0f : base.GetPropertyHeight(property, label);
    }
}
#endif