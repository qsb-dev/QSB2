using MessagePack;
using QSB2.QObject;
using QSB2.SectorSync;

namespace QSB2.PositionSync;

/// <summary>
/// for objects who change sectors frequently and need to position themselves relative to those sectors
/// </summary>
public class RelativeToSector(QObject.QObject qObject)
{
    public SectorDetector SectorDetector;
    public QSector QSector;

    /// <summary>
    /// non owners take a bit to receive the first sector message.
    /// mainly results in the object floating around a bit because its position is not set.
    /// could potentially just tell it to use the sun instead.
    /// TODO: somehow account for this in the initialization process?
    /// </summary>
    public bool SectorSet => QSector != null;

    public void Tick()
    {
        if (qObject.Owner.ID == -1) return; // no owner = do nothing

        if (qObject.Owner.DoWeOwn)
        {
            var sector = SectorDetector.GetLastEnteredSector(); // TODO: replace with heuristic
            if (sector == null) return;

            var qSector = sector.GetQObject<QSector>();

            // this is infrequent, so only send on change
            if (QSector != qSector)
            {
                qObject.Send(new ChangeSectorMessage
                {
                    SectorID = QSector.ID,
                }, -2);

                qObject.PositionSync.Reference = sector.transform;
            }

            QSector = qSector;
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