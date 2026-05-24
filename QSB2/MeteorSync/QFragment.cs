using System;
using System.Linq;
using HarmonyLib;
using MessagePack;
using QSB2.Patches;
using QSB2.QObject;
using QSB2.Utility;

namespace QSB2.MeteorSync;

public class QFragment : QObject<FragmentIntegrity>
{
    public float LeashLength;

    public override void Create()
    {
        var rnd = (float)QFragmentBuilder.LeashRandom.NextDouble();
        var min = QFragmentBuilder.WhiteHoleVolume._debrisDistMin;
        var max = QFragmentBuilder.WhiteHoleVolume._debrisDistMax;
        LeashLength = min + (max - min) * rnd;

        base.Create();
    }
}

public class QFragmentBuilder : QObjectBuilder<QFragment, FragmentIntegrity>
{
    public static WhiteHoleVolume WhiteHoleVolume;
    public static Random LeashRandom;

    public override void Create()
    {
        if (LoadManager.GetCurrentScene() != OWScene.SolarSystem) return;

        // NH can make multiple so ensure its the stock whitehole 
        var whiteHole = Extensions.GetAllComponents<AstroObject>().First(x => x.GetAstroObjectName() == AstroObject.Name.WhiteHole);
        WhiteHoleVolume = whiteHole?.GetComponentInChildren<WhiteHoleVolume>();

        LeashRandom = new Random(NetworkManager.LocalConnection.LoadCounter); // should be the same between all clients. kinda dumb but should work

        base.Create();
    }
}

[HarmonyPatch]
public class FragmentPatches() : QPatch(QPatchWhen.OnQObjectsCreated)
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DetachableFragment), nameof(DetachableFragment.Detach))]
    public static void DetachableFragment_Detach_Prefix(DetachableFragment __instance, out FragmentIntegrity __state) =>
        // this gets set to null in Detach, so store it here and and then restore it in postfix
        __state = __instance._fragmentIntegrity;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DetachableFragment), nameof(DetachableFragment.Detach))]
    public static void DetachableFragment_Detach_Postfix(DetachableFragment __instance, FragmentIntegrity __state) =>
        __instance._fragmentIntegrity = __state;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DebrisLeash), nameof(DebrisLeash.Init))]
    public static void DebrisLeash_Init(DebrisLeash __instance)
    {
        if (__instance._detachableFragment == null || __instance._detachableFragment._fragmentIntegrity == null)
        {
            return;
        }

        var qFragment = __instance._detachableFragment._fragmentIntegrity.GetQObject<QFragment>();
        __instance._leashLength = qFragment.LeashLength;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(FragmentIntegrity), nameof(FragmentIntegrity.AddDamage))]
    public static void FragmentIntegrity_AddDamage(FragmentIntegrity __instance)
    {
        if (!NetworkManager.IsHost) return;

        __instance.GetQObject<QFragment>().Send(new FragmentIntegrityMessage { Integrity = __instance._integrity }, -2);
    }
}

[MessagePackObject]
public class FragmentIntegrityMessage : QObjectMessage<QFragment>
{
    [Key(1)] public required float Integrity;

    // BUG: does not appear to handle mother fragment
    public override void OnReceive(QFragment qObject, int from, int to)
    {
        if (OWMath.ApproxEquals(qObject.Component._integrity, Integrity))
        {
            return;
        }

        qObject.Component._integrity = Integrity;
        qObject.Component.CallOnTakeDamage();
    }
}