using MessagePack;
using QSB2.QObject;

namespace QSB2.Messaging;

public abstract class QObjectMessage : Message
{
    [Key(0)] public int Type;
    [Key(1)] public int ID;

    public override void OnReceive(int from, int to)
    {
        var qObject = QObjectManager.Entries[Type].QObjects[ID];
        OnReceive(qObject, from, to);
    }

    public abstract void OnReceive(QObject.QObject qObject, int from, int to);
}