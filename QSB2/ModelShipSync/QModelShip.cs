using QSB2.QObject;

namespace QSB2.ModelShipSync;

public class QModelShip : QObject<ModelShipController>, ITickable
{
    public override void Create()
    {
        PositionSync = new(this);
        PositionSync.UpdateInterval = 1;
        PositionSync.OccasionalMode = true;
        PositionSync.Lerp = false;
        VelocitySync = new(this);
        RelativeToSector = new(this);
        RelativeToSector.SectorDetector = Component.GetComponentInChildren<SectorDetector>(true);
        Owner = new(this);
        Owner.ID = NetworkManager.ConnectionIDs[0];
        // TODO: owner queue for Enter/ExitRemoteFlightConsole

        TickableManager.Tickables.Add(this);

        base.Create();
    }

    public override void Destroy()
    {
        TickableManager.Tickables.Remove(this);

        base.Destroy();
    }

    public void Tick()
    {
        RelativeToSector.Tick();
        PositionSync.Tick();
        VelocitySync.Tick();
    }
}

public class QModelShipBuilder : QObjectBuilder<QModelShip, ModelShipController>;