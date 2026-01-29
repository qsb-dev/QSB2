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
        var entry = QObjectManager.Entries[Type];

        Logger.Log($"qobjects type {entry.Type} created = {Created} for {from}", MessageType.Info);
        if (Created) entry.CreatedFor.Add(from);
        else entry.CreatedFor.Remove(from);

        WakeUpManager.AllQObjectsCreated = QObjectManager.Entries.Values.All(x => x.CreatedFor.Count == NetworkManager.Connections.Count);
    }
}