using MessagePack;
using QSB.Utility;
using QSB2.QObject;
using SteamTransport;
using UnityEngine;

namespace QSB2.PositionSync;

public class PositionSync(QObject.QObject qObject)
{
    public Transform Reference;

    public Vector3 RelPos;
    public Quaternion RelRot;
    public float PrevTime; // for dropping out of order messages

    public float UpdateInterval = 0f;
    private float _timer;

    public bool SetConstantly = true;

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
            RelPos = Reference.ToRelPos(qObject.Component.transform.position);
            RelRot = Reference.ToRelRot(qObject.Component.transform.rotation);

            qObject.Send(new PositionMessage
            {
                RelPos = RelPos,
                RelRot = RelRot,
                Time = Time.unscaledTime,
            }, -2, Channels.Unreliable);
        }
        else
        {
            if (!SetConstantly)
            {
                _timer += Time.unscaledDeltaTime;
                if (_timer < UpdateInterval) return;
                _timer = 0;
            }

            // non owner - sync to unity component
            var body = qObject.Component.GetAttachedOWRigidbody();
            if (body)
            {
                body.SetPosition(Reference.FromRelPos(RelPos));
                body.SetRotation(Reference.FromRelRot(RelRot));
            }
            else
            {
                qObject.Component.transform.position = Reference.FromRelPos(RelPos);
                qObject.Component.transform.rotation = Reference.FromRelRot(RelRot);
            }
        }
    }

    public void Teleport()
    {
        // TODO: eventually when we do smooth guy, this acts as a "please dont lerp" indicator
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
        if (Time < qObject.PositionSync.PrevTime) return;
        qObject.PositionSync.RelPos = RelPos;
        qObject.PositionSync.RelRot = RelRot;
        qObject.PositionSync.PrevTime = Time;
    }
}