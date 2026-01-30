using System;
using System.Linq;
using MessagePack;
using OWML.Utils;
using QSB2.QObject;
using QSB2.WakeUpSync;

namespace QSB2.Messaging;

[MessagePackObject]
public class LeaveMessage : Message
{
    [Key(0)] public required int ID;

    public static event Action<int> Event;

    public override void OnReceive(int from, int to)
    {
        Event?.SafeInvoke(ID);
        NetworkManager.Connections.Remove(ID);
        Logger.Log($"{ID} left");

        // TODO: very stupid
        WakeUpManager.AllQObjectsCreated = NetworkManager.Connections.Values.All(x => x.QObjectsCreated.Count == QObjectManager.Entries.Count);
    }
}