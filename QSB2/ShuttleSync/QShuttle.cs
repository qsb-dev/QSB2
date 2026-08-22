/*using HarmonyLib;
using QSB2.Patches;
using QSB2.QObject;

namespace QSB2.ShuttleSync;

public class QShuttle : QObject<NomaiShuttleController>, ITickable
{
    public override void Create()
    {
        PositionSync = new(this);
        PositionSync.UpdateInterval = 10f;
        PositionSync.OccasionalMode = true;
        PositionSync.Lerp = false;
        VelocitySync = new(this);
        RelativeToSector = new(this);
        RelativeToSector.SectorDetector = Component.gameObject.AddComponent<SectorDetector>();
        RelativeToSector.SectorDetector._attachedRigidbody = Component._shuttleBody; // to make sector heuristic happy 
        // leave occupant type as undefined. we dont want to load things with this rn. this will lead to it going thru the floor LOLOLOO
        Owner = new(this);
        Owner.ID = NetworkManager.ConnectionIDs[0];

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

public class QShuttleBuilder : QObjectBuilder<QShuttle, NomaiShuttleController>;

/*public class ShuttlePatches : QPatch(QPatchWhen.OnQObjectsCreated)
{
    [HarmonyPrefix, HarmonyPatch(typeof(NomaiShuttleController),  nameof(NomaiShuttleController.UnsuspendShuttle))]
    pub
}#1#*/