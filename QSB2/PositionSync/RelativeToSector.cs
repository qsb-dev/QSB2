using System.Linq;
using MessagePack;
using OWML.Common;
using QSB2.Messaging;
using QSB2.QObject;
using QSB2.SectorSync;
using QSB2.Utility;
using UnityEngine;

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

    // put closest sector calculation on a timer since its expensive ig. thatll just make it lag spike in intervals lollolol
    public float UpdateInterval = 1f;
    private float _timer;

    public void Tick()
    {
        if (qObject.Owner.ID == -1) return; // no owner = do nothing

        if (qObject.Owner.DoWeOwn)
        {
            // TODO: either remove this or have Teleport immediately trigger a recalc
            _timer += Time.unscaledDeltaTime;
            if (_timer < UpdateInterval) return;
            _timer = 0;

            var sector = GetClosestSector();
            if (sector == null) return;

            var qSector = sector.GetQObject<QSector>();

            // this is infrequent, so only send on change
            if (qSector != QSector)
            {
                qObject.Send(new ChangeSectorMessage
                {
                    SectorID = qSector.ID,
                }, SendTo.Others);

                var oldRef = qObject.PositionSync.Reference;
                var newRef = sector.transform;
                qObject.PositionSync.Reference = newRef;
                qObject.PositionSync.ReferenceChanged(oldRef, newRef);
                qObject.VelocitySync?.ReferenceChanged(oldRef, newRef);
            }

            QSector = qSector;
        }
        else
        {
            // could have all of this be in message receive
            // BUG: doing this here means that sometimes we'll get position relative to old reference for a bit
            if (QSector == null) return;

            var sector = QSector.Component;
            var oldRef = qObject.PositionSync.Reference;
            var newRef = sector.transform;
            if (newRef != oldRef)
            {
                qObject.PositionSync.Reference = newRef;
                qObject.PositionSync.ReferenceChanged(oldRef, newRef);
                qObject.VelocitySync?.ReferenceChanged(oldRef, newRef);
            }
        }
    }

    #region closest sector heuristic

    private static Sector[] _cachedSectors;

    private Sector GetClosestSector()
    {
        var validSectors = SectorDetector._sectorList
            .Where(ShouldSyncTo)
            .ToList();

        if (validSectors.Count == 0)
        {
            if (_cachedSectors?.FirstOrDefault() == null)
            {
                _cachedSectors = Extensions.GetAllComponents<Sector>().ToArray();
            }

            validSectors = _cachedSectors
                .Where(x =>
                    // we only wanna sync to the major ones when far away
                    x.GetName() != Sector.Name.Unnamed &&
                    ShouldSyncTo(x))
                .ToList();
        }

        if (validSectors.Count == 0)
        {
            return null;
        }

        return validSectors
            .MinBy(GetPenaltyScore);
    }

    private static EyeShuttleController _cachedShuttleController;

    private bool ShouldSyncTo(Sector sector)
    {
        var occupantType = SectorDetector._occupantType;

        // if we're the ship, don't sync to own sector
        if (occupantType == DynamicOccupant.Ship && sector.GetName() == Sector.Name.Ship)
        {
            return false;
        }

        if (!sector.gameObject.activeInHierarchy)
        {
            return false;
        }

        // ig we gotta check if we're in the shuttle
        if (sector.name is "Sector_Shuttle" or "Sector_NomaiShuttleInterior")
        {
            if (LoadManager.GetCurrentScene() == OWScene.SolarSystem)
            {
                var shuttleController = sector.gameObject.GetComponentInParent<NomaiShuttleController>();
                if (shuttleController == null)
                {
                    Logger.Log($"Warning - Expected to find a NomaiShuttleController for {sector.name}!", MessageType.Warning);
                    return false;
                }

                if (!shuttleController.IsPlayerInside())
                {
                    return false;
                }
            }
            else if (LoadManager.GetCurrentScene() == OWScene.EyeOfTheUniverse)
            {
                if (!_cachedShuttleController)
                {
                    _cachedShuttleController = Extensions.GetAllComponents<EyeShuttleController>().Single();
                }

                if (!_cachedShuttleController._isPlayerInside)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private float GetPenaltyScore(Sector sector)
    {
        var rigidbody = SectorDetector._attachedRigidbody;

        // farther away = worse
        var sqrDistance = (sector._triggerRoot.transform.position - rigidbody.GetPosition()).sqrMagnitude;
        // bigger radius is usually not subsector = worse
        var radius = GetRadius(sector);
        // we wanna be moving at similar speeds (this is mainly for timeloop ring)
        var sqrVelocity = GetSqrVelocity(sector);

        return sqrDistance + radius * radius + sqrVelocity;
    }

    private float GetRadius(Sector sector)
    {
        // TODO : make this work for other stuff, not just shaped triggervolumes
        var trigger = sector.GetTriggerVolume();
        if (trigger && trigger.GetShape())
        {
            return trigger.GetShape().CalcWorldBounds().radius;
        }

        return 0f;
    }

    private float GetSqrVelocity(Sector sector)
    {
        var rigidbody = SectorDetector._attachedRigidbody;

        var sectorRigidbody = sector.GetOWRigidbody();
        if (sectorRigidbody && rigidbody)
        {
            var relativeVelocity = rigidbody.GetVelocity() - sectorRigidbody.GetPointVelocity(rigidbody.GetPosition());
            return relativeVelocity.sqrMagnitude;
        }

        return 0;
    }

    #endregion
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