using System;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Unity;
using OWML.Common;
using QSB2.Utility;
using SteamTransport;

namespace QSB2.Messaging;

public static class MessageManager
{
    private static readonly Dictionary<int, Type> _hashToType = new();

    static MessageManager()
    {
        foreach (var type in typeof(Message).GetDerivedTypes())
        {
            _hashToType.Add(type.Hash(), type);
        }

        MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(UnityResolver.InstanceWithStandardResolver);
    }

    public static void OnData(ArraySegment<byte> data)
    {
        try
        {
            var rawMessage = MessagePackSerializer.Deserialize<RawMessage>(data);
            var type = _hashToType[rawMessage.Type];
            var message = (Message)MessagePackSerializer.Deserialize(type, rawMessage.Message)!;
            message.OnReceive(rawMessage.From, rawMessage.To);
        }
        catch (Exception e)
        {
            Logger.Log(e.ToString(), MessageType.Error);
        }
    }

    public static void OnServerData(int fromID, ArraySegment<byte> data)
    {
        var rawMessage = MessagePackSerializer.Deserialize<RawMessage>(data);
        if (rawMessage.To == -1)
        {
            OnData(data); // send to self too
            foreach (var id in NetworkManager._serverClients)
            {
                NetworkManager._server.Send(id, data, Util.Channels.Reliable);
            }
        }
        else if (rawMessage.To == -2)
        {
            if (fromID != 0) OnData(data); // send to self too
            foreach (var id in NetworkManager._serverClients)
            {
                if (fromID == id) continue;
                NetworkManager._server.Send(id, data, Util.Channels.Reliable);
            }
        }
        else
        {
            if (rawMessage.To == 0)
                OnData(data); // just pass it to self
            else 
                NetworkManager._server.Send(rawMessage.To, data, Util.Channels.Reliable);
        }
    }
}