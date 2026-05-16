using QSB2.Messaging;
using QSB2.QObject;
using UnityEngine;

namespace QSB2.PlayerSync;

public class QPlayerBuilder : QObjectBuilder<QPlayer, Transform>
{
    static QPlayerBuilder()
    {
        LeaveMessage.Event += id => { NetworkManager.Connections[id].QPlayer?.Destroy(); };
    }

    public override void Create()
    {
        foreach (var connection in NetworkManager.Connections.Values)
        {
            new QPlayer
            {
                Connection = connection
            }.Create();
        }

        SendCreated<QPlayer>(true);
    }
}