using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using OWML.Common;
using QSB2.Utility;

namespace QSB2.Patches;

public static class QPatchManager
{
    private static readonly QPatch[] _patchInstances;
    private static readonly Dictionary<QPatchWhen, HarmonyInstance> _harmonyInstances;

    private class HarmonyInstance
    {
        public Harmony Harmony;
        public bool IsPatched;
    }

    static QPatchManager()
    {
        _patchInstances = typeof(QPatch).GetDerivedTypes().Select(x => (QPatch)Activator.CreateInstance(x)).ToArray();
        _harmonyInstances = Enum.GetValues(typeof(QPatchWhen)).Cast<QPatchWhen>().ToDictionary(x => x, x => new HarmonyInstance
        {
            Harmony = new Harmony(x.ToString())
        });
    }

    public static void Patch(QPatchWhen when)
    {
        var harmonyInstance = _harmonyInstances[when];
        if (harmonyInstance.IsPatched) return;
        foreach (var qPatch in _patchInstances.Where(x => x.When == when))
        {
            try
            {
                harmonyInstance.Harmony.PatchAll(qPatch.GetType());
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString(), MessageType.Error);
            }
        }

        harmonyInstance.IsPatched = true;
    }

    public static void Unpatch(QPatchWhen when)
    {
        var harmonyInstance = _harmonyInstances[when];
        if (!harmonyInstance.IsPatched) return;
        harmonyInstance.Harmony.UnpatchSelf();
        harmonyInstance.IsPatched = false;
    }
}