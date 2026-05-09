using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QSB2.Utility;

public static class Extensions
{
    public static Quaternion TransformRotation(this Transform transform, Quaternion localRotation)
        => transform.rotation * localRotation;

    public static GameObject InstantiateInactive(this GameObject original)
    {
        if (!original.activeSelf)
        {
            return GameObject.Instantiate(original);
        }

        original.SetActive(false);
        var copy = GameObject.Instantiate(original);
        original.SetActive(true);
        return copy;
    }

    public static IEnumerable<Type> GetDerivedTypes(this Type type)
    {
        return type.Assembly.GetTypes()
            .Where(x => !x.IsInterface && !x.IsAbstract && type.IsAssignableFrom(x))
            .OrderBy(x => x.FullName);
    }

    public static IEnumerable<T> GetAllComponents<T>() where T : Component
        => Resources.FindObjectsOfTypeAll<T>()
            .Where(x => x.gameObject.scene.name is not (null or "DontDestroyOnLoad"));

    public static int Hash(this Type type) => type.FullName.GetHashCode();


    public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
    {
        var comparer = Comparer<TKey>.Default;
        var yk = default(TKey);
        var y = default(TSource);
        var hasValue = false;
        foreach (var x in source)
        {
            var xk = keySelector(x);
            if (!hasValue)
            {
                hasValue = true;
                yk = xk;
                y = x;
            }
            else if (comparer.Compare(xk, yk) < 0)
            {
                yk = xk;
                y = x;
            }
        }

        if (!hasValue)
        {
            throw new InvalidOperationException("Sequence contains no elements");
        }

        return y;
    }

    public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
    {
        var comparer = Comparer<TKey>.Default;
        var yk = default(TKey);
        var y = default(TSource);
        var hasValue = false;
        foreach (var x in source)
        {
            var xk = keySelector(x);
            if (!hasValue)
            {
                hasValue = true;
                yk = xk;
                y = x;
            }
            else if (comparer.Compare(xk, yk) > 0)
            {
                yk = xk;
                y = x;
            }
        }

        if (!hasValue)
        {
            throw new InvalidOperationException("Sequence contains no elements");
        }

        return y;
    }
}