using System.Collections.Generic;

namespace QSB2.QObject;

public static class QObjectManager
{
    public class Entry
    {
        public int NextId;
        public readonly Dictionary<int, QObject> Objects = new();
    }

    public readonly static Dictionary<int, Entry> Entries = new();
}