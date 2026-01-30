using System.Linq;
using HarmonyLib;
using OWML.Common;
using QSB2.Messaging;
using QSB2.QObject;
using QSB2.Utility;
using UnityEngine;

namespace QSB2.WakeUpSync;

[HarmonyPatch]
public static class WakeUpManager
{
    public static float TimeScale = 1;

    public static bool AllQObjectsCreated; // TODO: move?
    public static bool CanJoin;

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
            CanJoin = true;

            // TODO: wait until host says go
            Delay.RunWhen(() => AllQObjectsCreated, () =>
            {
                Logger.Log("all qobjects created on both sides. starting loop", MessageType.Success);
                TimeScale = 1;
                CanJoin = false;
            });
        };

        // player list change = need to update this
        JoinMessage.Event += _ =>
        {
            AllQObjectsCreated = NetworkManager.Connections.Values.All(x => x.QObjectsCreated.Count == QObjectManager.Entries.Count);
        };
        LeaveMessage.Event += _ =>
        {
            AllQObjectsCreated = NetworkManager.Connections.Values.All(x => x.QObjectsCreated.Count == QObjectManager.Entries.Count);
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
}