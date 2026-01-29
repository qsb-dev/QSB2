using HarmonyLib;
using UnityEngine;

namespace QSB2.WakeUpSync;

[HarmonyPatch]
public static class WakeUpPatches
{
    [HarmonyPrefix, HarmonyPatch(typeof(Time), nameof(Time.timeScale), MethodType.Setter)]
    public static bool Time_timeScale_Setter(out float __result)
    {
        __result = WakeUpManager.TimeScale;
        return false;
    }
}