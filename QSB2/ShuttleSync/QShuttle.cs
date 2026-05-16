using QSB2.QObject;

/*
namespace QSB2.ShuttleSync;

public class QShuttle : QObject<NomaiShuttleController>, ITickable
{
    public override void Create()
    {
        PositionSync = new(this);
        // PositionSync.UpdateInterval = 1f;
        // PositionSync.OccasionalMode = true;
        // PositionSync.Lerp = false;
        VelocitySync = new(this);
        RelativeToSector = new(this);
        RelativeToSector.SectorDetector = Component.gameObject.AddComponent<SectorDetector>();
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

public class QShuttleBuilder : QObjectBuilder<QShuttle, NomaiShuttleController>;
*/

