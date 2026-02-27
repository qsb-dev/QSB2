using QSB2.QObject;
using QSB2.QObject.Verify;
using QSB2.Utility;
using UnityEngine;

namespace QSB2.ShipSync;

public class QShipBuilder : QObjectBuilder<QShip, Transform>
{
    public override void Create()
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
            Count = QObjectManager._entries[typeof(QShip).Hash()].QObjects.Count,
        }.Send(-1);
    }
}