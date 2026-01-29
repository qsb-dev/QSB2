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
        public Type Type = type;
        public int NextId;
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
            if (!NetworkManager.Connected) return;
            if (!originalScene.IsInGameScene()) return;

            PlayerManager.Destroy();
            QShipManager.Destroy();
            QSectorManager.Destroy();
        };
        QSceneManager.OnPostSceneLoad += (originalScene, loadScene) =>
        {
            if (!NetworkManager.Connected) return;
            if (!loadScene.IsInGameScene()) return;

            Delay.RunWhen(() => LateInitializerManager.isDoneInitializing, () =>
            {
                PlayerManager.Create();
                QShipManager.Create();
                QSectorManager.Create();
            });
        };
    }

    public static void Init()
    {
    }
}