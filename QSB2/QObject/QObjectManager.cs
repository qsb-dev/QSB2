using System;
using System.Collections.Generic;
using QSB2.Utility;
using UnityEngine;

namespace QSB2.QObject;

public static class QObjectManager
{
    public class Entry(Type type)
    {
        public Type Type = type;
        public int NextId;
        public readonly Dictionary<int, QObject> QObjects = new();
        public readonly HashSet<int> CreatedFor = new();
    }

    public static readonly Dictionary<int, Entry> Entries = new();

    public static readonly Dictionary<Component, QObject> _componentToObject = new();

    static QObjectManager()
    {
        foreach (var type in typeof(QObject).GetDerivedTypes())
        {
            Entries.Add(type.Hash(), new(type));
        }
    }
}