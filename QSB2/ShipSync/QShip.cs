using System.Linq;
using QSB2.Authority;
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
    }

    public override void Create()
    {
        Instance = this;

        PositionSync = new(this);
        RelativeToSector = new(this);
        RelativeToSector.SectorDetector = Locator.GetShipTransform().GetComponentInChildren<SectorDetector>();
        HasOwner = new(this);
        HasOwner.Owner = NetworkManager.Connections.Values.First().ID; // host at first

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
    }

    private void WeAreFlying(bool value)
    {
        Send(new OwnerQueueMessage
        {
            Action = value ? OwnerQueueAction.Force : OwnerQueueAction.Remove
        }, -1);
    }
}