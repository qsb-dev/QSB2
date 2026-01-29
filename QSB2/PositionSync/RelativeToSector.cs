using System;
using MessagePack;
using QSB2.Authority;
using QSB2.Messaging;
using QSB2.QObject;
using QSB2.SectorSync;
using UnityEngine;

namespace QSB2.PositionSync;

public class RelativeToSector : MonoBehaviour
{
    private QObject.QObject _qObject;
    private PositionSync _positionSync;
    private HasOwner _hasOwner;

    public QSector QSector;

    private void Start()
    {
        _qObject = GetComponent<QObject.QObject>();
        _positionSync = GetComponent<PositionSync>();
        _hasOwner = GetComponent<HasOwner>();
    }

    private void Update()
    {
        if (!_hasOwner.DoWeOwn) return;

        var sector = Locator.GetPlayerSectorDetector().GetLastEnteredSector();
        if (sector == null) return;
        QSector = (QSector)QObjectManager._componentToObject[sector];

        _qObject.Send(new SectorMessage
        {
            SectorID = QSector.ID,
        }, -2);
    }

    public void Receive(int id)
    {
        QSector = (QSector)QObjectManager.Entries[typeof(QSector).FullName.GetHashCode()].QObjects[id];
        var sector = (Sector)QSector.UnityComponent;
        _positionSync.Reference = sector.transform;
    }
}

[MessagePackObject]
public class SectorMessage : QObjectMessage
{
    [Key(2)] public required int SectorID;

    public override void OnReceive(QObject.QObject qObject, int from, int to)
    {
        qObject.GetComponent<RelativeToSector>().Receive(SectorID);
    }
}