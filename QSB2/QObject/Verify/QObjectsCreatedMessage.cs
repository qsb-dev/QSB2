using System.Linq;
using MessagePack;
using OWML.Common;
using QSB2.Messaging;
using QSB2.WakeUpSync;

namespace QSB2.QObject.Verify;

/// <summary>
/// signal that weve created or destroyed these specific qobjects
/// </summary>
[MessagePackObject]
public class QObjectsCreatedMessage : Message
{
    [Key(0)] public required int Type;
    [Key(1)] public required bool Created;

    public override void OnReceive(int from, int to)
    {
        var connection = NetworkManager.Connections[from];
        var type = QObjectManager.Entries[Type].Type;
        Logger.Log($"qobjects type {type} created = {Created} for {from}", MessageType.Info);

        if (Created) connection.QObjectsCreated.Add(type);
        else connection.QObjectsCreated.Remove(type);

        WakeUpManager.AllQObjectsCreated = NetworkManager.Connections.Values.All(x => x.QObjectsCreated.Count == QObjectManager.Entries.Count);
    }
}