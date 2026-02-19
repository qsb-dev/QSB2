using System;
using MessagePack;
using QSB2.Utility;
using SteamTransport;

namespace QSB2.Messaging;

public abstract class Message
{
    public void Send(int to, bool unreliable = false)
    {
        var rawMessage = new RawMessage
        {
            From = NetworkManager.LocalID,
            To = to,
            Type = GetType().Hash(),
            Message = MessagePackSerializer.Serialize(GetType(), this),
        };

        var data = new ArraySegment<byte>(MessagePackSerializer.Serialize(rawMessage));
        if (to == -1)
        {
            MessageManager.OnData(data); // send it to self also. then server will handle sending it to rest
        }
        else if (to == -2)
        {
            // server routing below does broadcast to everyone else
        }
        else if (to == NetworkManager.LocalID)
        {
            MessageManager.OnData(data); // pass it to self without going thru server
            return;
        }

        if (NetworkManager.IsHost)
            MessageManager.OnServerData(0, data); // we are the server. we route
        else
            NetworkManager._client.Send(data, unreliable ? Util.Channels.Unreliable : Util.Channels.Reliable); // send it to server, which will route 
    }

    public abstract void OnReceive(int from, int to);
}