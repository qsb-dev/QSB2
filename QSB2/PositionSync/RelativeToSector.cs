using MessagePack;
using QSB2.QObject;
using QSB2.SectorSync;
using SteamTransport;

namespace QSB2.PositionSync;

public class RelativeToSector(QObject.QObject qObject)
{
    public SectorDetector SectorDetector;
    public QSector QSector;
    /// <summary>
    /// non owners take a bit to receive the first sector message.
    /// TODO: somehow account for this in the initialization process?
    /// </summary>
    public bool SectorSet => QSector != null;

    public void Tick()
    {
        if (qObject.Owner.ID == -1) return; // no owner = do nothing
        
        if (qObject.Owner.DoWeOwn)
        {
            var sector = SectorDetector.GetLastEnteredSector();
            if (sector == null) return;
            QSector = sector.GetQObject<QSector>();

            qObject.Send(new ChangeSectorMessage
            {
                SectorID = QSector.ID,
            }, -2, Channels.Unreliable);
            
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
        qObject.RelativeToSector.QSector = SectorID.GetQObject<QSector>();
    }
}