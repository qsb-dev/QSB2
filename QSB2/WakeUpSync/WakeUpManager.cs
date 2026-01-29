using HarmonyLib;
using OWML.Common;
using QSB2.Utility;
using UnityEngine;

namespace QSB2.WakeUpSync;

[HarmonyPatch]
public static class WakeUpManager
{
    public static float TimeScale = 1;

    public static bool AllQObjectsCreated; // TODO: move?

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

            Delay.RunWhen(() => AllQObjectsCreated, () =>
            {
                Logger.Log("all qobjects created on both sides. starting loop", MessageType.Success);
                TimeScale = 1;
            });
        };
    }

    public static void Init()
    {
    }

    public static void Tick()
    {
        Time.timeScale = TimeScale;
    }

    /*
    [HarmonyPrefix, HarmonyPatch(typeof(Time), nameof(Time.timeScale), MethodType.Setter)]
    public static void Time_timeScale_Setter(ref float value)
    {
        value = TimeScale;
    }
*/
}