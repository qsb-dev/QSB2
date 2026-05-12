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

            // dont care about removing, itll go away on scene transition
            var shipCustomAttach = new GameObject(nameof(ShipCustomAttach));
            shipCustomAttach.transform.SetParent(Locator.GetShipTransform(), false);
            shipCustomAttach.AddComponent<ShipCustomAttach>();
        }

        SendCreated<QShip>(true);
    }
}