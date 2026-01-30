using System;
using MessagePack;
using OWML.Utils;

namespace QSB2.Messaging;

[MessagePackObject]
public class LeaveMessage : Message
{
    [Key(0)] public required int ID;

    /// <summary>
    /// right before connection is removed
    /// </summary>
    public static event Action<int> Event;

    public override void OnReceive(int from, int to)
    {
        Event?.SafeInvoke(ID);
        NetworkManager.Connections.Remove(ID);
        Logger.Log($"{ID} left");
    }
}