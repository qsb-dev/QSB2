using MessagePack;
using QSB2.Messaging;
using QSB2.QObject;
using QSB2.Utility;
using SteamTransport;
using UnityEngine;

namespace QSB2.PositionSync;

public class PositionSync(QObject.QObject qObject)
{
    public Transform Reference;

    public Vector3 RelPos = Vector3.zero;
    public Quaternion RelRot = Quaternion.identity;
    public float PrevTime; // for dropping out of order messages

    public float UpdateInterval = .1f;
    private float _timer;

    public bool OccasionalMode;
    public bool Lerp = true;

    private Vector3 _lerpedRelPos = Vector3.zero;
    private Quaternion _lerpedRelRot = Quaternion.identity;
    private Vector3 _currentVel = Vector3.zero;
    private Quaternion _currentAngVel = Quaternion.identity;

    public void Tick()
    {
        if (DebugGui.ShowGizmos && Reference != null)
        {
            /*
             * Red Cube = Where visible object should be
             * Green cube = Where visible object is
             * Red Line = Connection between Red Cube and Green Cube
             * Magenta cube = Reference transform
             * Cyan Line = Connection between Green cube and Magenta cube
             */

            Popcron.Gizmos.Cube(Reference.FromRelPos(RelPos), Reference.FromRelRot(RelRot), Vector3.one / 8, Color.red);
            Popcron.Gizmos.Cube(qObject.Component.transform.position, qObject.Component.transform.rotation, Vector3.one / 6, Color.green);
            Popcron.Gizmos.Line(Reference.FromRelPos(RelPos), qObject.Component.transform.position, Color.red);
            Popcron.Gizmos.Cube(Reference.position, Reference.rotation, Vector3.one / 8, Color.magenta);
            Popcron.Gizmos.Line(qObject.Component.transform.position, Reference.position, Color.cyan);
        }

        if (qObject.Owner.ID == -1) return; // no owner = do nothing

        if (Reference == null) return; // happens with RelativeToSector usually

        if (qObject.Owner.DoWeOwn)
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer < UpdateInterval) return;
            _timer = 0;

            // owner - sync from unity component
            _lerpedRelPos = RelPos = Reference.ToRelPos(qObject.Component.transform.position);
            _lerpedRelRot = RelRot = Reference.ToRelRot(qObject.Component.transform.rotation);
            _currentVel = Vector3.zero;
            _currentAngVel = Quaternion.identity;

            qObject.Send(new PositionMessage
            {
                RelPos = RelPos,
                RelRot = RelRot,
                Time = Time.unscaledTime,
            }, SendTo.Others, Channels.Unreliable);
        }
        else
        {
            // TODO: this is always opposite of lerp. just have non lerp mode set position in message receive
            if (OccasionalMode) return;

            if (Lerp)
            {
                _lerpedRelPos = Vector3.SmoothDamp(_lerpedRelPos, RelPos, ref _currentVel, UpdateInterval);
                _lerpedRelRot = Quaternion.SmoothDamp(_lerpedRelRot, RelRot, ref _currentAngVel, UpdateInterval);
            }
            else
            {
                _lerpedRelPos = RelPos;
                _lerpedRelRot = RelRot;
                _currentVel = Vector3.zero;
                _currentAngVel = Quaternion.identity;
            }

            // non owner - sync to unity component
            var body = qObject.Component.GetAttachedOWRigidbody();
            if (body)
            {
                body.SetPosition(Reference.FromRelPos(_lerpedRelPos));
                body.SetRotation(Reference.FromRelRot(_lerpedRelRot));
            }
            else
            {
                qObject.Component.transform.position = Reference.FromRelPos(_lerpedRelPos);
                qObject.Component.transform.rotation = Reference.FromRelRot(_lerpedRelRot);
            }
        }
    }

    public void Teleport()
    {
        _lerpedRelPos = RelPos;
        _lerpedRelRot = RelRot;
        _currentVel = Vector3.zero;
        _currentAngVel = Quaternion.identity;

        // TODO: turn into teleport message that instantly sends over location, velocity, and sector data
    }

    /// <summary>
    /// change all our location variables to be relative to the new reference
    /// </summary>
    public void ReferenceChanged(Transform oldRef, Transform newRef)
    {
        if (oldRef == null) return;

        if (Lerp)
        {
            var oldRefBody = oldRef.GetAttachedOWRigidbody();
            var newRefBody = newRef.GetAttachedOWRigidbody();

            RelPos = newRef.ToRelPos(oldRef.FromRelPos(RelPos));
            RelRot = newRef.ToRelRot(oldRef.FromRelRot(RelRot));
            _lerpedRelPos = newRef.ToRelPos(oldRef.FromRelPos(_lerpedRelPos));
            _lerpedRelRot = newRef.ToRelRot(oldRef.FromRelRot(_lerpedRelRot));
            var pos = qObject.Component.transform.position;
            // BUG: this seems to not work at all. zero values might just be better
            _currentVel = newRefBody.ToRelVel(oldRefBody.FromRelVel(_currentVel, pos), pos);
            _currentAngVel = Quaternion.identity; // dont really know how to handle this. its less noticeable
        }
        else
        {
            RelPos = newRef.ToRelPos(oldRef.FromRelPos(RelPos));
            RelRot = newRef.ToRelRot(oldRef.FromRelRot(RelRot));
        }
    }
}

[MessagePackObject]
public class PositionMessage : QObjectMessage
{
    [Key(2)] public required Vector3 RelPos;
    [Key(3)] public required Quaternion RelRot;
    [Key(4)] public required float Time;

    public override void OnReceive(QObject.QObject qObject, int from, int to)
    {
        var sync = qObject.PositionSync;

        if (Time < sync.PrevTime) return;
        sync.PrevTime = Time;

        if (sync.OccasionalMode)
        {
            // BUG: doesnt set rel stuff or lerped rel stuff
            var body = qObject.Component.GetAttachedOWRigidbody();
            if (body)
            {
                body.SetPosition(sync.Reference.FromRelPos(RelPos));
                body.SetRotation(sync.Reference.FromRelRot(RelRot));
            }
            else
            {
                qObject.Component.transform.position = sync.Reference.FromRelPos(RelPos);
                qObject.Component.transform.rotation = sync.Reference.FromRelRot(RelRot);
            }
        }
        else
        {
            sync.RelPos = RelPos;
            sync.RelRot = RelRot;
        }
    }
}