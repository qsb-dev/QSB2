using System.Diagnostics;
using HarmonyLib;
using OWML.Common;
using OWML.Logging;

namespace QSB2;

[HarmonyPatch]
public static class Logger
{
    public static readonly int ProcessInstanceId = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName)
        .IndexOf(x => x.Id == Process.GetCurrentProcess().Id);

    [HarmonyPrefix, HarmonyPatch(typeof(ModSocketOutput), nameof(ModSocketOutput.WriteLine), typeof(string), typeof(MessageType), typeof(string))]
    public static void ModSocketOutput_WriteLine(ref string line, MessageType type, string senderType)
    {
        line = $"[{ProcessInstanceId}] " + line;
    }

    public static void Log(string msg, MessageType type = MessageType.Message) => QSB2.Instance.ModHelper.Console.WriteLine(msg, type);
}