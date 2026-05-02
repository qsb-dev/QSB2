using System;
using System.Collections.Generic;
using System.Linq;
using QSB2.QObject;
using QSB2.Utility;
using QSB2.Utility.Deterministic;
using UnityEngine;

namespace QSB2;

// test to see what happens when i throw everything into qobjects

public class OtherQObject : QObject.QObject;

public class OtherBuilder : QObjectBuilder
{
    private static readonly List<Type> _allTypes;

    static OtherBuilder()
    {
        _allTypes = typeof(MonoBehaviour).GetDerivedTypes().ToList();
        _allTypes.Remove(typeof(NomaiInterfaceOrb));
        _allTypes.Remove(typeof(Sector));
    }

    public override void Create()
    {
        foreach (var type in _allTypes)
        {
            var components = Resources.FindObjectsOfTypeAll(type).Cast<Component>()
                .Where(x => x.gameObject.scene.name is not (null or "DontDestroyOnLoad"));
            foreach (var component in components.SortDeterministic())
            {
                new OtherQObject
                {
                    Component = component,
                }.Create();
            }
        }

        SendCreated<OtherQObject>(true);
    }

    public override void Destroy()
    {
        var entry = QObjectManager.Entries[typeof(OtherQObject).Hash()];
        foreach (var qObject in entry.QObjects.Values.ToList()) // we modify = copy
        {
            qObject.Destroy();
        }

        entry.NextId = 0;

        SendCreated<OtherQObject>(false);
    }
}