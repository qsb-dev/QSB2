using System;
using System.Collections.Generic;
using QSB2.Utility;
using UnityEngine;

namespace QSB2.QObject;

public static class QObjectManager
{
    public class Entry
    {
        public int NextId;
        public readonly Dictionary<int, IQObject> QObjects = new();
        public List<int> BuiltFor;
    }

    public static readonly Dictionary<Type, Entry> Entries = new();

    public static readonly Dictionary<Component, IQObject> _componentToObject = new();

    static QObjectManager()
    {
        foreach (var type in typeof(IQObject).GetDerivedTypes())
        {
            Entries.Add(type, new());
        }
    }
}