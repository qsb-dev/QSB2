using System.Linq;
using QSB2.Messaging;
using QSB2.QObject;
using QSB2.QObject.Verify;
using QSB2.Utility;

namespace QSB2.PlayerSync;

public class QPlayerBuilder : QObjectBuilder
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

    public override void Destroy()
    {
        var entry = QObjectManager.Entries[typeof(QPlayer).Hash()];
        foreach (var qObject in entry.QObjects.Values.ToList())
        {
            qObject.Destroy();
        }

        entry.NextId = 0;

        SendCreated<QPlayer>(false);
    }
}