using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OWML.Common;
using UnityEngine;

namespace QSB2;

public static class AutoStart
{
    public static readonly int ProcessIndex = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName)
        .IndexOf(x => x.Id == Process.GetCurrentProcess().Id);

    public static void Go()
    {
        var sr = new Vector2Int(Screen.width, Screen.height);
        if (sr != new Vector2Int(800, 600))
        {
            Logger.Log("autostart: expected window resolution (800, 600)", MessageType.Warning);
            return;
        }

        var dr = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        if (dr != new Vector2Int(1920, 1080))
        {
            Logger.Log("autostart: expected display resolution (1920, 1080)", MessageType.Warning);
            return;
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

        // MoveWindow(Process.GetCurrentProcess().MainWindowHandle, x, y, 800, 600, true);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
}