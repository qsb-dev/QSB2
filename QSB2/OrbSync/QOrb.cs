using HarmonyLib;
using MessagePack;
using OWML.Utils;
using QSB2.Ownership;
using QSB2.Patches;
using QSB2.QObject;
using QSB2.WakeUpSync;
using UnityEngine;

namespace QSB2.OrbSync;

public class QOrb : QObject<NomaiInterfaceOrb>, ITickable
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
}

[MessagePackObject]
public class OrbDragMessage : QObjectMessage<QOrb>
{
    [Key(1)] public required bool Value;

    public override void OnReceive(QOrb qObject, int from, int to)
    {
        if (Value == qObject.Component._isBeingDragged)
        {
            return;
        }

        if (Value)
        {
            qObject.Component._isBeingDragged = true;
            qObject.Component._interactibleCollider.enabled = false;
            if (qObject.Component._orbAudio != null)
            {
                qObject.Component._orbAudio.PlayStartDragClip();
            }
        }
        else
        {
            qObject.Component._isBeingDragged = false;
            qObject.Component._interactibleCollider.enabled = true;
        }
    }
}

[MessagePackObject]
public class OrbSlotMessage : QObjectMessage<QOrb>
{
    [Key(1)] public required int SlotIndex;
    [Key(2)] public required bool PlayAudio;

    public override void OnReceive(QOrb qObject, int from, int to)
    {
        var oldSlot = qObject.Component._occupiedSlot;
        var newSlot = SlotIndex == -1 ? null : qObject.Component._slots[SlotIndex];
        if (newSlot == oldSlot)
        {
            return;
        }

        if (oldSlot)
        {
            oldSlot._occupyingOrb = null;
            oldSlot.RaiseEvent(nameof(oldSlot.OnSlotDeactivated), oldSlot);

            qObject.Component._occupiedSlot = null;
        }

        if (newSlot)
        {
            newSlot._occupyingOrb = qObject.Component;
            if (Time.timeSinceLevelLoad > 1f)
            {
                newSlot.RaiseEvent(nameof(newSlot.OnSlotActivated), newSlot);
            }

            qObject.Component._occupiedSlot = newSlot;
            qObject.Component._enterSlotTime = Time.time;
            if (newSlot.CancelsDragOnCollision())
            {
                qObject.Component.CancelDrag();
            }

            if (PlayAudio && qObject.Component._orbAudio != null && newSlot.GetPlayActivationAudio())
            {
                qObject.Component._orbAudio.PlaySlotActivatedClip();
            }
        }
    }
}

[HarmonyPatch(typeof(NomaiInterfaceOrb))]
public class OrbPatches() : QPatch(QPatchWhen.OnQObjectsCreated)
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(NomaiInterfaceOrb.StartDragFromPosition))]
    private static bool StartDragFromPosition(NomaiInterfaceOrb __instance, ref bool __result, Vector3 manipPos)
    {
        if (__instance._orbBody.IsSuspended() || __instance._isBeingDragged)
        {
            __result = false;
            return false;
        }

        if (__instance.RecentlyEnteredSlot())
        {
            __instance._loseFocusToStartDrag = true;
        }

        if (Vector3.Distance(manipPos, __instance.transform.position) < __instance._startDragDist)
        {
            if (!__instance._loseFocusToStartDrag)
            {
                __instance._isBeingDragged = true;
                __instance._interactibleCollider.enabled = false;
                if (__instance._orbAudio != null)
                {
                    __instance._orbAudio.PlayStartDragClip();
                }

                var qOrb = __instance.GetQObject<QOrb>();
                qOrb.Send(new OrbDragMessage { Value = true }, -2);
                qOrb.OwnerQueue.DoAction(OwnerQueueAction.Force);
            }
        }
        else
        {
            __instance._loseFocusToStartDrag = false;
        }

        __result = __instance._isBeingDragged;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(NomaiInterfaceOrb.CancelDrag))]
    private static bool CancelDrag(NomaiInterfaceOrb __instance)
    {
        if (!__instance._isBeingDragged)
        {
            return false;
        }

        var qOrb = __instance.GetQObject<QOrb>();
        if (!qOrb.Owner.DoWeOwn)
        {
            return false;
        }

        qOrb.Send(new OrbDragMessage { Value = false }, -2);
        qOrb.OwnerQueue.DoAction(OwnerQueueAction.Remove);
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(NomaiInterfaceOrb.CheckSlotCollision))]
    private static bool CheckSlotCollision(NomaiInterfaceOrb __instance, bool playAudio)
    {
        var qOrb = __instance.GetQObject<QOrb>();
        if (qOrb.Owner.DoWeOwn)
        {
            if (__instance._occupiedSlot == null)
            {
                for (var slotIndex = 0; slotIndex < __instance._slots.Length; slotIndex++)
                {
                    var slot = __instance._slots[slotIndex];
                    if (slot != null && slot.CheckOrbCollision(__instance))
                    {
                        __instance._occupiedSlot = slot;
                        __instance._enterSlotTime = Time.time;
                        if (slot.CancelsDragOnCollision())
                        {
                            __instance.CancelDrag();
                        }

                        if (playAudio && __instance._orbAudio != null && slot.GetPlayActivationAudio())
                        {
                            __instance._orbAudio.PlaySlotActivatedClip();
                        }

                        qOrb.Send(new OrbSlotMessage
                        {
                            SlotIndex = slotIndex,
                            PlayAudio = playAudio
                        }, -2);
                        break;
                    }
                }
            }
            else if ((!__instance._occupiedSlot.IsAttractive() || __instance._isBeingDragged) && !__instance._occupiedSlot.CheckOrbCollision(__instance))
            {
                __instance._occupiedSlot = null;
                qOrb.Send(new OrbSlotMessage
                {
                    SlotIndex = -1,
                    PlayAudio = playAudio
                }, -2);
            }
        }

        __instance._owCollider.SetActivation(__instance._occupiedSlot == null || !__instance._occupiedSlot.IsAttractive() || __instance._isBeingDragged);

        return false;
    }
}