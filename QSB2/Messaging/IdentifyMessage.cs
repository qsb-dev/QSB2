using System.Collections.Generic;
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
    [Key(3)] public required bool CanJoin;
    [Key(4)] public required List<int> IDs;

    public override void OnReceive(int from, int to)
    {
        NetworkManager.LocalID = to;
        Logger.Log($"i am {to}");

        foreach (var id in IDs)
        {
            NetworkManager.Connections.Add(id, new(id));
            Logger.Log($"{id} exists");
        }

        var leave = false;
        if (QSBVersion != QSB2.QSBVersion) leave = true;
        if (GameVersion != QSB2.GameVersion) leave = true;
        if (DLCInstalled != QSB2.DLCInstalled) leave = true;
        if (!CanJoin) leave = true;
        if (leave)
        {
            Logger.Log("rejected. disconnecting");
            NetworkManager.Disconnect();
        }

        // tell everyone we joined
        new JoinMessage
        {
            ID = to
        }.Send(-1);
    }
}