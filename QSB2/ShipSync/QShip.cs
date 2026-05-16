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
        // BUG: with below on, relative to sector gets busted and the ship will be in the wrong place for a second when it switches sectors
        // PositionSync.UpdateInterval = 1f;
        // PositionSync.OccasionalMode = true;
        // PositionSync.Lerp = false;
        VelocitySync = new(this);
        RelativeToSector = new(this);
        RelativeToSector.SectorDetector = Locator.GetShipTransform().GetComponentInChildren<SectorDetector>();
        Owner = new(this);
        OwnerQueue = new(this);

        TickableManager.Tickables.Add(this); // this should sync before player and probe :PPP

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
        // no one owns = host takes
        if (Owner.ID == -1) Owner.ID = NetworkManager.ConnectionIDs[0];

        RelativeToSector.Tick();
        PositionSync.Tick();
        VelocitySync.Tick();
    }

    private void WeAreFlying(bool value)
    {
        OwnerQueue.DoAction(value ? OwnerQueueAction.Force : OwnerQueueAction.Remove);
    }
}