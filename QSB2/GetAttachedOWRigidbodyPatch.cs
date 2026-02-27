using HarmonyLib;
using QSB2.Patches;
using UnityEngine;

namespace QSB2;

/// <summary>
/// qsb1 did this to get inactive objects (like anglers).
/// theres a chance this breaks something, but idk what.
/// </summary>
[HarmonyPatch(typeof(OWExtensions))]
public class GetAttachedOWRigidbodyPatch() : QPatch(QPatchWhen.Immediately) // might turn into onconnected
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(OWExtensions.GetAttachedOWRigidbody), typeof(GameObject), typeof(bool))]
    private static bool GetAttachedOWRigidbody(GameObject obj, bool ignoreThisTransform, out OWRigidbody __result)
    {
        OWRigidbody owrigidbody = null;
        var transform = obj.transform;
        if (ignoreThisTransform)
        {
            transform = obj.transform.parent;
        }

        while (owrigidbody == null)
        {
            owrigidbody = transform.GetComponent<OWRigidbody>();
            /*
            if (owrigidbody != null && !owrigidbody.gameObject.activeInHierarchy)
            {
                owrigidbody = null;
            }
            */
            if ((transform == obj.transform.root && owrigidbody == null) || owrigidbody != null)
            {
                break;
            }

            transform = transform.parent;
        }

        __result = owrigidbody;
        return false;
    }
}