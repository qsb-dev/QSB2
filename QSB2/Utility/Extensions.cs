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


    public static float Map(this float value, float inputFrom, float inputTo, float outputFrom, float outputTo, bool clamp)
    {
        var mappedValue = (value - inputFrom) / (inputTo - inputFrom) * (outputTo - outputFrom) + outputFrom;

        return clamp
            ? Mathf.Clamp(mappedValue, outputTo, outputFrom)
            : mappedValue;
    }


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

    extension(Quaternion)
    {
        // Stolen from here: https://gist.github.com/maxattack/4c7b4de00f5c1b95a33b
        public static Quaternion SmoothDamp(Quaternion rot, Quaternion target, ref Quaternion deriv, float time)
        {
            if (Time.deltaTime < Mathf.Epsilon)
            {
                return rot;
            }

            // account for double-cover
            var dot = Quaternion.Dot(rot, target);
            var multi = dot > 0f ? 1f : -1f;
            target.x *= multi;
            target.y *= multi;
            target.z *= multi;
            target.w *= multi;
            // smooth damp (nlerp approx)
            var result = new Vector4(
                Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, time),
                Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, time),
                Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, time),
                Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, time)
            ).normalized;

            // ensure deriv is tangent
            var derivError = Vector4.Project(new Vector4(deriv.x, deriv.y, deriv.z, deriv.w), result);
            deriv.x -= derivError.x;
            deriv.y -= derivError.y;
            deriv.z -= derivError.z;
            deriv.w -= derivError.w;

            return new Quaternion(result.x, result.y, result.z, result.w);
        }
    }

    extension(System.Random random)
    {
        public int Range(int minInclusive, int maxExclusive) => random.Next(minInclusive, maxExclusive);
        public float Range(float minInclusive, float maxInclusive) => minInclusive + (maxInclusive - minInclusive) * (float)random.NextDouble();
    }
}