using System;
using System.Collections.Generic;
using QSB2.Player;
using QSB2.SectorSync;
using QSB2.ShipSync;
using QSB2.Utility;
using UnityEngine;

namespace QSB2.QObject;

public static class QObjectManager
{
    public class Entry(Type type)
    {
        public readonly Type Type = type;
        public int NextId; // not reset between loops, so ids in one loop will be totally different from ids in another loop
        public readonly Dictionary<int, QObject> QObjects = new();
        public readonly HashSet<int> CreatedFor = new();
    }

    public static readonly Dictionary<int, Entry> Entries = new();

    public static readonly Dictionary<Component, QObject> _componentToObject = new();

    static QObjectManager()
    {
        foreach (var type in typeof(QObject).GetDerivedTypes())
        {
            Entries.Add(type.Hash(), new(type));
        }

        QSceneManager.OnPreSceneLoad += (originalScene, loadScene) =>
        {
            if (!NetworkManager.IsConnected) return;
            if (!originalScene.IsGameScene()) return;

            PlayerManager.Destroy();
            QShipManager.Destroy();
            QSectorManager.Destroy();
        };
        QSceneManager.OnPostSceneLoad += (originalScene, loadScene) =>
        {
            if (!NetworkManager.IsConnected) return;
            if (!loadScene.IsGameScene()) return;

            Delay.RunWhen(() => LateInitializerManager.isDoneInitializing, () =>
            {
                PlayerManager.Create();
                QShipManager.Create();
                QSectorManager.Create();
            });
        };

        // leave if not in game scene
        QSceneManager.OnPreSceneLoad += (originalScene, loadScene) =>
        {
            if (!loadScene.IsGameScene()) NetworkManager.Disconnect();
        };
    }

    public static void Init()
    {
    }
}