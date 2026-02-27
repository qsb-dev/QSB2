using System.Linq;
using QSB2.Messaging;
using QSB2.QObject;
using QSB2.QObject.Verify;
using QSB2.Utility;

namespace QSB2.ProbeSync;

public class ProbeBuilder : QObjectBuilder
{
    static ProbeBuilder()
    {
        LeaveMessage.Event += id => { NetworkManager.Connections[id].Probe?.Destroy(); };
    }

    public override void Create()
    {
        foreach (var connection in NetworkManager.Connections.Values)
        {
            new Probe
            {
                Connection = connection
            }.Create();
        }

        new QObjectsCreatedMessage
        {
            Type = typeof(Probe).Hash(),
            Created = true,
            Count = QObjectManager._entries[typeof(Probe).Hash()].QObjects.Count,
        }.Send(-1);
    }

    public override void Destroy()
    {
        var entry = QObjectManager._entries[typeof(Probe).Hash()];
        foreach (var qObject in entry.QObjects.Values.ToList())
        {
            qObject.Destroy();
        }

        entry.NextId = 0;

        new QObjectsCreatedMessage
        {
            Type = typeof(Probe).Hash(),
            Created = false
        }.Send(-1);
    }
}