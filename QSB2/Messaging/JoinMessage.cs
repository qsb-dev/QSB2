using MessagePack;

namespace QSB2.Messaging;

[MessagePackObject]
public class JoinMessage : Message
{
    [Key(0)] public required int ID;

    public override void OnReceive(int from, int to)
    {
        NetworkManager.Connections.Add(ID, new(ID));
        Logger.Log($"{ID} connected");
    }
}