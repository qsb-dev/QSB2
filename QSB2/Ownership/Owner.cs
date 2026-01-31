using System.Collections.Generic;
using System.Linq;
using MessagePack;
using QSB2.QObject;

namespace QSB2.Ownership;

public struct Owner(QObject.QObject qObject)
{
    public bool DoWeOwn => ID == NetworkManager.LocalID;

    public int ID = -1;
}

public struct OwnerQueue(QObject.QObject qObject)
{
    // BUG: if 2 clients send messages at the same time, what happens? do they arrive in the same order on both ends? else this would get desynced
    public readonly List<int> IDs = new();

    public void DoAction(OwnerQueueAction action, int id = -1)
    {
        qObject.Send(new OwnerQueueMessage
        {
            PlayerID = id == -1 ? NetworkManager.LocalID : id,
            Action = action
        }, -1);
    }
}

[MessagePackObject]
public class OwnerQueueMessage : QObjectMessage
{
    [Key(2)] public required int PlayerID;
    [Key(3)] public required OwnerQueueAction Action;

    public override void OnReceive(QObject.QObject qObject, int from, int to)
    {
        var ownerQueue = qObject.OwnerQueue.IDs;

        switch (Action)
        {
            case OwnerQueueAction.Add:
                ownerQueue.SafeAdd(PlayerID);
                break;

            case OwnerQueueAction.Remove:
                ownerQueue.Remove(PlayerID);
                break;

            case OwnerQueueAction.Force:
                ownerQueue.Remove(PlayerID);
                ownerQueue.Insert(0, PlayerID);
                break;
        }

        // empty queue = defer to host
        qObject.Owner.ID = ownerQueue.Count != 0 ? ownerQueue[0] : NetworkManager.Connections.Keys.Min();
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