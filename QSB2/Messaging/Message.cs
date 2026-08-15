using System;
using MessagePack;
using QSB2.Utility;
using SteamTransport;

namespace QSB2.Messaging;

public abstract class Message
{
    /// <summary>
    /// are we currently receiving a message?
    /// </summary>
    protected static bool Receiving => MessageManager.Receiving;

    public void Send(int to, int channelId = Channels.Reliable)
    {
        var rawMessage = new RawMessage
        {
            From = NetworkManager.LocalID,
            To = to,
            Type = GetType().Hash(),
            Message = MessagePackSerializer.Serialize(GetType(), this),
        };

        var data = new ArraySegment<byte>(MessagePackSerializer.Serialize(rawMessage));
        if (MessageManager.DoMessageLoopback)
        {
            if (to == -1)
            {
                MessageManager.OnData(data, channelId); // send it to self also. then server will handle sending it to rest
            }
            else if (to == -2)
            {
                // server routing below does broadcast to everyone else
            }
            else if (to == NetworkManager.LocalID)
            {
                MessageManager.OnData(data, channelId); // pass it to self without going thru server
                return;
            }

            if (NetworkManager.IsHost)
                MessageManager.OnServerData(0, data, channelId); // we are the server. we route
            else
                NetworkManager._client.Send(data, channelId); // send it to server, which will route 
        }
        else
        {
            if (NetworkManager.IsHost)
                MessageManager.OnServerData(0, data, channelId); // we are the server. we route
            else
                NetworkManager._client.Send(data, channelId); // send it to server, which will route 
        }
    }

    public abstract void OnReceive(int from, int to);
}