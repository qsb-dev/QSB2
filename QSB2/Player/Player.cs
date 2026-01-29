using QSB2.PositionSync;
using QSB2.QObject;
using UnityEngine;

namespace QSB2.Player;

// players are special in that they create/destroy their linked object, and they can be created and destroyed mid game

/// <summary>
/// for actual player in the world
/// </summary>
public class Player : QObject.QObject<Player>
{
    public Connection Connection;

    public override void Create()
    {
        PositionSync = new(this);
        TickableManager.Tickables.Add(PositionSync);
        PositionSync.Create();
        RelativeToSector = new(this);
        TickableManager.Tickables.Add(RelativeToSector);
        HasOwner = new(this);
        HasOwner.Owner = Connection.ID;

        if (HasOwner.DoWeOwn)
        {
            // we own. grab local guy
            UnityComponent = Locator.GetPlayerTransform();

            Logger.Log($"local player {Connection.ID} created");
        }
        else
        {
            // create player object
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityComponent = go.GetComponent<Transform>();

            Logger.Log($"remote player {Connection.ID} created");
        }

        base.Create();
    }

    public override void Destroy()
    {
        base.Destroy();

        TickableManager.Tickables.Remove(PositionSync);
        TickableManager.Tickables.Remove(RelativeToSector);

        if (HasOwner.DoWeOwn)
        {
            // remove player object
            GameObject.Destroy(UnityComponent.gameObject);
        }
    }
}