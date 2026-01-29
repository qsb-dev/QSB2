using QSB2.Messaging;
using UnityEngine;

namespace QSB2.QObject;

/// <summary>
/// network object that links to an in-game unity component
/// </summary>
// TODO: dont use unity here, we can just include fields for additional behavior classes
public abstract class QObject : MonoBehaviour
{
    public int ID;
    public Component UnityComponent;

    protected virtual void Start()
    {
        DontDestroyOnLoad(gameObject);
        gameObject.name = $"QObject_{GetType().Name}";

        if (!QObjectManager.Entries.TryGetValue(GetType().FullName.GetHashCode(), out var entry))
        {
            entry = new();
            QObjectManager.Entries.Add(GetType().FullName.GetHashCode(), entry);
        }

        ID = entry.NextId++;
        entry.QObjects.Add(ID, this);
        QObjectManager._componentToObject.Add(UnityComponent, this);
    }

    protected virtual void OnDestroy()
    {
        QObjectManager.Entries[GetType().FullName.GetHashCode()].QObjects.Remove(ID);
        QObjectManager._componentToObject.Remove(UnityComponent);
    }

    // syntax sugar
    public void SendMessage(QObjectMessage message, int to = -1)
    {
        message.Type = GetType().FullName.GetHashCode();
        message.ID = ID;
        message.Send(to);
    }
}