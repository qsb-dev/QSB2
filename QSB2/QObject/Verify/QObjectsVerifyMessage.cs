using System;
using MessagePack;
using QSB2.Messaging;

namespace QSB2.QObject.Verify;

/// <summary>
/// host sends over some data about world objects.
/// </summary>
[MessagePackObject]
public class QObjectsVerifyMessage : Message
{
    /*
     * qsb1 works as follows:
     * - host sends over hash of object count and type names to every guy
     * - if they dont match, guy asks for more info
     * - host sends dump of each object (paths, categorized by the type)
     * - guy compares paths and reports differences before disconnecting
     */

    public override void OnReceive(int from, int to)
    {
    }

    // TODO: what the heck happens if i disconnect mid frame like im currently calling this???
    public static void DoVerify()
    {
        if (!NetworkManager.IsHost) return;

        foreach (var entry in QObjectManager.Entries.Values)
        {
            foreach (var connection in NetworkManager.Connections.Values)
            {
                if (!connection.QObjectsCreated.TryGetValue(entry.Type, out var count))
                {
                    NetworkManager._server.Disconnect(connection.ID, "youre missing objects!");
                    // throw new Exception($"host has qobjects for {entry.Type}, but connection {connection.ID} does not!");
                }

                if (count != entry.QObjects.Count)
                {
                    NetworkManager._server.Disconnect(connection.ID, "youre missing objects!");
                    // throw new Exception($"host has {entry.QObjects.Count} qobjects for {entry.Type}, but connection {connection.ID} has {count}!");
                }
            }
        }
    }
}