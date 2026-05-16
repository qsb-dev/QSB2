using QSB2.QObject;

/*namespace QSB2.ModelShipSync;

public class QModelShip : QObject<ModelShipController>, ITickable
{
    public override void Create()
    {
        PositionSync = new(this);
        VelocitySync = new(this);
        RelativeToSector = new(this);
        RelativeToSector.SectorDetector = Component.GetComponentInChildren<SectorDetector>();
        Owner = new(this);
        Owner.ID = NetworkManager.ConnectionIDs[0];

        TickableManager.Tickables.Add(this); // this should sync before player and probe :PPP

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

public class QModelShipBuilder : QObjectBuilder<QModelShip, ModelShipController>;*/