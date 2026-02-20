using System;
using MessagePack;
using QSB2.Utility;
using SteamTransport;

namespace QSB2.Messaging;

public abstract class Message
{
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
        if (NetworkManager.IsHost)
            MessageManager.OnServerData(0, data, channelId); // we are the server. we route
        else
            NetworkManager._client.Send(data, channelId); // send it to server, which will route 
    }

    public abstract void OnReceive(int from, int to);
}