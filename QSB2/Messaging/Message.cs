using System;
using MessagePack;

namespace QSB2.Messaging;

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