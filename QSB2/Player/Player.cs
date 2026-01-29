using QSB2.QObject;
using UnityEngine;

namespace QSB2.Player;

// players are special in that they create/destroy their linked object, and they can be created and destroyed mid game

/// <summary>
/// for actual player in the world
/// </summary>
public class Player : QObject<Transform>, ITickable
{
    public Connection Connection;

    public override void Create()
    {
        PositionSync = new(this);
        RelativeToSector = new(this);
        RelativeToSector.SectorDetector = Locator.GetPlayerSectorDetector();
        HasOwner = new(this);
        HasOwner.Owner = Connection.ID;

        TickableManager.Tickables.Add(this);

        if (HasOwner.DoWeOwn)
        {
            // we own. grab local guy
            Component = Locator.GetPlayerCameraController().transform;

            Logger.Log($"local player {Connection.ID} created");
        }
        else
        {
            // create player object
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.Destroy(go.GetComponent<Collider>());
            Component = go.GetComponent<Transform>();

            Logger.Log($"remote player {Connection.ID} created");
        }

        base.Create();
    }

    public override void Destroy()
    {
        base.Destroy();

        TickableManager.Tickables.Remove(this);

        if (!HasOwner.DoWeOwn)
        {
            // remove player object
            GameObject.Destroy(Component.gameObject);

            Logger.Log($"remote player {ID} destroyed");
        }
    }

    public void Tick()
    {
        RelativeToSector.Tick();
        PositionSync.Tick();
    }
}