using System;
using MessagePack;
using OWML.Utils;

namespace QSB2.Messaging;

[MessagePackObject]
public class LeaveMessage : Message
{
    [Key(0)] public required int ID;

    public static event Action<int> Event;

    public override void OnReceive(int from, int to)
    {
        NetworkManager.Connections.Remove(ID);
        Event?.SafeInvoke(ID);
        Logger.Log($"{ID} disconnected");
    }
}