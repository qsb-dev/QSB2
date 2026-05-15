using MessagePack;
using QSB.Utility;
using QSB2.QObject;
using QSB2.Utility;
using SteamTransport;
using UnityEngine;

namespace QSB2.PositionSync;

public class PositionSync(QObject.QObject qObject)
{
    public Transform Reference;

    public Vector3 RelPos;
    public Quaternion RelRot;
    public float PrevTime; // for dropping out of order messages

    public float UpdateInterval = .1f;
    private float _timer;

    public bool OccasionalMode;
    public bool Lerp = true;

    private Vector3 _lerpedRelPos;
    private Quaternion _lerpedRelRot;
    private Vector3 _currentVel;
    private Quaternion _currentAngVel;
    private Transform _lastReference;

    public void Tick()
    {
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

            qObject.Send(new PositionMessage
            {
                RelPos = RelPos,
                RelRot = RelRot,
                Time = Time.unscaledTime,
            }, -2, Channels.Unreliable);
        }
        else
        {
            if (OccasionalMode) return;

            if (_lastReference != Reference)
            {
                // update relative location since we've changed references
                // BUG: this doesnt change the damp vel/angvel so it looks weird
                _lerpedRelPos = RelPos = Reference.ToRelPos(qObject.Component.transform.position);
                _lerpedRelRot = RelRot = Reference.ToRelRot(qObject.Component.transform.rotation);
            }
            _lerpedRelPos = Lerp ? Vector3.SmoothDamp(_lerpedRelPos, RelPos, ref _currentVel, UpdateInterval) : RelPos;
            _lerpedRelRot = Lerp ? Quaternion.SmoothDamp(_lerpedRelRot, RelRot, ref _currentAngVel, UpdateInterval) : RelRot;
            _lastReference = Reference;

            // non owner - sync to unity component
            var body = qObject.Component.GetAttachedOWRigidbody();
            if (body)
            {
                body.SetPosition(Reference.FromRelPos(Lerp ? _lerpedRelPos : RelPos));
                body.SetRotation(Reference.FromRelRot(Lerp ? _lerpedRelRot : RelRot));
            }
            else
            {
                qObject.Component.transform.position = Reference.FromRelPos(Lerp ? _lerpedRelPos : RelPos);
                qObject.Component.transform.rotation = Reference.FromRelRot(Lerp ? _lerpedRelRot : RelRot);
            }
        }
    }

    public void Teleport()
    {
        _lerpedRelPos = RelPos;
        _lerpedRelRot = RelRot;
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