using System;
using System.Collections.Generic;
using MessagePack;
using QSB2.Utility;

namespace QSB2.Messaging;

public static class MessageManager
{
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
        message.OnReceive(rawMessage.From, rawMessage.To);
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

public abstract class Message
{
    public void Send(int to = -1)
    {
        var rawMessage = new RawMessage
        {
            From = NetworkManager.LocalID,
            To = to,
            Type = GetType().FullName.GetHashCode(),
            Message = MessagePackSerializer.Serialize(GetType(), this),
        };

        var data = new ArraySegment<byte>(MessagePackSerializer.Serialize(rawMessage));
        if (this is JoinMessage or LeaveMessage)
        {
            // special broadcast message hack since we might not have _client yet
            foreach (var id in NetworkManager._serverClients)
                NetworkManager._server.Send(id, data);
        }
        else
        {
            NetworkManager._client.Send(data);
        }
    }

    public abstract void OnReceive(int from, int to);
}

[MessagePackObject]
public class JoinMessage : Message
{
    [Key(0)] public required string QSBVersion;
    [Key(1)] public required string GameVersion;
    [Key(2)] public required bool DLCInstalled;
    [Key(3)] public required int ID;

    public override void OnReceive(int from, int to)
    {
        var leave = false;
        if (QSBVersion != QSB2.QSBVersion) leave = true;
        if (GameVersion != QSB2.GameVersion) leave = true;
        if (DLCInstalled != QSB2.DLCInstalled) leave = true;
        NetworkManager.LocalID = ID;
        if (leave) NetworkManager.Disconnect();
        
        NetworkManager.Clients.Add(ID);
        Logger.Log($"{ID} connected");
    }
}

[MessagePackObject]
public class LeaveMessage : Message
{
    [Key(0)] public required int ID;

    public override void OnReceive(int from, int to)
    {
        NetworkManager.Clients.Remove(ID);
        Logger.Log($"{ID} disconnected");
    }
}