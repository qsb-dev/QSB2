using MessagePack;
using QSB2.QObject;

namespace QSB2.Messaging;

public abstract class QObjectMessage<T> : Message where T : QObject.QObject<T>
{
    [Key(0)] public int ID;

    public override void OnReceive(int from, int to)
    {
        var qObject = (T)QObjectManager.Entries[typeof(T)].QObjects[ID];
        OnReceive(qObject, from, to);
    }

    public abstract void OnReceive(T qObject, int from, int to);
}

/// <summary>
/// signal that weve built these specific qobjects
/// </summary>
[MessagePackObject]
public class QObjectsBuiltMessage<T> : Message where T : QObject.QObject<T>
{
    [Key(1)] public bool Built;

    public override void OnReceive(int from, int to)
    {
        var entry = QObjectManager.Entries[typeof(T)];

        if (Built) entry.BuiltFor.Add(from);
        else entry.BuiltFor.Remove(from);
    }
}