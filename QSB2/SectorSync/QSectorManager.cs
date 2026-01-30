using System.Linq;
using QSB2.QObject;
using QSB2.QObject.Verify;
using QSB2.Utility;
using QSB2.Utility.Deterministic;

namespace QSB2.SectorSync;

public class QSectorManager
{
    public static void Create()
    {
        foreach (var sector in Extensions.GetAllComponents<Sector>().SortDeterministic())
        {
            new QSector
            {
                Component = sector
            }.Create();
        }

        new QObjectsCreatedMessage
        {
            Type = typeof(QSector).Hash(),
            Created = true,
            Count = QObjectManager.Entries[typeof(QSector).Hash()].QObjects.Count
        }.Send(-1);
    }

    public static void Destroy()
    {
        var entry = QObjectManager.Entries[typeof(QSector).Hash()];
        foreach (var qObject in entry.QObjects.Values.ToList()) // we modify = copy
        {
            qObject.Destroy();
        }

        entry.NextId = 0;

        new QObjectsCreatedMessage
        {
            Type = typeof(QSector).Hash(),
            Created = false
        }.Send(-1);
    }
}