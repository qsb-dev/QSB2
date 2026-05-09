using System;
using System.Collections.Generic;
using System.Linq;
using OWML.Common;
using QSB2.QObject;
using QSB2.Utility;
using UnityEngine;

namespace QSB2.SectorSync;

public class QSectorBuilder : QObjectBuilder<QSector, Sector>
{
    public override void Create()
    {
        CreateFakeSectors();

        base.Create();
    }


    // painstakingly manually crafted in qsb1
    private static void CreateFakeSectors()
    {
        if (LoadManager.GetCurrentScene() != OWScene.SolarSystem)
        {
            return;
        }

        // time loop spinning ring
        {
            var TimeLoopRing_Body = GameObject.Find("TimeLoopRing_Body");
            var Sector_TimeLoopInterior = GameObject.Find("Sector_TimeLoopInterior").GetComponent<Sector>();
            // use the same trigger as the parent sector
            FakeSector.Create(TimeLoopRing_Body, Sector_TimeLoopInterior,
                x => x._triggerRoot = Sector_TimeLoopInterior._triggerRoot);
        }

        // TH elevators
        foreach (var elevator in Extensions.GetAllComponents<Elevator>())
        {
            FakeSector.Create(elevator.gameObject,
                elevator.GetComponentInParent<Sector>(),
                x => x._triggerRoot = elevator.gameObject);
        }

        // rafts
        foreach (var raft in Extensions.GetAllComponents<RaftController>())
        {
            FakeSector.Create(raft.gameObject,
                raft._sector,
                x => x._triggerRoot = raft._rideVolume.gameObject);
        }

        // cage elevators
        foreach (var cageElevator in Extensions.GetAllComponents<CageElevator>())
        {
            FakeSector.Create(cageElevator._platformBody.gameObject,
                cageElevator.gameObject.GetComponentInParent<Sector>(),
                x =>
                {
                    x.gameObject.AddComponent<OWTriggerVolume>();
                    var shape = x.gameObject.AddComponent<BoxShape>();
                    shape.size = new Vector3(2.5f, 4.25f, 2.5f);
                    shape.center = new Vector3(0, 2.15f, 0);

                    // When the cage elevator warps when entering/exiting the underground,
                    // the player's sector detector is removed from the fake sector.
                    // So when the elevator is moving and they leave the sector, it means they have warped
                    // and should be added back in.
                    x.OnOccupantExitSector.AddListener((e) =>
                    {
                        if (cageElevator.isMoving) x.AddOccupant(e);
                    });
                });
        }

        // prisoner elevator
        {
            var prisonerElevator = Extensions.GetAllComponents<PrisonCellElevator>().Single();
            FakeSector.Create(prisonerElevator._elevatorBody.gameObject,
                prisonerElevator.gameObject.GetComponentInParent<Sector>(),
                x =>
                {
                    x.gameObject.AddComponent<OWTriggerVolume>();
                    var shape = x.gameObject.AddComponent<BoxShape>();
                    shape.size = new Vector3(4f, 6.75f, 6.7f);
                    shape.center = new Vector3(0, 3.3f, 3.2f);
                });
        }


        //black hole forge
        {
            var forge = GameObject.Find("BlackHoleForgePivot");
            FakeSector.Create(forge,
                forge.GetComponentInParent<Sector>(),
                x =>
                {
                    x._triggerRoot = GameObject.Find("BrittleHollow_Body/Sector_BH/Sector_NorthHemisphere/Sector_NorthPole/" +
                                                     "Sector_HangingCity/Sector_HangingCity_BlackHoleForge/BlackHoleForgePivot/" +
                                                     "Volumes_BlackHoleForge/DirectionalForceVolume");
                });
        }

        // black hole forge entrance elevator
        {
            var entrance = GameObject.Find("BlackHoleForge_EntrancePivot");
            var sector = GameObject.Find("Sector_HangingCity_BlackHoleForge").GetComponent<Sector>();
            FakeSector.Create(entrance,
                sector,
                x =>
                {
                    x.gameObject.AddComponent<OWTriggerVolume>();
                    var shape = x.gameObject.AddComponent<BoxShape>();
                    shape.size = new Vector3(5.5f, 5.8f, 5.5f);
                    shape.center = new Vector3(0f, 2.9f, 1.5f);
                });
        }

        // OPC probe
        {
            var probe = Locator._orbitalProbeCannon
                .GetRequiredComponent<OrbitalProbeLaunchController>()
                ._probeBody;
            if (probe)
            {
                // just create a big circle around the probe lol
                FakeSector.Create(probe.gameObject,
                    null,
                    x =>
                    {
                        x.gameObject.AddComponent<OWTriggerVolume>();
                        x.gameObject.AddComponent<SphereShape>().radius = 100;
                    });
            }
        }
    }
}

// man i wrote some bs here for qsb1
public class FakeSector : Sector
{
    public static void Create(GameObject go, Sector parent, Action<FakeSector> setupSector)
    {
        var name = $"FakeSector_{go.name}";
        if (go.transform.Find(name))
        {
            Logger.Log($"{name} already exists", MessageType.Warning);
            return;
        }

        var go2 = new GameObject(name);
        go2.SetActive(false);
        go2.transform.SetParent(go.transform, false);

        var fakeSector = go2.AddComponent<FakeSector>();
        fakeSector._name = (Name)(-1);
        fakeSector._subsectors = new List<Sector>();
        fakeSector._idString = name;
        fakeSector.SetParentSector(parent);
        setupSector(fakeSector);

        go2.SetActive(true);
    }
}