using System;
using System.Collections.Generic;
using MessagePack;

namespace QSB2.Messaging;

public static class MessageManager
{
    private static readonly Dictionary<int, Type> _hashToType = new();

    public static void OnData(ArraySegment<byte> data)
    {
        var rawMessage = MessagePackSerializer.Deserialize<RawMessage>(data);
        var type = _hashToType[rawMessage.Type];
        var message = (Message)MessagePackSerializer.Deserialize(type, rawMessage.Message)!;
        message.OnReceive(rawMessage.From, rawMessage.To);
    }

    public static void OnServerData(int fromID, ArraySegment<byte> data)
    {
        var rawMessage = MessagePackSerializer.Deserialize<RawMessage>(data);
        if (rawMessage.To == -1)
        {
            foreach (var toID in NetworkManager.ServerClients)
            {
                if (fromID == toID) continue;
                NetworkManager._server.Send(toID, data);
            }
        }
        else
        {
            NetworkManager._server.Send(rawMessage.To, data);
        }
    }
}

[MessagePackObject]
public struct RawMessage
{
    [Key(0)] public required int From;
    [Key(1)] public required int To;
    [Key(2)] public required int Type;
    [Key(3)] public required byte[] Message;
    // in case message does bs, we dont need to deal with that when forwarding from the server
    // also keeps it open instead of closed union
}

public abstract class Message
{
    public void Send(int to = -1)
    {
        var rawMessage = new RawMessage
        {
            From = NetworkManager.LocalID,
            To = to,
            Type = GetType().GetHashCode(),
            Message = MessagePackSerializer.Serialize(GetType(), this),
        };

        var data = new ArraySegment<byte>(MessagePackSerializer.Serialize(rawMessage));
        if (this is JoinMessage)
        {
            // special bc cuz we dont have a local id or _client yet
            NetworkManager._server.Send(to, data);
        }
        else
        {
            NetworkManager._client.Send(data);
        }
    }

    public abstract void OnReceive(int from, int to);
}

/// <summary>
/// host sends. we validate
/// </summary>
[MessagePackObject]
public class JoinMessage : Message
{
    [Key(0)] public required string QSBVersion;
    [Key(1)] public required string GameVersion;
    [Key(2)] public required bool DLCInstalled;

    public override void OnReceive(int from, int to)
    {
        if (QSBVersion != QSB2.QSBVersion) return;
        if (GameVersion != QSB2.GameVersion) return;
        if (DLCInstalled != QSB2.DLCInstalled) return;
        NetworkManager.LocalID = to;
    }
}