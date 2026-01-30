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

    public static bool AllQObjectsCreated; // TODO: move?
    public static bool CanJoin;
    public static bool HostSaysGo;

    static WakeUpManager()
    {
        // handle sync at beginning of loop
        QSceneManager.OnPostSceneLoad += (originalScene, loadScene) =>
        {
            if (!NetworkManager.IsConnected) return;
            if (!loadScene.IsGameScene()) return;

            // we start paused
            Logger.Log("new loop. waiting for qobjects", MessageType.Info);
            TimeScale = 0;
            AllQObjectsCreated = false;

            if (NetworkManager.IsHost)
            {
                CanJoin = true;
                HostSaysGo = false;

                Delay.RunWhen(() => Keyboard.current.enterKey.isPressed, () =>
                {
                    CanJoin = false;
                    new HostSaysGoMessage().Send(-1);
                });
            }
        };
    }

    public static void Init()
    {
    }

    public static void Tick()
    {
        if (!NetworkManager.IsConnected) return;
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
        WakeUpManager.HostSaysGo = true;
        
        Delay.RunWhen(() => WakeUpManager.AllQObjectsCreated, () =>
        {
            Logger.Log("all qobjects created on both sides. starting loop", MessageType.Success);
            WakeUpManager.TimeScale = 1;
        });
    }
}