using MessagePack;
using QSB.Utility;
using QSB2.QObject;
using UnityEngine;

namespace QSB2.PositionSync;

public class PositionSync(QObject.QObject qObject)
{
    public Transform Reference;

    public Vector3 RelPos;
    public Quaternion RelRot;

    public void Tick()
    {
        if (Reference == null) return; // will result in thing floating around for a bit. maybe make invisible while thats happening
        
        if (qObject.Owner.DoWeOwn)
        {
            // owner - sync from unity component
            RelPos = Reference.ToRelPos(qObject.Component.transform.position);
            RelRot = Reference.ToRelRot(qObject.Component.transform.rotation);

            qObject.Send(new PositionMessage
            {
                RelPos = RelPos,
                RelRot = RelRot,
            }, -2);
        }
        else
        {
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

    public override void OnReceive(QObject.QObject qObject, int from, int to)
    {
        qObject.PositionSync.RelPos = RelPos;
        qObject.PositionSync.RelRot = RelRot;
    }
}