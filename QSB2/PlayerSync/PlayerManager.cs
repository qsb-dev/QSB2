using System.Linq;
using QSB2.Messaging;
using QSB2.QObject;
using QSB2.QObject.Verify;
using QSB2.Utility;

namespace QSB2.PlayerSync;

public class PlayerManager
{
    static PlayerManager()
    {
        LeaveMessage.Event += id => { NetworkManager.Connections[id].Player?.Destroy(); };
    }

    public static void Create()
    {
        foreach (var connection in NetworkManager.Connections.Values)
        {
            new Player
            {
                Connection = connection
            }.Create();
        }

        new QObjectsCreatedMessage
        {
            Type = typeof(Player).Hash(),
            Created = true,
            Count = QObjectManager.Entries[typeof(Player).Hash()].QObjects.Count,
        }.Send(-1);
    }

    public static void Destroy()
    {
        var entry = QObjectManager.Entries[typeof(Player).Hash()];
        foreach (var qObject in entry.QObjects.Values.ToList())
        {
            qObject.Destroy();
        }

        entry.NextId = 0;

        new QObjectsCreatedMessage
        {
            Type = typeof(Player).Hash(),
            Created = false
        }.Send(-1);
    }
}