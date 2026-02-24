using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// 특정 루트 GameObject 에 붙어서,
/// 그 자식 계층 내부의 의존성을 관리하는 컴포넌트입니다.
/// 
/// - [Inject]       : 같은 루트 계층 내의 컴포넌트를 찾아 에디터에서 베이크
/// - [GlobalInject] : MasterInstaller 가 관리하는 Manager Layer 컴포넌트를 런타임에 주입
/// </summary>
[DefaultExecutionOrder(-500)]
public class ObjectInstaller : MonoBehaviour
{
    // ------------------- Runtime (GlobalInject) -------------------

    private void Awake()
    {
        InjectGlobalDependencies();
    }

    /// <summary>
    /// 이 ObjectInstaller 를 루트로 하는 계층 내부에서
    /// [SceneInject], [GlobalInject] 필드를 가진 컴포넌트에
    /// 씬 전용 / 전역 Manager 를 주입합니다.
    /// </summary>
    private void InjectGlobalDependencies()
    {
        var targets = GetComponentsInChildren<MonoBehaviour>(true);

        foreach (var target in targets)
        {
            if (target == null) continue;

            var type = target.GetType();

            // 1) 씬 전용 매니저 주입 ([SceneInject] -> SceneInstaller)
            var sceneFields = TypeDataCache.GetSceneInjectFields(type);
            foreach (var field in sceneFields)
            {
                var resolvedScene = SceneInstaller.Instance != null
                    ? SceneInstaller.Instance.Resolve(field.FieldType) as Component
                    : null;
                if (resolvedScene != null)
                {
                    field.SetValue(target, resolvedScene);
                }
                else
                {
                    Debug.LogWarning($"[ObjectInstaller] Failed to resolve SceneInject for field {field.Name} ({field.FieldType.Name}) on {target.name}");
                }
            }

            // 2) 전역 매니저 주입 ([GlobalInject] -> MasterInstaller)
            var globalFields = TypeDataCache.GetGlobalInjectFields(type);

            foreach (var field in globalFields)
            {
                var resolved = MasterInstaller.Instance != null 
                    ? MasterInstaller.Instance.Resolve(field.FieldType) as Component 
                    : null;
                if (resolved != null)
                {
                    field.SetValue(target, resolved);
                }
                else
                {
                    Debug.LogWarning($"[ObjectInstaller] Failed to resolve GlobalInject for field {field.Name} ({field.FieldType.Name}) on {target.name}");
                }
            }
        }
    }

    // ------------------- Editor (Bake: Inject / GlobalInject) -------------------

#if UNITY_EDITOR

    /// <summary>
    /// 이 ObjectInstaller 를 루트로 하는 계층 내부의 의존성을
    /// 1) 로컬 참조([Inject])는 에디터에서 실제 레퍼런스를 Bake 하고,
    /// 2) 전역 Manager 참조([GlobalInject])는 런타임 DI를 위해
    ///    MasterInstaller 전역 레지스트리만 최신 상태로 갱신합니다.
    /// </summary>
    public void BakeDependencies()
    {
        // 0. 전역 Manager 레지스트리 갱신 (MasterInstaller 가 있을 때에만)
        var master = MasterInstaller.Instance;
        if (master != null)
        {
            master.RefreshRegistry();
        }

        // 1. 공급자(Providers) 스캔 (자식 포함)
        var providers = GetComponentsInChildren<Component>(true).ToList();

        // 2. 수여자(Targets) 스캔 및 주입 (로컬 Inject 전용)
        var targets = GetComponentsInChildren<MonoBehaviour>(true);
        int injectCount = 0;

        foreach (var target in targets)
        {
            if (target == null) continue;

            var fields = target.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                // [Inject] : 로컬(같은 루트 계층)에서 타입이 맞는 컴포넌트 검색 후 Bake
                if (System.Attribute.IsDefined(field, typeof(InjectAttribute)))
                {
                    var match = providers.FirstOrDefault(p => p != target && field.FieldType.IsInstanceOfType(p));
                    if (match != null) SetField(target, field, match, ref injectCount);
                }
            }

            UnityEditor.EditorUtility.SetDirty(target);
        }

        Debug.Log($"<color=#42f56c>[ObjectInstaller]</color> Bake Complete! ({injectCount} links updated)");
    }

    private void SetField(object target, FieldInfo field, Component value, ref int count)
    {
        var current = field.GetValue(target) as Component;
        if (current != value)
        {
            UnityEditor.Undo.RecordObject(target as UnityEngine.Object, "Bake Dependency");
            field.SetValue(target, value);
            count++;
        }
    }
#endif
}