using System.Collections.Generic;
using System.Linq;
using QSB2.QObject.Verify;
using QSB2.Utility;
using QSB2.Utility.Deterministic;
using UnityEngine;

namespace QSB2.QObject;

public abstract class QObjectBuilder
{
    public abstract void Create();

    // TODO?: refactor this into just a "destroy everything" thing and have separate message?
    //        i think we only really want granularity when creating, destroy is just get rid of everything
    public abstract void Destroy();

    #region utils

    protected static void CreateWith<TQ, TC>() where TQ : QObject<TC>, new() where TC : Component
    {
        foreach (var component in Extensions.GetAllComponents<TC>().SortDeterministic())
        {
            new TQ
            {
                Component = component
            }.Create();
        }

        SendCreated<TQ>(true);
    }

    protected static void DestroyWith<TQ, TC>() where TQ : QObject<TC>, new() where TC : Component
    {
        var entry = QObjectManager.Entries[typeof(TQ).Hash()];
        foreach (var qObject in entry.QObjects.Values.ToList()) // we modify = copy
        {
            qObject.Destroy();
        }

        entry.NextId = 0;

        SendCreated<TQ>(false);
    }

    protected static void SendCreated<T>(bool created)
    {
        // BUG: without loopback. it will take a minute for us to realize we destroyed our objects, which is bad cuz it means we'll keep receiving messages like they exist!
        var msg = new QObjectsCreatedMessage
        {
            Type = typeof(T).Hash(),
            Created = created
        };
        if (created) msg.Count = QObjectManager.Entries[typeof(T).Hash()].QObjects.Count;
        msg.Send(-1);
    }

    #endregion
}

// convenience thing
public abstract class QObjectBuilder<TQ, TC> : QObjectBuilder where TQ : QObject<TC>, new() where TC : Component
{
    public override void Create() => CreateWith<TQ, TC>();
    public override void Destroy() => DestroyWith<TQ, TC>();
}