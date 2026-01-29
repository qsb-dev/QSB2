using HarmonyLib;
using UnityEngine;

namespace QSB2.WakeUpSync;

[HarmonyPatch]
public static class WakeUpManager
{
    public static float TimeScale = 1;

    [HarmonyPrefix, HarmonyPatch(typeof(Time), nameof(Time.timeScale), MethodType.Setter)]
    public static void Time_timeScale_Setter(ref float value)
    {
        value = TimeScale;
    }
}