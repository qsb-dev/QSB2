using QSB2.Messaging;
using UnityEngine;

namespace QSB2.QObject;

/// <summary>
/// network object that links to an in-game unity component
/// </summary>
public abstract class QObject : MonoBehaviour
{
    public int ID;
    public Component UnityComponent;

    protected virtual void Start()
    {
        DontDestroyOnLoad(gameObject);
        gameObject.name = $"QObject_{GetType().Name}";

        if (!QObjectManager.Entries.TryGetValue(GetType().FullName.GetHashCode(), out var entry))
            entry = new();
        ID = entry.NextId++;
        entry.QObjects.Add(ID, this);
    }

    protected virtual void OnDestroy()
    {
        QObjectManager.Entries[GetType().FullName.GetHashCode()].QObjects.Remove(ID);
    }

    // syntax sugar
    public void SendMessage(QObjectMessage message, int to = -1)
    {
        message.Type = GetType().FullName.GetHashCode();
        message.ID = ID;
        message.Send(to);
    }
}