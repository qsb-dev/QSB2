using MessagePack;

namespace QSB2.Messaging;

/// <summary>
/// server tells us who we are
/// </summary>
[MessagePackObject]
public class IdentifyMessage : Message
{
    [Key(0)] public required string QSBVersion;
    [Key(1)] public required string GameVersion;
    [Key(2)] public required bool DLCInstalled;

    public override void OnReceive(int from, int to)
    {
        var leave = false;
        if (QSBVersion != QSB2.QSBVersion) leave = true;
        if (GameVersion != QSB2.GameVersion) leave = true;
        if (DLCInstalled != QSB2.DLCInstalled) leave = true;
        if (leave) NetworkManager.Disconnect();

        NetworkManager.LocalID = to;
        Logger.Log($"i am {to}");

        // tell everyone else that we joined
        new JoinMessage
        {
            ID = to
        }.Send(-2);
    }
}