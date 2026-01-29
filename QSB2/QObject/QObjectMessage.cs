using MessagePack;
using QSB2.Messaging;
using QSB2.Utility;

namespace QSB2.QObject;

public abstract class QObjectMessage : Message
{
    [Key(0)] public int Type;
    [Key(1)] public int ID;

    public override void OnReceive(int from, int to)
    {
        var qObject = QObjectManager.Entries[Type].QObjects[ID];
        OnReceive(qObject, from, to);
    }

    public abstract void OnReceive(QObject qObject, int from, int to);
}

// more compact cuz not sending the type
public abstract class QObjectMessage<T> : Message where T : QObject
{
    [Key(0)] public int ID;

    public override void OnReceive(int from, int to)
    {
        var qObject = (T)QObjectManager.Entries[typeof(T).Hash()].QObjects[ID];
        OnReceive(qObject, from, to);
    }

    public abstract void OnReceive(T qObject, int from, int to);
}

/// <summary>
/// signal that weve built these specific qobjects
/// </summary>
[MessagePackObject]
public class QObjectsCreatedMessage : Message
{
    [Key(0)] public required int Type;
    [Key(1)] public required bool Created;

    public override void OnReceive(int from, int to)
    {
        var entry = QObjectManager.Entries[Type];

        Logger.Log($"qobjects type {entry.Type} created");
        if (Created) entry.CreatedFor.Add(from);
        else entry.CreatedFor.Remove(from);
    }
}