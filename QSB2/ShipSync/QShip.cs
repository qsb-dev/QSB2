using System.Linq;
using QSB2.Authority;
using QSB2.Messaging;
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
                Instance?.Send(new OwnerQueueMessage
                {
                    PlayerID = id,
                    Action = OwnerQueueAction.Remove
                }, -1);
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
        HasOwner = new(this);
        HasOwner.Owner = NetworkManager.Connections.Keys.Min(); // host is smallest id

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
        Send(new OwnerQueueMessage
        {
            PlayerID = NetworkManager.LocalID,
            Action = value ? OwnerQueueAction.Force : OwnerQueueAction.Remove,
        }, -1);
    }
}