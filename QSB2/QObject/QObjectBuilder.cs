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

        new QObjectsCreatedMessage
        {
            Type = typeof(TQ).Hash(),
            Created = true,
            Count = QObjectManager._entries[typeof(TQ).Hash()].QObjects.Count
        }.Send(-1);
    }

    public override void Destroy()
    {
        var entry = QObjectManager._entries[typeof(TQ).Hash()];
        foreach (var qObject in entry.QObjects.Values.ToList()) // we modify = copy
        {
            qObject.Destroy();
        }

        entry.NextId = 0;

        new QObjectsCreatedMessage
        {
            Type = typeof(TQ).Hash(),
            Created = false
        }.Send(-1);
    }
}