using HarmonyLib;
using MessagePack;
using QSB2.Ownership;
using QSB2.QObject;
using QSB2.WakeUpSync;

namespace QSB2.OrbSync;

// BUG: move orb to slot probably not accounted for here. might also break for some slots maybe idk
// BUG: suspended orb doesnt trigger slot message
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

    [HarmonyPostfix, HarmonyPatch(typeof(NomaiInterfaceOrb), nameof(NomaiInterfaceOrb.StartDragFromPosition))]
    public static void NomaiInterfaceOrb_StartDragFromPosition(NomaiInterfaceOrb __instance)
    {
        if (!WakeUpManager.AllQObjectsCreated) return;
        if (!__instance._isBeingDragged) return; // might not have set this to true

        var orb = __instance.GetQObject<Orb>();
        orb.OwnerQueue.DoAction(OwnerQueueAction.Force);

        orb.Send(new OrbDragMessage
        {
            Value = true
        }, -2);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(NomaiInterfaceOrb), nameof(NomaiInterfaceOrb.CancelDrag))]
    public static void NomaiInterfaceOrb_CancelDrag(NomaiInterfaceOrb __instance)
    {
        if (!WakeUpManager.AllQObjectsCreated) return;
        var orb = __instance.GetQObject<Orb>();
        orb.OwnerQueue.DoAction(OwnerQueueAction.Remove);

        orb.Send(new OrbDragMessage
        {
            Value = false
        }, -2);
    }
}

[MessagePackObject]
public class OrbDragMessage : QObjectMessage<Orb>
{
    [Key(1)] public required bool Value;

    public override void OnReceive(Orb qObject, int from, int to)
    {
        qObject.Component._isBeingDragged = Value;
    }
}