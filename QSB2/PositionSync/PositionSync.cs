using MessagePack;
using QSB.Utility;
using QSB2.Authority;
using QSB2.Messaging;
using UnityEngine;

namespace QSB2.PositionSync;

public class PositionSync : MonoBehaviour
{
    public Transform Reference;

    private QObject.QObject _qObject;
    private HasOwner _hasOwner;

    private void Start()
    {
        _qObject = GetComponent<QObject.QObject>();
        _hasOwner = GetComponent<HasOwner>();
        
        // give it some sane value
        Reference = CenterOfTheUniverse.s_instance._staticReferenceFrame.transform;
    }

    private void Update()
    {
        if (!_hasOwner.DoWeOwn) return;

        // owner - sync from unity component
        transform.position = Reference.ToRelPos(_qObject.UnityComponent.transform.position);
        transform.rotation = Reference.ToRelRot(_qObject.UnityComponent.transform.rotation);
        
        _qObject.SendMessage(new PositionMessage
        {
            Position = transform.position,
            Rotation = transform.rotation,
        }, -2);
    }

    public void Receive(Vector3 position, Quaternion rotation)
    {
        // non owner - sync to unity component
        transform.position = position;
        transform.rotation = rotation;

        _qObject.UnityComponent.transform.position = Reference.FromRelPos(transform.position);
        _qObject.UnityComponent.transform.rotation = Reference.FromRelRot(transform.rotation);
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
        qObject.GetComponent<PositionSync>().Receive(Position, Rotation);
    }
}