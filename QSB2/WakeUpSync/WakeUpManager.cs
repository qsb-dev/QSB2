using System.Linq;
using HarmonyLib;
using MessagePack;
using OWML.Common;
using QSB2.Messaging;
using QSB2.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QSB2.WakeUpSync;

[HarmonyPatch]
public static class WakeUpManager
{
    public static float TimeScale = 1;

    // TODO: move?
    public static bool AllQObjectsCreated;
    public static bool AllScenesSame;

    public static bool HostSaysGo;
    public static bool CanJoin;

    static WakeUpManager()
    {
        // handle sync at beginning of loop
        QSceneManager.OnPostSceneLoad += (originalScene, loadScene) =>
        {
            if (!NetworkManager.IsConnected) return;
            if (!loadScene.IsGameScene()) return;

            // we start paused
            TimeScale = 0;
            CanJoin = true;
            HostSaysGo = false;
            AllQObjectsCreated = false;
            AllScenesSame = false;

            // Logger.Log("waiting for scene same", MessageType.Info);
            // Delay.RunWhen(() => AllScenesSame, () =>
            // {
                Logger.Log("waiting for host to say go", MessageType.Info);
                if (NetworkManager.IsHost)
                {
                    Delay.RunWhen(() => Keyboard.current.enterKey.isPressed, () =>
                    {
                        new HostSaysGoMessage().Send(-1);

                        Delay.RunWhen(() => AllQObjectsCreated, () =>
                        {
                            Logger.Log("all qobjects created on both sides. starting loop", MessageType.Success);
                            TimeScale = 1;
                            CanJoin = false;
                        });
                    });
                }
                else
                {
                    Delay.RunWhen(() => AllQObjectsCreated, () =>
                    {
                        Logger.Log("all qobjects created on both sides. starting loop", MessageType.Success);
                        TimeScale = 1;
                        CanJoin = false;
                    });
                }
            // });
        };
    }


    public static void Init()
    {
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
            var hostTime = NetworkManager.Connections[NetworkManager.Connections.Keys.Min()].Time;
            var myTime = NetworkManager.LocalConnection.Time;
            var diff = hostTime - myTime;
            TimeScale = Mathf.Pow(2, Mathf.Clamp(diff, -2, 2));
        }

        if (Time.timeSinceLevelLoad < _lastTimeSend || Time.timeSinceLevelLoad > _lastTimeSend + 1)
        {
            _lastTimeSend = Time.timeSinceLevelLoad;

            new TimeMessage
            {
                Time = Time.timeSinceLevelLoad
            }.Send(-1);
        }

        Time.timeScale = TimeScale;
    }

    /*
    [HarmonyPrefix, HarmonyPatch(typeof(Time), nameof(Time.timeScale), MethodType.Setter)]
    public static void Time_timeScale_Setter(ref float value)
    {
        value = TimeScale;
    }
    */

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerCameraEffectController), nameof(PlayerCameraEffectController.OnStartOfTimeLoop))]
    public static bool PlayerCameraEffectController_OnStartOfTimeLoop(PlayerCameraEffectController __instance)
    {
        if (!NetworkManager.IsConnected) return true;

        __instance.WakeUp();
        return false;
    }
}

[MessagePackObject]
public class HostSaysGoMessage : Message
{
    public override void OnReceive(int from, int to)
    {
        Logger.Log("host says go. waiting for qobjects", MessageType.Info);
        WakeUpManager.HostSaysGo = true;
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