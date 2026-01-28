using System.Diagnostics;
using OWML.Common;

namespace QSB2;

public static class Logger
{
    public static readonly int ProcessInstanceId = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName)
        .IndexOf(x => x.Id == Process.GetCurrentProcess().Id);

    public static void Log(string msg, MessageType type = MessageType.Message)
    {
        msg = $"[{ProcessInstanceId}] " + msg;

        QSB2.Instance.ModHelper.Console.WriteLine(msg, type);
    }
}