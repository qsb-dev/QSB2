using System.Linq;
using QSB2.QObject;
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
        foreach (var qObject in QObjectManager.Entries[typeof(QSector).Hash()].QObjects.Values.ToList()) // we modify = copy
        {
            qObject.Destroy();
        }

        new QObjectsCreatedMessage
        {
            Type = typeof(QSector).Hash(),
            Created = false
        }.Send(-1);
    }
}