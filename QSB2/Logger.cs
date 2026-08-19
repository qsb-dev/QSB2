using HarmonyLib;
using OWML.Common;
using OWML.Logging;
using QSB2.Patches;

namespace QSB2;

public static class Logger
{
    public static void Log(string msg, MessageType type = MessageType.Message) => QSB2.Instance.ModHelper.Console.WriteLine(msg, type);
}

[HarmonyPatch]
public class LoggerPatch() : QPatch(QPatchWhen.Immediately)
{
    [HarmonyPrefix, HarmonyPatch(typeof(ModSocketOutput), nameof(ModSocketOutput.WriteLine), typeof(string), typeof(MessageType), typeof(string))]
    public static void ModSocketOutput_WriteLine(ref string line, MessageType type, string senderType)
    {
        line = $"[{NetworkManager.LocalID}] " + line;
    }
}