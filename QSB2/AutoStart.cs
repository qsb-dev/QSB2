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
        yield return new WaitForSeconds(1); // wait for title screen stuff to exist and finish loading profile and etc

        Logger.Log("doing autostart things", MessageType.Info);

        var sr = new Vector2Int(Screen.width, Screen.height);
        Logger.Log($"sr = {sr}");

        var dr = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        Logger.Log($"dr = {dr}");

        var y = dr.y / 2 - sr.y / 2;
        var x = dr.x / 2 - sr.x / 2;

        if (ProcessIndex == 0)
        {
            x -= sr.x / 2;
        }
        else
        {
            x += sr.x / 2;
        }

        SetWindowPos(GetActiveWindow(), (IntPtr)0, x, y, 0, 0, 0x0001 | 0x0004);

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

        yield return new WaitUntil(() => NetworkManager.IsConnected);

        BypassHostWaitingForPlayers = true;
        titleScreenManager._resumeGameAction.Submit();
    }

    [DllImport("C:\\Windows\\System32\\user32.dll", SetLastError = true)]
    private static extern IntPtr GetActiveWindow();

    [DllImport("C:\\Windows\\System32\\user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
}