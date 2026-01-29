using QSB2.Messaging;
using QSB2.QObject;
using QSB2.Utility;

namespace QSB2.Player;

public class PlayerManager
{
    static PlayerManager()
    {
        LeaveMessage.Event += id =>
        {
            var connection = NetworkManager.Connections[id];
            connection.Player.Destroy();
            connection.Player = null;
        };
    }
    
    public static void Create()
    {
        foreach (var connection in NetworkManager.Connections.Values)
        {
            connection.Player = new();
            connection.Player.Connection = connection;
            connection.Player.Create();
        }

        new QObjectsCreatedMessage
        {
            Type = typeof(Player).Hash(),
            Created = true
        }.Send(-1);
    }

    public static void Destroy()
    {
        foreach (var connection in NetworkManager.Connections.Values)
        {
            connection.Player.Destroy();
            connection.Player = null;
        }

        new QObjectsCreatedMessage
        {
            Type = typeof(Player).Hash(),
            Created = false
        }.Send(-1);
    }
}