using HarmonyLib;
using MessagePack;
using OWML.Common;
using QSB2.Messaging;
using QSB2.Patches;
using QSB2.QObject;
using UnityEngine;

namespace QSB2.MeteorSync;

public class QMeteorLauncher : QObject<MeteorLauncher>;

public class QMeteorLauncherBuilder : QObjectBuilder<QMeteorLauncher, MeteorLauncher>;

[MessagePackObject]
public class MeteorPreLaunchMessage : QObjectMessage<QMeteorLauncher>
{
    public override void OnReceive(QMeteorLauncher qObject, int from, int to)
    {
        var launcher = qObject.Component;

        foreach (var launchParticle in launcher._launchParticles)
        {
            launchParticle.Play();
        }
    }
}

[MessagePackObject]
public class MeteorLaunchMessage : QObjectMessage<QMeteorLauncher>
{
    [Key(1)] public required int MeteorId;
    [Key(2)] public required float LaunchSpeed;

    public override void OnReceive(QMeteorLauncher qObject, int from, int to)
    {
        var meteor = MeteorId.GetQObject<QMeteor>().Component;
        var launcher = qObject.Component;

        meteor.Initialize(launcher.transform, launcher._detectableField, launcher._detectableFluid);

        var linearVelocity = launcher._parentBody.GetPointVelocity(launcher.transform.position) + launcher.transform.TransformDirection(launcher._launchDirection) * LaunchSpeed;
        var angularVelocity = launcher.transform.forward * 2f;
        meteor.Launch(null, launcher.transform.position, launcher.transform.rotation, linearVelocity, angularVelocity);
        if (launcher._audioSector.ContainsOccupant(DynamicOccupant.Player))
        {
            launcher._launchSource.pitch = Random.Range(0.4f, 0.6f);
            launcher._launchSource.PlayOneShot(AudioType.BH_MeteorLaunch);
        }

        foreach (var launchParticle in launcher._launchParticles)
        {
            launchParticle.Stop();
        }
    }
}

[HarmonyPatch]
public class MeteorLauncherPatches() : QPatch(QPatchWhen.OnQObjectsCreated)
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MeteorLauncher), nameof(MeteorLauncher.FixedUpdate))]
    public static bool MeteorLauncher_FixedUpdate(MeteorLauncher __instance)
    {
        if (!NetworkManager.IsHost) return false;

        if (__instance._launchedMeteors != null)
        {
            for (var i = __instance._launchedMeteors.Count - 1; i >= 0; i--)
            {
                if (__instance._launchedMeteors[i] == null)
                {
                    __instance._launchedMeteors.QuickRemoveAt(i);
                }
                else if (__instance._launchedMeteors[i].isSuspended)
                {
                    __instance._meteorPool.Add(__instance._launchedMeteors[i]);
                    __instance._launchedMeteors.QuickRemoveAt(i);
                }
            }
        }

        if (__instance._launchedDynamicMeteors != null)
        {
            for (var j = __instance._launchedDynamicMeteors.Count - 1; j >= 0; j--)
            {
                if (__instance._launchedDynamicMeteors[j] == null)
                {
                    __instance._launchedDynamicMeteors.QuickRemoveAt(j);
                }
                else if (__instance._launchedDynamicMeteors[j].isSuspended)
                {
                    __instance._dynamicMeteorPool.Add(__instance._launchedDynamicMeteors[j]);
                    __instance._launchedDynamicMeteors.QuickRemoveAt(j);
                }
            }
        }

        if (__instance._initialized && Time.time > __instance._lastLaunchTime + __instance._launchDelay)
        {
            if (!__instance._areParticlesPlaying)
            {
                __instance._areParticlesPlaying = true;
                foreach (var launchParticle in __instance._launchParticles)
                {
                    launchParticle.Play();
                }

                __instance.GetQObject<QMeteorLauncher>()
                    .Send(new MeteorPreLaunchMessage(), SendTo.Others);
            }

            if (Time.time > __instance._lastLaunchTime + __instance._launchDelay + 2.3f)
            {
                __instance.LaunchMeteor();
                __instance._lastLaunchTime = Time.time;
                __instance._launchDelay = Random.Range(__instance._minInterval, __instance._maxInterval);
                __instance._areParticlesPlaying = false;
                foreach (var launchParticle in __instance._launchParticles)
                {
                    launchParticle.Stop();
                }
            }
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MeteorLauncher), nameof(MeteorLauncher.LaunchMeteor))]
    public static bool MeteorLauncher_LaunchMeteor(MeteorLauncher __instance)
    {
        if (!NetworkManager.IsHost) return false;

        var flag = __instance._dynamicMeteorPool != null && (__instance._meteorPool == null || Random.value < __instance._dynamicProbability);
        MeteorController meteorController = null;
        if (!flag)
        {
            if (__instance._meteorPool.Count == 0)
            {
                Logger.Log("MeteorLauncher is out of Meteors!", MessageType.Warning);
            }
            else
            {
                meteorController = __instance._meteorPool[__instance._meteorPool.Count - 1];
                meteorController.Initialize(__instance.transform, __instance._detectableField, __instance._detectableFluid);
                __instance._meteorPool.QuickRemoveAt(__instance._meteorPool.Count - 1);
                __instance._launchedMeteors.Add(meteorController);
            }
        }
        else if (__instance._dynamicMeteorPool.Count == 0)
        {
            Logger.Log("MeteorLauncher is out of Dynamic Meteors!", MessageType.Warning);
        }
        else
        {
            meteorController = __instance._dynamicMeteorPool[__instance._dynamicMeteorPool.Count - 1];
            meteorController.Initialize(__instance.transform, null, null);
            __instance._dynamicMeteorPool.QuickRemoveAt(__instance._dynamicMeteorPool.Count - 1);
            __instance._launchedDynamicMeteors.Add(meteorController);
        }

        if (meteorController != null)
        {
            var launchSpeed = Random.Range(__instance._minLaunchSpeed, __instance._maxLaunchSpeed);

            var linearVelocity = __instance._parentBody.GetPointVelocity(__instance.transform.position) + __instance.transform.TransformDirection(__instance._launchDirection) * launchSpeed;
            var angularVelocity = __instance.transform.forward * 2f;
            meteorController.Launch(null, __instance.transform.position, __instance.transform.rotation, linearVelocity, angularVelocity);
            if (__instance._audioSector.ContainsOccupant(DynamicOccupant.Player))
            {
                __instance._launchSource.pitch = Random.Range(0.4f, 0.6f);
                __instance._launchSource.PlayOneShot(AudioType.BH_MeteorLaunch);
            }

            __instance.GetQObject<QMeteorLauncher>()
                .Send(new MeteorLaunchMessage
                {
                    MeteorId = meteorController.GetQObject<QMeteor>().ID,
                    LaunchSpeed = launchSpeed
                }, SendTo.Others);
        }

        return false;
    }
}