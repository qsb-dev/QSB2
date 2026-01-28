using MessagePack;

namespace QSB2.Messaging;

[MessagePackObject]
public class JoinMessage : Message
{
    [Key(0)] public required string QSBVersion;
    [Key(1)] public required string GameVersion;
    [Key(2)] public required bool DLCInstalled;
    [Key(3)] public required int ID;

    public override void OnReceive(int from, int to)
    {
        if (ID == to)
        {
            var leave = false;
            if (QSBVersion != QSB2.QSBVersion) leave = true;
            if (GameVersion != QSB2.GameVersion) leave = true;
            if (DLCInstalled != QSB2.DLCInstalled) leave = true;
            NetworkManager.LocalID = ID;
            Logger.Log($"local id is {ID}");
            if (leave) NetworkManager.Disconnect();
        }

        // TODO: this adds even if local player kicks themselves above. this might be a problem
        NetworkManager.Connections.Add(ID, new(ID));
        Logger.Log($"{ID} connected");
    }
}