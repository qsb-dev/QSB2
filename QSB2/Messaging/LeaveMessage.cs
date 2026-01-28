using MessagePack;

namespace QSB2.Messaging;

[MessagePackObject]
public class LeaveMessage : Message
{
    [Key(0)] public required int ID;

    public override void OnReceive(int from, int to)
    {
        NetworkManager.Connections.Remove(ID);
        Logger.Log($"{ID} disconnected");
    }
}