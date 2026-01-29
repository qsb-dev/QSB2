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
                UnityComponent = sector
            }.Create();
        }
    }

    public static void Destroy()
    {
        foreach (var qObject in QObjectManager.Entries[typeof(QSector).FullName.GetHashCode()].QObjects.Values)
        {
            qObject.Destroy();
        }
    }
}