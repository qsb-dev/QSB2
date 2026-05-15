using System;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Unity;
using OWML.Common;
using QSB2.Patches;
using QSB2.Utility;

namespace QSB2.Messaging;

public static class MessageManager
{
    /// <summary>
    /// should messages to self skip routing thru the server?
    ///
    /// true makes local messages receive immediately but it breaks message ordering guarantee that steam does between all clients
    /// </summary>
    public const bool DoMessageLoopback = true;

    private static readonly Dictionary<int, Type> _hashToType = new();

    static MessageManager()
    {
        foreach (var type in typeof(Message).GetDerivedTypes())
        {
            _hashToType.Add(type.Hash(), type);
        }

        MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(UnityResolver.InstanceWithStandardResolver);
    }

    public static void OnData(ArraySegment<byte> data, int channelId)
    {
        try
        {
            var rawMessage = MessagePackSerializer.Deserialize<RawMessage>(data);
            var type = _hashToType[rawMessage.Type];
            var message = (Message)MessagePackSerializer.Deserialize(type, rawMessage.Message)!;
            QPatch.Remote = rawMessage.From != NetworkManager.LocalID;
            message.OnReceive(rawMessage.From, rawMessage.To);
        }
        catch (Exception e)
        {
            Logger.Log(e.ToString(), MessageType.Error);
        }

        QPatch.Remote = false;
    }

    public static void OnServerData(int fromID, ArraySegment<byte> data, int channelId)
    {
        var rawMessage = MessagePackSerializer.Deserialize<RawMessage>(data);
        if (DoMessageLoopback)
        {
            if (rawMessage.To is -1 or -2)
            {
                // client will handle OnData for itself if needed
                // this just sends to everyone else   
                if (fromID != 0) OnData(data, channelId); // we are server. send it to us also
                foreach (var id in NetworkManager._serverClients)
                {
                    if (fromID == id) continue;
                    NetworkManager._server.Send(id, data, channelId);
                }
            }
            else
            {
                if (rawMessage.To == 0)
                    OnData(data, channelId); // we are server. pass to self
                else
                    NetworkManager._server.Send(rawMessage.To, data, channelId);
            }
        }
        else
        {
            if (rawMessage.To == -1)
            {
                OnData(data, channelId); // we are server. send it to us also
                foreach (var id in NetworkManager._serverClients)
                {
                    NetworkManager._server.Send(id, data, channelId);
                }
            }
            else if (rawMessage.To == -2)
            {
                if (fromID != 0) OnData(data, channelId); // we are server. send it to us also
                foreach (var id in NetworkManager._serverClients)
                {
                    if (fromID == id) continue;
                    NetworkManager._server.Send(id, data, channelId);
                }
            }
            else
            {
                if (rawMessage.To == 0)
                    OnData(data, channelId); // we are server. pass to self
                else
                    NetworkManager._server.Send(rawMessage.To, data, channelId);
            }
        }
    }
}