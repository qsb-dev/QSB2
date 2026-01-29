using QSB2.Authority;
using QSB2.Messaging;
using QSB2.PositionSync;
using UnityEngine;

namespace QSB2.QObject;

public interface IQObject
{
    void Create();
    void Destroy();
}

/// <summary>
/// network object that links to an in-game unity component
/// </summary>
public abstract class QObject<T> : IQObject where T : QObject<T>
{
    public int ID;
    public Component UnityComponent;

    public PositionSync.PositionSync<T> PositionSync;
    public VelocitySync<T> VelocitySync;
    public HasOwner<T> HasOwner;
    public RelativeToSector<T> RelativeToSector;

    public virtual void Create()
    {
        var entry = QObjectManager.Entries[GetType()];
        ID = entry.NextId++;
        entry.QObjects.Add(ID, this);
        QObjectManager._componentToObject.Add(UnityComponent, this);
    }

    public virtual void Destroy()
    {
        QObjectManager.Entries[GetType()].QObjects.Remove(ID);
        QObjectManager._componentToObject.Remove(UnityComponent);
    }

    // syntax sugar
    public void Send(QObjectMessage<T> message, int to)
    {
        message.ID = ID;
        message.Send(to);
    }
}