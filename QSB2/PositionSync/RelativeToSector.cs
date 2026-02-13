using MessagePack;
using QSB2.QObject;
using QSB2.SectorSync;
using QSB2.Utility;

namespace QSB2.PositionSync;

public class RelativeToSector(QObject.QObject qObject)
{
    public SectorDetector SectorDetector;
    public QSector QSector;

    public void Tick()
    {
        if (qObject.Owner.ID == -1) return; // no owner = do nothing
        
        if (qObject.Owner.DoWeOwn)
        {
            var sector = SectorDetector.GetLastEnteredSector();
            if (sector == null) return;
            QSector = (QSector)QObjectManager._componentToObject[sector];

            qObject.Send(new ChangeSectorMessage
            {
                SectorID = QSector.ID,
            }, -2, true);
            
            qObject.PositionSync.Reference = sector.transform;
        }
        else
        {
            if (QSector == null) return;
            
            var sector = QSector.Component;
            qObject.PositionSync.Reference = sector.transform;
        }
    }
}

[MessagePackObject]
public class ChangeSectorMessage : QObjectMessage
{
    [Key(2)] public required int SectorID;

    public override void OnReceive(QObject.QObject qObject, int from, int to)
    {
        qObject.RelativeToSector.QSector = (QSector)QObjectManager.Entries[typeof(QSector).Hash()].QObjects[SectorID];
    }
}