using QSB2.QObject;
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

        SendCreated<QShip>(true);
    }
}