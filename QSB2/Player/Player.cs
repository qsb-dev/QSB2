using QSB2.QObject;
using UnityEngine;

namespace QSB2.Player;

// players are special in that they create/destroy their linked object, and they can be created and destroyed mid game

/// <summary>
/// for actual player in the world
/// </summary>
public class Player : QObject<Transform>, ITickable
{
    public required Connection Connection;

    public override void Create()
    {
        PositionSync = new(this);
        RelativeToSector = new(this);
        RelativeToSector.SectorDetector = Locator.GetPlayerSectorDetector();
        Owner = new(this);
        Owner.ID = Connection.ID;

        Connection.Player = this;

        TickableManager.Tickables.Add(this);

        if (Owner.DoWeOwn)
        {
            // we own. grab local guy
            Component = Locator.GetPlayerCameraController().transform;

            Logger.Log($"local player for {Connection.ID} created");
        }
        else
        {
            // create player object
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.Destroy(go.GetComponent<Collider>());
            Component = go.GetComponent<Transform>();

            Logger.Log($"remote player for {Connection.ID} created");
        }

        base.Create();
    }

    public override void Destroy()
    {
        base.Destroy();
        Connection.Player = null;

        TickableManager.Tickables.Remove(this);

        if (!Owner.DoWeOwn)
        {
            // remove player object
            GameObject.Destroy(Component.gameObject);

            Logger.Log($"remote player for {Connection.ID} destroyed");
        }
    }

    public void Tick()
    {
        RelativeToSector.Tick();
        PositionSync.Tick();
    }
}