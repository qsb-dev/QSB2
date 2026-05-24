using System.Linq;
using MessagePack;
using OWML.Common;
using QSB2.Messaging;
using QSB2.Patches;
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
    [Key(2)] public int Count;

    public override void OnReceive(int from, int to)
    {
        var connection = NetworkManager.Connections[from];
        var type = QObjectManager.Entries[Type].Type;

        if (Created)
        {
            Logger.Log($"qobjects type {type} CREATED count {Count} for {from}", MessageType.Info);
            connection.QObjectsCreated.Add(type, Count);
        }
        else
        {
            Logger.Log($"qobjects type {type} DESTROYED for {from}", MessageType.Info);
            connection.QObjectsCreated.Remove(type);
        }

        WakeUpManager.RecalcAllSameFlags();
    }
}