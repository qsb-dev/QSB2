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
        Logger.Log("host says go. waiting for qobjects", MessageType.Info);
        WakeUpManager.HostSaysGo = true;
    }
}