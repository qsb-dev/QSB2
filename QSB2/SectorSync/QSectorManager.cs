using QSB2.QObject;
using QSB2.Utility;
using UnityEngine;

namespace QSB2.SectorSync;

public class QSectorManager
{
    public static void Init()
    {
        foreach (var sector in Extensions.GetAllComponents<Sector>())
        {
            new GameObject().AddComponent<QSector>().UnityComponent = sector;
        }
    }

    public static void Uninit()
    {
        foreach (var qObject in QObjectManager.Entries[typeof(QSector).FullName.GetHashCode()].QObjects.Values)
        {
            GameObject.Destroy(qObject.gameObject);
        }
    }
}