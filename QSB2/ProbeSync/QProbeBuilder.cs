using System.Linq;
using QSB2.Messaging;
using QSB2.QObject;
using QSB2.Utility;

namespace QSB2.ProbeSync;

public class QProbeBuilder : QObjectBuilder
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

    public override void Destroy()
    {
        var entry = QObjectManager.Entries[typeof(QProbe).Hash()];
        foreach (var qObject in entry.QObjects.Values.ToList())
        {
            qObject.Destroy();
        }

        entry.NextId = 0;

        SendCreated<QProbe>(false);
    }
}