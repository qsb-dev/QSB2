using System;
using MessagePack;
using OWML.Utils;

namespace QSB2.Messaging;

[MessagePackObject]
public class JoinMessage : Message
{
    [Key(0)] public required int ID;
    [Key(1)] public required string Name;

    /// <summary>
    /// right after connection is added
    /// </summary>
    public static event Action<int> Event;

    public override void OnReceive(int from, int to)
    {
        NetworkManager.Connections.Add(ID, new(ID, Name));
        NetworkManager.ConnectionIDs.Add(ID);
        Event?.SafeInvoke(ID);
        Logger.Log($"{ID} joined");
    }
}