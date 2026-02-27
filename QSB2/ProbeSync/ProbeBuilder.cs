using System.Linq;
using QSB2.Messaging;
using QSB2.QObject;
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

        SendCreated<Probe>(true);
    }

    public override void Destroy()
    {
        var entry = QObjectManager.Entries[typeof(Probe).Hash()];
        foreach (var qObject in entry.QObjects.Values.ToList())
        {
            qObject.Destroy();
        }

        entry.NextId = 0;

        SendCreated<Probe>(false);
    }
}