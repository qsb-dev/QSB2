using MessagePack;
using QSB2.Messaging;
using QSB2.QObject;
using QSB2.SectorSync;

namespace QSB2.PositionSync;

public class RelativeToSector(QObject.QObject qObject)
{
    public QSector QSector;

    public void Tick()
    {
        if (qObject.HasOwner.DoWeOwn)
        {
            var sector = Locator.GetPlayerSectorDetector().GetLastEnteredSector();
            if (sector == null) return;
            QSector = (QSector)QObjectManager._componentToObject[sector];

            qObject.Send(new SectorMessage
            {
                SectorID = QSector.ID,
            }, -2);
        }
        else
        {
            var sector = (Sector)QSector.UnityComponent;
            qObject.PositionSync.Reference = sector.transform;
        }
    }
}

[MessagePackObject]
public class SectorMessage : QObjectMessage
{
    [Key(2)] public required int SectorID;

    public override void OnReceive(QObject.QObject qObject, int from, int to)
    {
        qObject.RelativeToSector.QSector = (QSector)QObjectManager.Entries[typeof(QSector).FullName.GetHashCode()].QObjects[SectorID];
    }
}