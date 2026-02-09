using System.Linq;
using QSB2.Messaging;
using QSB2.Ownership;
using QSB2.QObject;
using UnityEngine;

namespace QSB2.ShipSync;

public class QShip : QObject<Transform>, ITickable
{
    public static QShip Instance;

    static QShip()
    {
        GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", _ => Instance?.WeAreFlying(true));
        GlobalMessenger.AddListener("ExitFlightConsole", () => Instance?.WeAreFlying(false));

        LeaveMessage.Event += id =>
        {
            if (NetworkManager.IsHost)
            {
                // leaving player = left the seat. host does this since the player left
                Instance?.OwnerQueue.DoAction(OwnerQueueAction.Remove, id);
            }
        };
    }

    public override void Create()
    {
        Instance = this;

        PositionSync = new(this);
        VelocitySync = new(this);
        RelativeToSector = new(this);
        RelativeToSector.SectorDetector = Locator.GetShipTransform().GetComponentInChildren<SectorDetector>();
        Owner = new(this);
        Owner.ID = NetworkManager.Connections.Keys.Min(); // set to host for now
        OwnerQueue = new(this);

        TickableManager.Tickables.Add(this);

        Component = Locator.GetShipTransform();

        base.Create();
    }

    public override void Destroy()
    {
        Instance = null;

        base.Destroy();

        TickableManager.Tickables.Remove(this);
    }

    public void Tick()
    {
        RelativeToSector.Tick();
        PositionSync.Tick();
        VelocitySync.Tick();
    }

    private void WeAreFlying(bool value)
    {
        OwnerQueue.DoAction(value ? OwnerQueueAction.Force : OwnerQueueAction.Remove);
    }
}