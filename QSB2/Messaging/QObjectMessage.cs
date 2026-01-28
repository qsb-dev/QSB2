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
        qObject.OnReceiveMessage(this, from, to);
    }
}