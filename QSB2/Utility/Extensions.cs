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
}