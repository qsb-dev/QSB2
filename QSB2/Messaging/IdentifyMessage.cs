using MessagePack;
using QSB2.WakeUpSync;
using Steamworks;

namespace QSB2.Messaging;

/// <summary>
/// server tells us who we and existing people are
/// </summary>
[MessagePackObject]
public class IdentifyMessage : Message
{
    [Key(0)] public required string QSBVersion;
    [Key(1)] public required string GameVersion;
    [Key(2)] public required bool DLCInstalled;

    [Key(3)] public required bool CanJoin;

    // the minimal initial state
    [Key(4)] public required (int ID, string name, OWScene scene, int loadCounter)[] Connections;
    [Key(5)] public required bool HostWaitingForPlayers;
    // not qobjects cuz thats initialized after everyone joins

    public override void OnReceive(int from, int to)
    {
        NetworkManager.LocalID = to;
        Logger.Log($"i am {to}");

        foreach (var x in Connections)
        {
            NetworkManager.Connections.Add(x.ID, new(x.ID, x.name) { Scene = x.scene, LoadCounter = x.loadCounter });
            NetworkManager.ConnectionIDs.Add(x.ID);
            Logger.Log($"{x.ID} exists");
        }

        WakeUpManager.HostWaitingForPlayers = HostWaitingForPlayers;

        var leave = false;
        if (QSBVersion != QSB2.QSBVersion) leave = true;
        if (GameVersion != QSB2.GameVersion) leave = true;
        if (DLCInstalled != QSB2.DLCInstalled) leave = true;
        if (!CanJoin) leave = true;
        if (leave)
        {
            // we never send join message here, so no one even knows we exist. we can safely leave without bothering anymore
            Logger.Log("rejected. disconnecting");
            NetworkManager.Disconnect();
        }

        // tell everyone we joined
        new JoinMessage
        {
            ID = to,
            Name = SteamFriends.GetPersonaName()
        }.Send(-1);
    }
}