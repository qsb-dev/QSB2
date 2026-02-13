using System.Linq;
using QSB2.QObject;
using QSB2.QObject.Verify;
using QSB2.Utility;
using QSB2.Utility.Deterministic;

namespace QSB2.OrbSync;

public class OrbManager
{
    public static void Create()
    {
        foreach (var orb in Extensions.GetAllComponents<NomaiInterfaceOrb>().SortDeterministic())
        {
            new Orb
            {
                Component = orb
            }.Create();
        }

        new QObjectsCreatedMessage
        {
            Type = typeof(Orb).Hash(),
            Created = true,
            Count = QObjectManager.Entries[typeof(Orb).Hash()].QObjects.Count
        }.Send(-1);
    }

    public static void Destroy()
    {
        var entry = QObjectManager.Entries[typeof(Orb).Hash()];
        foreach (var qObject in entry.QObjects.Values.ToList()) // we modify = copy
        {
            qObject.Destroy();
        }

        entry.NextId = 0;

        new QObjectsCreatedMessage
        {
            Type = typeof(Orb).Hash(),
            Created = false
        }.Send(-1);
    }
}