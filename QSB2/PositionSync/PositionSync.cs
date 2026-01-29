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
        if (qObject.HasOwner.DoWeOwn)
        {
            // owner - sync from unity component
            RelPos = Reference.ToRelPos(qObject.UnityComponent.transform.position);
            RelRot = Reference.ToRelRot(qObject.UnityComponent.transform.rotation);

            qObject.Send(new PositionMessage
            {
                Position = RelPos,
                Rotation = RelRot,
            }, -2);
        }
        else
        {
            // non owner - sync to unity component
            qObject.UnityComponent.transform.position = Reference.FromRelPos(RelPos);
            qObject.UnityComponent.transform.rotation = Reference.FromRelRot(RelRot);
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
    [Key(2)] public required Vector3 Position;
    [Key(3)] public required Quaternion Rotation;

    public override void OnReceive(QObject.QObject qObject, int from, int to)
    {
        qObject.PositionSync.RelPos = Position;
        qObject.PositionSync.RelRot = Rotation;
    }
}