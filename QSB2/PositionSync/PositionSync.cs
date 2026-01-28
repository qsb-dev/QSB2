using MessagePack;
using QSB.Utility;
using QSB2.Authority;
using QSB2.Messaging;
using UnityEngine;

namespace QSB2.PositionSync;

[RequireComponent(typeof(QObject.QObject))]
[RequireComponent(typeof(HasOwner))]
public class PositionSync : MonoBehaviour
{
    public Transform Reference;

    private QObject.QObject _qObject;
    private HasOwner _hasOwner;

    private void Start()
    {
        _qObject = GetComponent<QObject.QObject>();
        _hasOwner = GetComponent<HasOwner>();
    }

    private void Update()
    {
        if (_hasOwner.Owner != NetworkManager.LocalID) return;

        // owner - sync from unity component
        transform.position = Reference.ToRelPos(_qObject.UnityComponent.transform.position);
        transform.rotation = Reference.ToRelRot(_qObject.UnityComponent.transform.rotation);
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
    [Key(2)] public Vector3 Position;
    [Key(3)] public Quaternion Rotation;

    public override void OnReceive(QObject.QObject qObject, int from, int to)
    {
        qObject.GetComponent<PositionSync>().Receive(Position, Rotation);
    }
}