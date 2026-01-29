using System.Collections.Generic;
using System.Linq;
using MessagePack;
using QSB2.QObject;

namespace QSB2.Authority;

public class HasOwner(QObject.QObject qObject)
{
    public bool DoWeOwn => Owner == NetworkManager.LocalID;

    public int Owner = -1;
    // optional. we can just set owner once and never touch it again
    public readonly List<int> OwnerQueue = new();
}

[MessagePackObject]
public class OwnerQueueMessage : QObjectMessage
{
    [Key(2)] public required OwnerQueueAction Action;

    public override void OnReceive(QObject.QObject qObject, int from, int to)
    {
        var ownerQueue = qObject.HasOwner.OwnerQueue;

        switch (Action)
        {
            case OwnerQueueAction.Add:
                ownerQueue.SafeAdd(from);
                break;

            case OwnerQueueAction.Remove:
                ownerQueue.Remove(from);
                break;

            case OwnerQueueAction.Force:
                ownerQueue.Remove(from);
                ownerQueue.Insert(0, from);
                break;
        }

        // empty queue = defer to host
        qObject.HasOwner.Owner = ownerQueue.Count != 0 ? ownerQueue[0] : NetworkManager.Connections.Values.First().ID;
    }
}

public enum OwnerQueueAction : byte
{
    /// <summary>
    /// add player to the queue
    /// </summary>
    Add,

    /// <summary>
    /// remove player from the queue
    /// </summary>
    Remove,

    /// <summary>
    /// add player to the queue and force them to the front
    /// </summary>
    Force
}