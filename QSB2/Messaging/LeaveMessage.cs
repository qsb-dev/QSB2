using System;
using MessagePack;
using QSB2.Utility;

namespace QSB2.Messaging;

[MessagePackObject]
public class LeaveMessage : Message
{
    [Key(0)] public required int ID;

    public static event Action<int> Event;

    public override void OnReceive(int from, int to)
    {
        Event?.QSafeInvoke(ID);
        NetworkManager.Connections.Remove(ID);
        Logger.Log($"{ID} disconnected");
    }
}