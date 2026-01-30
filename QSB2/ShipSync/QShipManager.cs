using System.Linq;
using QSB2.QObject;
using QSB2.QObject.Verify;
using QSB2.Utility;

namespace QSB2.ShipSync;

public class QShipManager
{
    public static void Create()
    {
        if (LoadManager.GetCurrentScene() != OWScene.EyeOfTheUniverse)
        {
            new QShip
            {
                Component = Locator.GetShipTransform()
            }.Create();
        }

        new QObjectsCreatedMessage
        {
            Type = typeof(QShip).Hash(),
            Created = true,
            Count = QObjectManager.Entries[typeof(QShip).Hash()].QObjects.Count,
        }.Send(-1);
    }

    public static void Destroy()
    {
        var entry = QObjectManager.Entries[typeof(QShip).Hash()];
        foreach (var qObject in entry.QObjects.Values.ToList()) // we modify = copy
        {
            qObject.Destroy();
        }

        entry.NextId = 0;

        new QObjectsCreatedMessage
        {
            Type = typeof(QShip).Hash(),
            Created = false
        }.Send(-1);
    }
}