using UnityEngine;

namespace QSB2.QObject;

public abstract class QObject : MonoBehaviour
{
    public int ID;

    protected virtual void Start()
    {
        DontDestroyOnLoad(gameObject);

        if (!QObjectManager.Entries.TryGetValue(GetType().GetHashCode(), out var entry))
            entry = new();
        ID = entry.NextId++;
        entry.Objects.Add(ID, this);
    }

    protected virtual void OnDestroy()
    {
        QObjectManager.Entries[GetType().GetHashCode()].Objects.Remove(ID);
    }
}