using QSB2.Authority;
using QSB2.Messaging;
using QSB2.PositionSync;
using UnityEngine;

namespace QSB2.QObject;

/// <summary>
/// network object that links to an in-game unity component
/// </summary>
public abstract class QObject
{
    public int ID;
    public Component UnityComponent;

    public PositionSync.PositionSync PositionSync;
    public VelocitySync VelocitySync;
    public HasOwner HasOwner;
    public RelativeToSector RelativeToSector;

    public virtual void Create()
    {
        var entry = QObjectManager.Entries[GetType().FullName.GetHashCode()];
        ID = entry.NextId++;
        entry.QObjects.Add(ID, this);
        QObjectManager._componentToObject.Add(UnityComponent, this);
    }

    public virtual void Destroy()
    {
        QObjectManager.Entries[GetType().FullName.GetHashCode()].QObjects.Remove(ID);
        QObjectManager._componentToObject.Remove(UnityComponent);
    }

    // syntax sugar
    public void Send(QObjectMessage message, int to)
    {
        message.Type = GetType().FullName.GetHashCode();
        message.ID = ID;
        message.Send(to);
    }
}