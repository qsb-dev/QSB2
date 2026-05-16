using QSB2.QObject;
using UnityEngine;

namespace QSB2.ProbeSync;

// players are special in that they create/destroy their linked object, and they can be created and destroyed mid game

/// <summary>
/// for actual player in the world
/// </summary>
public class QProbe : QObject<Transform>, ITickable
{
    public Connection Connection;

    public override void Create()
    {
        PositionSync = new(this);
        RelativeToSector = new(this);
        RelativeToSector.SectorDetector = Locator.GetProbe().GetSectorDetector();
        Owner = new(this);
        Owner.ID = Connection.ID;

        Connection.QProbe = this;

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
        Connection.QPlayer = null;

        TickableManager.Tickables.Remove(this);

        if (!Owner.DoWeOwn)
        {
            GameObject.Destroy(Component.gameObject);
        }
    }

    public void Tick()
    {
        if (Owner.DoWeOwn && !Component.gameObject.activeSelf)
        {
            // put probe on player when its disabled
            // TODO: stupid
            Component.transform.position = Locator.GetPlayerTransform().transform.position;
            Component.transform.rotation = Locator.GetPlayerTransform().transform.rotation;
        }
        
        RelativeToSector.Tick();
        PositionSync.Tick();
    }
}