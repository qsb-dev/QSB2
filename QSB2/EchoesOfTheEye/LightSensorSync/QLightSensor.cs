using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MessagePack;
using QSB2.Messaging;
using QSB2.Patches;
using QSB2.PlayerSync;
using QSB2.QObject;

namespace QSB2.EchoesOfTheEye.LightSensorSync;

public class QLightSensor : QObject<SingleLightSensor>
{
    public readonly HashSet<QPlayer> Illuminators = new();
    public bool LocalIlluminated;
    public List<DreamLanternController> LocalLanternList;
    public bool IsPlayerLightSensor;

    public override void Create()
    {
        IsPlayerLightSensor = Component.name is "CameraDetector" or "REMOTE_CameraDetector";

        // dont think i need to fire any events here, Start already handles that
        // BUG: at the towers, taking away the lanterns on one side and not the other triggers darkness, even tho it shouldnt
        if (Component._startIlluminated)
        {
            foreach (var qPlayer in QObjectManager.GetQObjects<QPlayer>())
                Illuminators.Add(qPlayer);
            LocalIlluminated = true;
        }

        if (Component._detectDreamLanterns)
        {
            LocalLanternList = new();
        }

        base.Create();
    }
}

public class QLightSensorBuilder : QObjectBuilder<QLightSensor, SingleLightSensor>;

[MessagePackObject]
public class SensorIlluminatedMessage : QObjectMessage<QLightSensor>
{
    [Key(1)] public required bool Value;

    public override void OnReceive(QLightSensor qObject, int from, int to)
    {
        if (Value) qObject.Illuminators.Add(NetworkManager.Connections[from].QPlayer);
        else qObject.Illuminators.Remove(NetworkManager.Connections[from].QPlayer);

        var illuminated = qObject.Component._illuminated;
        qObject.Component._illuminated = qObject.Illuminators.Count > 0;
        if (qObject.Component._illuminated && !illuminated)
            qObject.Component.OnDetectLight.Invoke();
        else if (!qObject.Component._illuminated && illuminated)
            qObject.Component.OnDetectDarkness.Invoke();
    }
}

[HarmonyPatch(typeof(SingleLightSensor))]
public class LightSensorPatches() : QPatch(QPatchWhen.OnQObjectsCreated)
{
    [HarmonyPrefix, HarmonyPatch(nameof(SingleLightSensor.OnSectorOccupantsUpdated))]
    private static bool OnSectorOccupantsUpdated(SingleLightSensor __instance)
    {
        var qLightSensor = __instance.GetQObject<QLightSensor>();

        var containsAnyOccupants = __instance._sector.ContainsAnyOccupants(DynamicOccupant.Player | DynamicOccupant.Probe);
        if (containsAnyOccupants && !__instance.enabled)
        {
            __instance.enabled = true;
            __instance._lightDetector.GetShape().enabled = true;
            if (__instance._preserveStateWhileDisabled)
            {
                __instance._fixedUpdateFrameDelayCount = 10;
            }
        }
        else if (!containsAnyOccupants && __instance.enabled)
        {
            __instance.enabled = false;
            __instance._lightDetector.GetShape().enabled = false;
            if (!__instance._preserveStateWhileDisabled)
            {
                qLightSensor.Send(new SensorIlluminatedMessage { Value = false }, SendTo.All);
            }
        }

        return false;
    }


    /// <summary>
    /// to prevent allocating a new list every frame
    /// </summary>
    private static readonly List<DreamLanternController> _illuminatingDreamLanternList = new();

    [HarmonyPrefix]
    [HarmonyPatch(nameof(SingleLightSensor.ManagedFixedUpdate))]
    private static bool ManagedFixedUpdate(SingleLightSensor __instance)
    {
        if (__instance._fixedUpdateFrameDelayCount > 0)
        {
            __instance._fixedUpdateFrameDelayCount--;
            return false;
        }

        var qLightSensor = __instance.GetQObject<QLightSensor>();

        // we store global illumination in __instance._illuminated
        // but smuggle local illumination out of UpdateIllumination
        var prevIlluminated = __instance._illuminated;
        // var prevLanternList = __instance._illuminatingDreamLanternList;
        __instance.UpdateIllumination();
        var prevLocalIlluminated = qLightSensor.LocalIlluminated;
        var prevLocalLanternList = qLightSensor.LocalLanternList;
        qLightSensor.LocalIlluminated = __instance._illuminated;
        qLightSensor.LocalLanternList = __instance._illuminatingDreamLanternList;
        __instance._illuminated = prevIlluminated;
        // __instance._illuminatingDreamLanternList = prevLanternList;

        if (qLightSensor.LocalIlluminated != prevLocalIlluminated)
            qLightSensor.Send(new SensorIlluminatedMessage { Value = qLightSensor.LocalIlluminated }, SendTo.All);

        if (__instance._detectDreamLanterns)
        {
            if (!qLightSensor.LocalLanternList?.SequenceEqual(prevLocalLanternList) ?? false)
            {
                // TODO: similar thing as above. union all local lists together to get the global list
                // or dont? if only the player with the local list cares and no one else does, it might be fine to just have everyone track it individually 
            }
        }

        return false;
    }
}