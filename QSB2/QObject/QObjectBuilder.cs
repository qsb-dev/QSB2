using System.Linq;
using QSB2.QObject.Verify;
using QSB2.Utility;
using QSB2.Utility.Deterministic;
using UnityEngine;

namespace QSB2.QObject;

public abstract class QObjectBuilder
{
    public abstract void Create();
    public abstract void Destroy();

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
}

public abstract class QObjectBuilder<TQ, TC> : QObjectBuilder where TQ : QObject<TC>, new() where TC : Component
{
    public override void Create()
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


    public override void Destroy()
    {
        var entry = QObjectManager.Entries[typeof(TQ).Hash()];
        foreach (var qObject in entry.QObjects.Values.ToList()) // we modify = copy
        {
            qObject.Destroy();
        }

        entry.NextId = 0;

        SendCreated<TQ>(false);
    }
}