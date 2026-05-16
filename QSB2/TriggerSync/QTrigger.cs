using System.Collections.Generic;
using MessagePack;
using QSB2.Messaging;
using QSB2.PlayerSync;
using QSB2.QObject;
using UnityEngine;

namespace QSB2.TriggerSync;

public class QTrigger : QObject<OWTriggerVolume>
{
    public List<QPlayer> Occupants = new();

    public override void Create()
    {
        base.Create();

        Component.OnEntry += OnEntry;
        Component.OnExit += OnExit;

        // you started in volume = everyone started in that volume i think
        if (Component.IsTrackingObject(Locator.GetPlayerDetector()))
        {
            foreach (var qPlayer in QObjectManager.GetQObjects<QPlayer>())
            {
                OnEntry(qPlayer);
            }
        }
    }

    public virtual void OnEntry(QPlayer qPlayer)
    {
        Occupants.SafeAdd(qPlayer);
    }

    public virtual void OnExit(QPlayer qPlayer)
    {
        Occupants.Remove(qPlayer);
    }

    static QTrigger()
    {
        LeaveMessage.Event += id =>
        {
            var qPlayer = NetworkManager.Connections[id].QPlayer;
            foreach (var qTrigger in QObjectManager.GetQObjects<QTrigger>())
            {
                qTrigger.Occupants.Remove(qPlayer);
            }
        };
    }

    private void OnEntry(GameObject hitObj)
    {
        Send(new TriggerMessage
        {
            Enter = true
        }, -1);
    }

    private void OnExit(GameObject hitObj)
    {
        Send(new TriggerMessage
        {
            Enter = false
        }, -1);
    }
}

[MessagePackObject]
public class TriggerMessage : QObjectMessage<QTrigger>
{
    [Key(1)] public required bool Enter;

    public override void OnReceive(QTrigger qObject, int from, int to)
    {
        if (Enter) qObject.OnEntry(NetworkManager.Connections[from].QPlayer);
        else qObject.OnExit(NetworkManager.Connections[from].QPlayer);
    }
}

public class QTriggerBuilder : QObjectBuilder<QTrigger, OWTriggerVolume>
{
    public override void Create()
    {
        SendCreated<QTrigger>(true);
    }

    public override void Destroy()
    {
        SendCreated<QTrigger>(false);
    }
}