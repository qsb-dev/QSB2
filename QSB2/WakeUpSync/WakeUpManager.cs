using HarmonyLib;
using MessagePack;
using OWML.Common;
using QSB2.Messaging;
using QSB2.Patches;
using QSB2.QObject;
using QSB2.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QSB2.WakeUpSync;

public static class WakeUpManager
{
    public static float TimeScale = 1;

    public static bool AllQObjectsCreated;
    public static bool AllScenesSame;
    public static bool HostWaitingForPlayers;
    public static bool CanJoin; // set on host

    static WakeUpManager()
    {
        // handle sync at beginning of loop
        QSceneManager.OnPostSceneLoad += (originalScene, loadScene) =>
        {
            if (!NetworkManager.IsConnected) return;
            if (!loadScene.IsGameScene()) return;

            // we start paused
            TimeScale = 0;

            if (NetworkManager.IsHost)
            {
                CanJoin = true;

                new HostWaitingForPlayersMessage
                {
                    Value = true
                }.Send(-1);
                Delay.RunWhen(() => Keyboard.current.enterKey.isPressed, () =>
                {
                    new HostWaitingForPlayersMessage
                    {
                        Value = false
                    }.Send(-1);
                });
            }

            // will eventually get set from object manager
            Delay.RunWhen(() => AllQObjectsCreated, () =>
            {
                Logger.Log("all qobjects created on both sides. starting loop", MessageType.Success);
                TimeScale = 1;
                CanJoin = false;
                StartableManager.Start();
            });
        };
    }

    private static float _lastTimeSend;

    public static void Tick()
    {
        if (!NetworkManager.IsConnected) return;

        foreach (var connection in NetworkManager.Connections.Values)
        {
            connection.Time += Time.deltaTime;
        }

        if (AllQObjectsCreated)
        {
            // a minor amount of actual timesync because yes it is actually needed
            var hostTime = NetworkManager.Connections[NetworkManager.ConnectionIDs[0]].Time;
            var myTime = NetworkManager.LocalConnection.Time;
            var diff = hostTime - myTime;
            // TimeScale = Mathf.Pow(2, Mathf.Clamp(diff, -2, 2));
        }

        if (Time.timeSinceLevelLoad < _lastTimeSend || Time.timeSinceLevelLoad > _lastTimeSend + 1)
        {
            _lastTimeSend = Time.timeSinceLevelLoad;

            // BUG: broadcast uses server list, but client may not have that connection yet. causes error for a bit
            new TimeMessage
            {
                Time = Time.timeSinceLevelLoad
            }.Send(-1);
        }

        // BUG: not properly preventing pausing does a buncha goofy player movement bugs. im lazy rn
        Time.timeScale = TimeScale;
    }
}

[HarmonyPatch]
public class WakeUpPatches() : QPatch(QPatchWhen.OnConnected)
{
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerCameraEffectController), nameof(PlayerCameraEffectController.OnStartOfTimeLoop))]
    public static bool PlayerCameraEffectController_OnStartOfTimeLoop(PlayerCameraEffectController __instance)
    {
        __instance.WakeUp();
        return false;
    }

    /*
    [HarmonyPrefix, HarmonyPatch(typeof(Time), nameof(Time.timeScale), MethodType.Setter)]
    public static void Time_timeScale_Setter(ref float value)
    {
        value = TimeScale;
    }
    */
}

[MessagePackObject]
public class HostWaitingForPlayersMessage : Message
{
    [Key(0)] public required bool Value;

    public override void OnReceive(int from, int to)
    {
        Logger.Log($"host waiting for players = {Value}", MessageType.Info);
        WakeUpManager.HostWaitingForPlayers = Value;
    }
}

[MessagePackObject]
public class TimeMessage : Message
{
    [Key(0)] public required float Time;

    public override void OnReceive(int from, int to)
    {
        NetworkManager.Connections[from].Time = Time;
    }
}