using System;
using System.Collections.Generic;
using MessagePack;
using OWML.Utils;
using QSB2.QObject;

namespace QSB2.Ownership;

public class Owner(QObject.QObject qObject)
{
    public bool DoWeOwn => ID == NetworkManager.LocalID;

    public int ID = -1;
}

// TODO: on player leave, remove them if they are the owner and tell others

public class OwnerQueue(QObject.QObject qObject)
{
    public List<int> IDs;
    public Action OnOwnerChange;
    public bool WaitingOnResponse;

    public void DoAction(OwnerQueueAction action, int id = -1)
    {
        WaitingOnResponse = true;
        qObject.Send(new OwnerActionMessage
        {
            PlayerID = id == -1 ? NetworkManager.LocalID : id,
            Action = action
        }, NetworkManager.ConnectionIDs[0]);
    }
}

/// <summary>
/// send action to server
/// </summary>
[MessagePackObject]
public class OwnerActionMessage : QObjectMessage
{
    [Key(2)] public required int PlayerID;
    [Key(3)] public required OwnerQueueAction Action;

    public override void OnReceive(QObject.QObject qObject, int from, int to)
    {
        qObject.OwnerQueue.IDs ??= new();
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

        qObject.Send(new OwnerMessage
        {
            // empty queue = no one owns
            OwnerID = ownerQueue.Count != 0 ? ownerQueue[0] : -1
        }, -1);
    }
}

/// <summary>
/// server responds with new owner
/// </summary>
[MessagePackObject]
public class OwnerMessage : QObjectMessage
{
    [Key(2)] public required int OwnerID;

    public override void OnReceive(QObject.QObject qObject, int from, int to)
    {
        qObject.Owner.ID = OwnerID;
        qObject.OwnerQueue.WaitingOnResponse = false;
        qObject.OwnerQueue.OnOwnerChange?.SafeInvoke();
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
    /// add player to the queue and force them to the front.
    /// use with caution, as someone else may steal your ownership at any time, including immediately after you force.
    /// </summary>
    Force
}