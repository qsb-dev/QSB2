using System.Diagnostics;

namespace QSB2;

public static class Logger
{
    public static readonly int ProcessInstanceId = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName)
        .IndexOf(x => x.Id == Process.GetCurrentProcess().Id);

    public static void Log(string msg)
    {
        msg = $"[{ProcessInstanceId}] " + msg;

        QSB2.Instance.ModHelper.Console.WriteLine(msg);
    }
}