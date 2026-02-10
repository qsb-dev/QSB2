using MessagePack;
using QSB2.Utility;
using SteamTransport;

namespace QSB2.Messaging;

public abstract class Message
{
    public void Send(int to)
    {
        var rawMessage = new RawMessage
        {
            From = NetworkManager.LocalID,
            To = to,
            Type = GetType().Hash(),
            Message = MessagePackSerializer.Serialize(GetType(), this),
        };

        NetworkManager._client.Send(new(MessagePackSerializer.Serialize(rawMessage)), Util.Channels.Reliable);
    }

    public abstract void OnReceive(int from, int to);
}