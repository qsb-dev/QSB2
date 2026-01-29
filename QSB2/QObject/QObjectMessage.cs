using MessagePack;
using QSB2.Messaging;

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

/// <summary>
/// signal that weve built these specific qobjects
/// </summary>
[MessagePackObject]
public class QObjectsBuiltMessage : Message
{
    [Key(0)] public int Type;
    [Key(1)] public bool Built;

    public override void OnReceive(int from, int to)
    {
        var entry = QObjectManager.Entries[Type];

        if (Built) entry.BuiltFor.Add(from);
        else entry.BuiltFor.Remove(from);
    }
}