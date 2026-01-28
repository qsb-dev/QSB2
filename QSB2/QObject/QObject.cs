using QSB2.Messaging;
using UnityEngine;

namespace QSB2.QObject;

/// <summary>
/// network object that links to an in-game unity component
/// </summary>
public abstract class QObject : MonoBehaviour
{
    public int ID;
    public MonoBehaviour UnityComponent;
    /// <summary>
    /// for player and probe. created uhh jfsafgasgksagksl
    /// </summary>
    public bool OwnsUnityObject;

    protected virtual void Start()
    {
        DontDestroyOnLoad(gameObject);

        if (!QObjectManager.Entries.TryGetValue(GetType().FullName.GetHashCode(), out var entry))
            entry = new();
        ID = entry.NextId++;
        entry.QObjects.Add(ID, this);
    }

    protected virtual void OnDestroy()
    {
        QObjectManager.Entries[GetType().FullName.GetHashCode()].QObjects.Remove(ID);
        
        if (OwnsUnityObject) Destroy(UnityComponent.gameObject);
    }

    public void SendMessage(QObjectMessage message, int to = -1)
    {
        message.Type = GetType().FullName.GetHashCode();
        message.ID = ID;
        message.Send(to);
    }

    public virtual void OnReceiveMessage(QObjectMessage message, int from, int to)
    {
    }
}