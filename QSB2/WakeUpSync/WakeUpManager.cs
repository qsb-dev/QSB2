using System.Linq;
using HarmonyLib;
using MessagePack;
using OWML.Common;
using QSB2.Messaging;
using QSB2.Patches;
using QSB2.QObject;
using QSB2.QObject.Verify;
using QSB2.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QSB2.WakeUpSync;

public static class WakeUpManager
{
    public static float TimeScale = 1;

    public static bool HostWaitingForPlayers;
    public static bool CanJoin; // set on host

    public static bool AllQObjectsCreated;
    public static bool AllScenesSame;

    public static void RecalcCachedFlags()
    {
        // this means we need a created message for all qobject types, even if none are created...
        AllQObjectsCreated = NetworkManager.Connections.Values.All(x => x.QObjectsCreated.Count == QObjectManager.Entries.Count);
        if (AllQObjectsCreated) QPatchManager.Patch(QPatchWhen.OnQObjectsCreated);
        else QPatchManager.Unpatch(QPatchWhen.OnQObjectsCreated);

        // BUG: one player can destroy (by looping) while another does not. either destroy here or account for that in death/loop sync
        //      doesnt really break anything, just suddenly all multiplayer stuff will stop being multiplayer

        var lc = NetworkManager.LocalConnection;
        AllScenesSame = NetworkManager.Connections.Values.All(c => c.Scene == lc.Scene && c.LoadCounter == lc.LoadCounter);
    }

    static WakeUpManager()
    {
        // handle sync at beginning of loop
        QSceneManager.OnPostSceneLoad += (originalScene, loadScene) =>
        {
            if (!loadScene.IsGameScene()) return;

            // we start paused
            TimeScale = 0;

            if (NetworkManager.IsHost)
            {
                CanJoin = true;

                new HostWaitingForPlayersMessage
                {
                    Value = true
                }.Send(SendTo.All);
                Delay.RunWhen(() => Keyboard.current.enterKey.isPressed || AutoStart.BypassHostWaitingForPlayers, () =>
                {
                    new HostWaitingForPlayersMessage
                    {
                        Value = false
                    }.Send(SendTo.All);
                });
            }

            // will eventually get set from object manager
            Delay.RunWhen(() => AllQObjectsCreated, () =>
            {
                QObjectsVerifyMessage.DoVerify(); // TODO: stupid

                Logger.Log("all qobjects created on both sides. starting loop", MessageType.Success);
                TimeScale = 1;
                CanJoin = false;
            });
        };

        JoinMessage.Event += _ => RecalcCachedFlags();
        LeaveMessage.Event += _ => Delay.FireOnNextUpdate(RecalcCachedFlags);
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
            // exponential because -1 diff should be half speed and 1 diff should be 2x speed
            // TimeScale = Mathf.Pow(2, Mathf.Clamp(diff, -2, 2));
        }

        if (Time.timeSinceLevelLoad < _lastTimeSend || Time.timeSinceLevelLoad > _lastTimeSend + 1)
        {
            _lastTimeSend = Time.timeSinceLevelLoad;

            // BUG: broadcast uses server list, but client may not have that connection yet. causes error for a bit
            new TimeMessage
            {
                Time = Time.timeSinceLevelLoad
            }.Send(SendTo.All);
        }

        if (!OWTime.IsPaused())
            Time.timeScale = TimeScale;
    }
}

[HarmonyPatch]
public class WakeUpPatches() : QPatch(QPatchWhen.Immediately)
{
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerCameraEffectController), nameof(PlayerCameraEffectController.OnStartOfTimeLoop))]
    public static bool PlayerCameraEffectController_OnStartOfTimeLoop(PlayerCameraEffectController __instance)
    {
        // wake up immediately
        __instance.WakeUp();
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCameraEffectController), nameof(PlayerCameraEffectController.WakeUp))]
    public static void PlayerCameraEffectController_WakeUp(PlayerCameraEffectController __instance)
    {
        // prevent funny thing when you pause while waking up
        Locator.GetPauseCommandListener().AddPauseCommandLock();
        Delay.RunWhen(() => !__instance._isOpeningEyes, () => Locator.GetPauseCommandListener().RemovePauseCommandLock());
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(OWTime), nameof(OWTime.Pause))]
    public static bool StopPausing(ref OWTime.PauseType pauseType)
    {
        // loading pausing should stay. everything else should not pause
        if (pauseType is OWTime.PauseType.Initializing
            or OWTime.PauseType.Streaming
            or OWTime.PauseType.Loading)
        {
            return true;
        }
        else
        {
            // stop NomaiVR from pausing manually grrrrrrrrrrr
            // https://github.com/Raicuparta/nomai-vr/blob/master/NomaiVR/UI/LookArrow.cs#L138
            pauseType = OWTime.PauseType.Menu;
            return false;
        }
    }

    // TODO: is this applicable? why is this needed?
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SubmitActionSkipToNextLoop), nameof(SubmitActionSkipToNextLoop.AdvanceToNewTimeLoop))]
    public static void PreventMeditationSoftlock()
        => OWInput.ChangeInputMode(InputMode.Character);
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