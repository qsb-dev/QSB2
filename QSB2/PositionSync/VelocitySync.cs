using MessagePack;
using QSB2.QObject;
using QSB2.Utility;
using SteamTransport;
using UnityEngine;

namespace QSB2.PositionSync;

public class VelocitySync(QObject.QObject qObject)
{
    public Vector3 RelVel = Vector3.zero;
    public Vector3 RelAngVel = Vector3.zero;
    public float PrevTime; // for dropping out of order messages

    public float UpdateInterval = .1f;
    private float _timer;

    public void Tick()
    {
        if (qObject.Owner.ID == -1) return; // no owner = do nothing

        if (qObject.PositionSync.Reference == null) return;

        var refBody = qObject.PositionSync.Reference.GetAttachedOWRigidbody();
        var body = qObject.Component.GetAttachedOWRigidbody();

        if (qObject.Owner.DoWeOwn)
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer < UpdateInterval) return;
            _timer = 0;

            // owner - sync from unity component
            RelVel = refBody.ToRelVel(body.GetVelocity(), body.GetPosition());
            RelAngVel = refBody.ToRelAngVel(body.GetAngularVelocity());

            qObject.Send(new VelocityMessage
            {
                RelVel = RelVel,
                RelAngVel = RelAngVel,
                Time = Time.unscaledTime
            }, -2, Channels.Unreliable);
        }
        else
        {
            // non owner - sync to unity component
            if (body is ShipBody) ShipBody_SetVelocity(body, refBody.FromRelVel(RelVel, body.GetPosition()));
            else body.SetVelocity(refBody.FromRelVel(RelVel, body.GetPosition()));
            body.SetAngularVelocity(refBody.FromRelAngVel(RelAngVel));
        }
    }

    // ship with player inside does some bs that we dont want. so this hack exists for that
    private static void ShipBody_SetVelocity(OWRigidbody body, Vector3 newVelocity)
    {
        if (body.RunningKinematicSimulation())
            body._kinematicRigidbody.velocity = newVelocity + Locator.GetCenterOfTheUniverse().GetStaticFrameVelocity_Internal();
        else
            body._rigidbody.velocity = newVelocity + Locator.GetCenterOfTheUniverse().GetStaticFrameVelocity_Internal();
        body._lastVelocity = body._currentVelocity;
        body._currentVelocity = newVelocity;
    }

    /// <summary>
    /// change all our location variables to be relative to the new reference
    /// </summary>
    public void ReferenceChanged(Transform oldRef, Transform newRef)
    {
        if (oldRef == null) return;
        
        var newRefBody = newRef.GetAttachedOWRigidbody();
        var oldRefBody = oldRef.GetAttachedOWRigidbody();

        var pos = qObject.Component.transform.position;
        RelVel = newRefBody.ToRelVel(oldRefBody.FromRelVel(RelVel, pos), pos);
        RelAngVel = newRefBody.ToRelAngVel(oldRefBody.FromRelAngVel(RelAngVel));
    }
}

[MessagePackObject]
public class VelocityMessage : QObjectMessage
{
    [Key(2)] public required Vector3 RelVel;
    [Key(3)] public required Vector3 RelAngVel;
    [Key(4)] public required float Time;

    public override void OnReceive(QObject.QObject qObject, int from, int to)
    {
        var sync = qObject.VelocitySync;

        if (Time < sync.PrevTime) return;
        sync.PrevTime = Time;

        sync.RelVel = RelVel;
        sync.RelAngVel = RelAngVel;
    }
}