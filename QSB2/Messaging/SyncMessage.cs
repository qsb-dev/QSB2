using System.Collections.Generic;
using MessagePack;

namespace QSB2.Messaging;

[MessagePackObject]
public class SyncMessage : Message
{
    [Key(0)] public required List<int> IDs;

    public override void OnReceive(int from, int to)
    {
        foreach (var id in IDs)
        {
            NetworkManager.Connections.Add(id, new(id));
            Logger.Log($"{id} exists");
        }
    }
}