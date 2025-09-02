using System;
using System.Collections.Generic;

public static class Services
{
    static readonly Dictionary<Type, object> map = new Dictionary<Type, object>();

    public static void Add<T>(T s) where T : class { map[typeof(T)] = s; }
    public static void Add(Type t, object s)            { map[t] = s; }

    public static T Get<T>() where T : class
    {
        map.TryGetValue(typeof(T), out var s);
        return (T)s;
    }

    public static object Get(Type t)
    {
        map.TryGetValue(t, out var s);
        return s;
    }

    public static bool Has<T>() => map.ContainsKey(typeof(T));
    public static void Remove<T>() => map.Remove(typeof(T));
    public static void Clear() => map.Clear();
}
