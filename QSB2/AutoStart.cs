using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using HarmonyLib;
using OWML.Common;
using QSB2.Patches;
using QSB2.Utility;
using UnityEngine;

namespace QSB2;

public static class AutoStart
{
    public static readonly int ProcessIndex = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName)
        .IndexOf(x => x.Id == Process.GetCurrentProcess().Id);

    public static bool BypassHostWaitingForPlayers;

    public static IEnumerator Go()
    {
        var titleScreenManager = GameObject.FindObjectOfType<TitleScreenManager>();
        yield return new WaitForSeconds(10); // wait for title screen stuff to exist and finish loading profile and etc

        var sr = new Vector2Int(Screen.width, Screen.height);
        if (sr != new Vector2Int(800, 600))
        {
            Logger.Log("autostart: expected window resolution (800, 600)", MessageType.Warning);
            yield break;
        }

        var dr = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        if (dr != new Vector2Int(1920, 1080))
        {
            Logger.Log("autostart: expected display resolution (1920, 1080)", MessageType.Warning);
            yield break;
        }

        Logger.Log("doing autostart things", MessageType.Info);

        var y = dr.y / 2 - sr.y / 2;
        int x;

        if (ProcessIndex == 0)
        {
            x = 0 + sr.x / 2;
        }
        else
        {
            x = dr.x - sr.x / 2;
        }

        // no work :(
        // MoveWindow(Process.GetCurrentProcess().MainWindowHandle, x, y, 800, 600, true);

        NetworkManager.Address = "127.0.0.1:1337";
        NetworkManager.UseIpAddress = true;
        NetworkManager.DoFakeNetworkErrors = 0;
        if (ProcessIndex == 0)
        {
            NetworkManager.Host();
        }
        else
        {
            NetworkManager.Connect();
        }

        // TODO: test to see if this breaks if we do this before fully connected
        yield return new WaitUntil(() => NetworkManager.IsConnected);
     
        BypassHostWaitingForPlayers = true;
        titleScreenManager._resumeGameAction.Submit();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
}