using System.Linq;
using MessagePack;
using OWML.Common;
using QSB2.Messaging;
using QSB2.Utility;
using QSB2.WakeUpSync;

namespace QSB2.QObject;

public abstract class QObjectMessage : Message
{
    [Key(0)] public int Type;
    [Key(1)] public int ID;

    public override void OnReceive(int from, int to)
    {
        var entry = QObjectManager.Entries[Type];
        if (!entry.QObjects.TryGetValue(ID, out var qObject))
        {
            Logger.Log($"received message {GetType()} with unknown qobject type {entry.Type} id {ID}", MessageType.Error);
            return;
        }

        OnReceive(qObject, from, to);
    }

    public abstract void OnReceive(QObject qObject, int from, int to);
}

// more compact cuz not sending the type
public abstract class QObjectMessage<T> : Message where T : QObject, new() // non abstract
{
    [Key(0)] public int ID;

    public override void OnReceive(int from, int to)
    {
        var entry = QObjectManager.Entries[typeof(T).Hash()];
        if (!entry.QObjects.TryGetValue(ID, out var qObject))
        {
            Logger.Log($"received message {GetType()} with unknown qobject type {entry.Type} id {ID}", MessageType.Error);
            return;
        }

        OnReceive((T)qObject, from, to);
    }

    public abstract void OnReceive(T qObject, int from, int to);
}

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