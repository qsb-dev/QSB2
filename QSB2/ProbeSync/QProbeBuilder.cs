using QSB2.Messaging;
using QSB2.QObject;
using UnityEngine;

namespace QSB2.ProbeSync;

public class QProbeBuilder : QObjectBuilder<QProbe, Transform>
{
    static QProbeBuilder()
    {
        LeaveMessage.Event += id => { NetworkManager.Connections[id].QProbe?.Destroy(); };
    }

    public override void Create()
    {
        foreach (var connection in NetworkManager.Connections.Values)
        {
            new QProbe
            {
                Connection = connection
            }.Create();
        }

        SendCreated<QProbe>(true);
    }
}