using HarmonyLib;
using QSB2.Ownership;
using QSB2.QObject;

namespace QSB2.OrbSync;

[HarmonyPatch]
public class Orb : QObject<NomaiInterfaceOrb>, ITickable
{
    public override void Create()
    {
        PositionSync = new(this);
        PositionSync.Reference = Component.GetAttachedOWRigidbody().GetOrigParent(); // always relative to parent
        VelocitySync = new(this);
        Owner = new(this);
        OwnerQueue = new(this);

        TickableManager.Tickables.Add(this);

        base.Create();
    }
    
    public override void Destroy()
    {
        base.Destroy();

        TickableManager.Tickables.Remove(this);
    }

    public void Tick()
    {
        PositionSync.Tick();
        VelocitySync.Tick();
    }

    [HarmonyPrefix, HarmonyPatch(typeof(NomaiInterfaceOrb), nameof(NomaiInterfaceOrb.StartDragFromPosition))]
    public static void NomaiInterfaceOrb_StartDragFromPosition(NomaiInterfaceOrb __instance)
    {
        var orb = QObjectManager._componentToObject[__instance];
        orb.OwnerQueue.DoAction(OwnerQueueAction.Force);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(NomaiInterfaceOrb), nameof(NomaiInterfaceOrb.CancelDrag))]
    public static void NomaiInterfaceOrb_CancelDrag(NomaiInterfaceOrb __instance)
    {
        var orb = QObjectManager._componentToObject[__instance];
        orb.OwnerQueue.DoAction(OwnerQueueAction.Remove);
    }
}