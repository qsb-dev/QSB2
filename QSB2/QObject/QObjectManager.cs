using System.Collections.Generic;
using UnityEngine;

namespace QSB2.QObject;

public static class QObjectManager
{
    public class Entry
    {
        public int NextId;
        public readonly Dictionary<int, QObject> QObjects = new();
    }

    public static readonly Dictionary<int, Entry> Entries = new();

    public static readonly Dictionary<Component, QObject> _componentToObject = new();
}