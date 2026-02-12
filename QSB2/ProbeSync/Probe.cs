using QSB2.QObject;
using UnityEngine;

namespace QSB2.ProbeSync;

// players are special in that they create/destroy their linked object, and they can be created and destroyed mid game

/// <summary>
/// for actual player in the world
/// </summary>
public class Probe : QObject<Transform>, ITickable
{
    public required Connection Connection;

    public override void Create()
    {
        PositionSync = new(this);
        RelativeToSector = new(this);
        RelativeToSector.SectorDetector = Locator.GetProbe().GetSectorDetector();
        Owner = new(this);
        Owner.ID = Connection.ID;

        Connection.Probe = this;

        TickableManager.Tickables.Add(this);

        if (Owner.DoWeOwn)
        {
            Component = Locator.GetProbe().transform;
        }
        else
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.Destroy(go.GetComponent<Collider>());
            Component = go.GetComponent<Transform>();
            go.AddComponent<Light>().range = 50;
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
            GameObject.Destroy(Component.gameObject);
        }
    }

    public void Tick()
    {
        RelativeToSector.Tick();
        PositionSync.Tick();
    }
}