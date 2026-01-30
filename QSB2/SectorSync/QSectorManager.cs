using System.Linq;
using QSB2.QObject;
using QSB2.QObject.Verify;
using QSB2.Utility;
using UnityEngine;

namespace QSB2.SectorSync;

public class QSectorManager
{
    public static void Create()
    {
        foreach (var sector in Extensions.GetAllComponents<Sector>())
        {
            new QSector
            {
                Component = sector
            }.Create();
        }

        new QObjectsCreatedMessage
        {
            Type = typeof(QSector).Hash(),
            Created = true
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