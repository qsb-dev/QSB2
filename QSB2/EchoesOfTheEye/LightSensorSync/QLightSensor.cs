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
    // global is union/or of local
    public readonly Dictionary<QPlayer, bool> GlobalIlluminated = new();
    public bool GlobalIlluminatedValue => GlobalIlluminated.Any(x => x.Value);
    public readonly Dictionary<QPlayer, List<DreamLanternController>> GlobalLanternList = new();
    public List<DreamLanternController> GlobalLanternListValue => GlobalLanternList.SelectMany(x => x.Value).Distinct().ToList();
    public bool LocalIlluminated;
    public List<DreamLanternController> LocalLanternList;
    public bool IsPlayerLightSensor;

    public override void Create()
    {
        IsPlayerLightSensor = Component.name is "CameraDetector" or "REMOTE_CameraDetector";

        // dont think i need to fire any events here, Start already handles that
        // BUG: at the towers, taking away the lanterns on one side and not the other triggers darkness, even tho it shouldnt
        foreach (var qPlayer in QObjectManager.GetQObjects<QPlayer>())
            GlobalIlluminated.Add(qPlayer, Component._startIlluminated);
        LocalIlluminated = Component._startIlluminated;

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
        qObject.GlobalIlluminated[NetworkManager.Connections[from].QPlayer] = Value;

        var illuminated = qObject.Component._illuminated;
        qObject.Component._illuminated = qObject.GlobalIlluminatedValue;
        if (qObject.Component._illuminated && !illuminated)
            qObject.Component.OnDetectLight.Invoke();
        else if (!qObject.Component._illuminated && illuminated)
            qObject.Component.OnDetectDarkness.Invoke();
    }
}

[MessagePackObject]
public class SectorLanternListMessage : QObjectMessage<QLightSensor>
{
    [Key(1)] public required List<DreamLanternController> Value;

    public override void OnReceive(QLightSensor qObject, int from, int to)
    {
        qObject.GlobalLanternList[NetworkManager.Connections[from].QPlayer] = Value;

        qObject.Component._illuminatingDreamLanternList = qObject.GlobalLanternListValue;
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
        var prevLanternList = __instance._illuminatingDreamLanternList;
        __instance.UpdateIllumination();
        var prevLocalIlluminated = qLightSensor.LocalIlluminated;
        var prevLocalLanternList = qLightSensor.LocalLanternList;
        qLightSensor.LocalIlluminated = __instance._illuminated;
        qLightSensor.LocalLanternList = __instance._illuminatingDreamLanternList;
        __instance._illuminated = prevIlluminated;
        __instance._illuminatingDreamLanternList = prevLanternList;

        if (qLightSensor.LocalIlluminated != prevLocalIlluminated)
            qLightSensor.Send(new SensorIlluminatedMessage { Value = qLightSensor.LocalIlluminated }, SendTo.All);

        if (__instance._detectDreamLanterns)
        {
            if (!qLightSensor.LocalLanternList.SequenceEqual(prevLocalLanternList))
            {
                qLightSensor.Send(new SectorLanternListMessage { Value = qLightSensor.LocalLanternList }, SendTo.All);
            }
        }

        return false;
    }
}