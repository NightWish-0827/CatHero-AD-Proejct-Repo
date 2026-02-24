using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// 리플렉션 비용을 줄이기 위해
/// [GlobalInject], [SceneInject] 필드 정보를 타입별로 캐싱하는 헬퍼입니다.
/// </summary>
public static class TypeDataCache
{
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
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
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
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
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

