using QSB2.Ownership;
using QSB2.PositionSync;
using QSB2.Utility;
using UnityEngine;

namespace QSB2.QObject;

/// <summary>
/// network object that links to an in-game unity component
/// </summary>
public abstract class QObject
{
    public int ID;
    public Component Component;

    #region mixins

    // TODO: this is already dumb, combine into one thing with a buncha flags probably. there is a DAG of these guys depending on each other already
    public PositionSync.PositionSync PositionSync;
    public VelocitySync VelocitySync;
    public Owner Owner;
    public OwnerQueue OwnerQueue;
    public RelativeToSector RelativeToSector;

    #endregion

    public virtual void Create()
    {
        var entry = QObjectManager.Entries[GetType().Hash()];
        ID = entry.NextId++;
        entry.QObjects.Add(ID, this);
        QObjectManager._componentToObject.Add(Component, this);
    }

    public virtual void Destroy()
    {
        QObjectManager.Entries[GetType().Hash()].QObjects.Remove(ID);
        QObjectManager._componentToObject.Remove(Component);
    }

    // syntax sugar
    public void Send(QObjectMessage message, int to)
    {
        message.Type = GetType().Hash();
        message.ID = ID;
        message.Send(to);
    }

    public void Send<T>(QObjectMessage<T> message, int to) where T : QObject, new()
    {
        message.ID = ID;
        message.Send(to);
    }
}

// convenience thing
public abstract class QObject<T> : QObject where T : Component
{
    public new T Component
    {
        get => (T)base.Component;
        set => base.Component = value;
    }
}