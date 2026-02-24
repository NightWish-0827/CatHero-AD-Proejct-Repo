using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 리플렉션 비용을 줄이기 위해
/// [GlobalInject], [SceneInject] 필드 정보를 타입별로 캐싱하는 헬퍼입니다.
/// </summary>
public static class TypeDataCache
{
    /// <summary>
    /// 상속 체인을 순회하여 해당 타입 및 베이스 타입의 모든 인스턴스 필드를 반환합니다.
    /// C# GetFields는 상속된 private 필드를 반환하지 않으므로, 베이스 타입을 수동 순회해야 합니다.
    /// - type == null 시 빈 시퀀스 반환 (방어 코드)
    /// - MonoBehaviour 이상의 Unity 엔진 타입은 제외 (불필요한 리플렉션 및 엔진 내부 필드 노출 방지)
    /// </summary>
    public static IEnumerable<FieldInfo> GetAllInstanceFields(Type type)
    {
        if (type == null) yield break;

        var current = type;
        while (current != null && current != typeof(object) && current != typeof(MonoBehaviour))
        {
            var fields = current.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (var f in fields)
                yield return f;
            current = current.BaseType;
        }
    }
    private static readonly Dictionary<Type, List<FieldInfo>> _globalInjectCache
        = new Dictionary<Type, List<FieldInfo>>();

    private static readonly Dictionary<Type, List<FieldInfo>> _sceneInjectCache
        = new Dictionary<Type, List<FieldInfo>>();

    /// <summary>
    /// 해당 타입에서 [GlobalInject] 가 붙은 필드 목록을 반환합니다. (캐싱됨)
    /// </summary>
    public static List<FieldInfo> GetGlobalInjectFields(Type type)
    {
        if (type == null) return new List<FieldInfo>();

        if (_globalInjectCache.TryGetValue(type, out var cached))
        {
            return cached;
        }

        var result = new List<FieldInfo>();
        foreach (var field in GetAllInstanceFields(type))
        {
            if (Attribute.IsDefined(field, typeof(GlobalInjectAttribute)))
            {
                result.Add(field);
            }
        }

        _globalInjectCache[type] = result;
        return result;
    }

    /// <summary>
    /// 해당 타입에서 [SceneInject] 가 붙은 필드 목록을 반환합니다. (캐싱됨)
    /// </summary>
    public static List<FieldInfo> GetSceneInjectFields(Type type)
    {
        if (type == null) return new List<FieldInfo>();

        if (_sceneInjectCache.TryGetValue(type, out var cached))
        {
            return cached;
        }

        var result = new List<FieldInfo>();
        foreach (var field in GetAllInstanceFields(type))
        {
            if (Attribute.IsDefined(field, typeof(SceneInjectAttribute)))
            {
                result.Add(field);
            }
        }

        _sceneInjectCache[type] = result;
        return result;
    }
}

