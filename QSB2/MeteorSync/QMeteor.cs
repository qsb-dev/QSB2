using HarmonyLib;
using QSB2.Messaging;
using QSB2.Patches;
using QSB2.QObject;
using UnityEngine;

namespace QSB2.MeteorSync;

public class QMeteor : QObject<MeteorController>
{
    public static bool IsSpecialImpact(GameObject go) =>
        go == Locator.GetPlayerCollider().gameObject ||
        Locator.GetProbe() != null && go == Locator.GetProbe()._anchor._collider.gameObject;
}

public class QMeteorBuilder : QObjectBuilder<QMeteor, MeteorController>;

/// <summary>
/// for syncing impact with a remote player/probe
/// </summary>
public class MeteorSpecialImpactMessage : QObjectMessage<QMeteor>
{
    public override void OnReceive(QMeteor qObject, int from, int to)
    {
        var meteor = qObject.Component;

        meteor._intactRenderer.enabled = false;
        meteor._impactLight.enabled = true;
        meteor._impactLight.intensity = meteor._impactLightCurve.Evaluate(0f);
        foreach (var impactParticle in meteor._impactParticles)
        {
            impactParticle.Play();
        }

        meteor._impactSource.PlayOneShot(AudioType.BH_MeteorImpact);
        foreach (var owCollider in meteor._owColliders)
        {
            owCollider.SetActivation(false);
        }

        meteor._owRigidbody.MakeKinematic();
        FragmentSurfaceProxy.UntrackMeteor(meteor);
        FragmentCollisionProxy.UntrackMeteor(meteor);
        meteor._ignoringCollisions = false;
        meteor._hasImpacted = true;
        meteor._impactTime = Time.time;
        var probe = Locator.GetProbe();
        if (probe != null && probe.IsAnchored() && probe.transform.IsChildOf(meteor.transform))
        {
            probe.Unanchor();
        }

        if (meteor._owRigidbody.GetReferenceFrame() != null)
        {
            meteor._owRigidbody.GetReferenceFrame().FireDestroyEvent();
        }
    }
}

[HarmonyPatch]
public class MeteorPatches() : QPatch(QPatchWhen.OnQObjectsCreated)
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MeteorController), nameof(MeteorController.Impact))]
    public static void MeteorController_Impact(MeteorController __instance,
        GameObject hitObject, Vector3 impactPoint, Vector3 impactVel)
    {
        if (QMeteor.IsSpecialImpact(hitObject))
        {
            __instance.GetQObject<QMeteor>()
                .Send(new MeteorSpecialImpactMessage());
        }
    }
}