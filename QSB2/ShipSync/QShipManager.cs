using System.Linq;
using QSB2.QObject;
using QSB2.Utility;

namespace QSB2.ShipSync;

public class QShipManager
{
    public static void Create()
    {
        new QShip
        {
            Component = Locator.GetShipTransform()
        }.Create();

        new QObjectsCreatedMessage
        {
            Type = typeof(QShip).Hash(),
            Created = true
        }.Send(-1);
    }

    public static void Destroy()
    {
        QObjectManager.Entries[typeof(QShip).Hash()].QObjects.Values.Single().Destroy();

        new QObjectsCreatedMessage
        {
            Type = typeof(QShip).Hash(),
            Created = false
        }.Send(-1);
    }
}