using System;
using System.Collections.Generic;
using MessagePack;
using QSB2.Utility;

namespace QSB2.Messaging;

public static class MessageManager
{
    private static bool Receiving;
    
    private static readonly Dictionary<int, Type> _hashToType = new();

    static MessageManager()
    {
        foreach (var type in typeof(Message).GetDerivedTypes())
        {
            _hashToType.Add(type.FullName.GetHashCode(), type);
        }
    }

    public static void OnData(ArraySegment<byte> data)
    {
        var rawMessage = MessagePackSerializer.Deserialize<RawMessage>(data);
        var type = _hashToType[rawMessage.Type];
        var message = (Message)MessagePackSerializer.Deserialize(type, rawMessage.Message)!;
        Receiving = true;
        message.OnReceive(rawMessage.From, rawMessage.To);
        Receiving = false;
    }

    public static void OnServerData(int fromID, ArraySegment<byte> data)
    {
        var rawMessage = MessagePackSerializer.Deserialize<RawMessage>(data);
        if (rawMessage.To == -1)
        {
            foreach (var toID in NetworkManager._serverClients)
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