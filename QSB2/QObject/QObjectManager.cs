using System;
using System.Collections.Generic;
using QSB2.OrbSync;
using QSB2.PlayerSync;
using QSB2.ProbeSync;
using QSB2.SectorSync;
using QSB2.ShipSync;
using QSB2.Utility;
using QSB2.WakeUpSync;
using UnityEngine;

namespace QSB2.QObject;

public static class QObjectManager
{
    // TODO: entry per object manager instead of per qobject? if one manager does multiple subclasses like item
    public class Entry(Type type)
    {
        public readonly Type Type = type;
        public int NextId;
        public readonly Dictionary<int, QObject> QObjects = new();
    }

    public static readonly Dictionary<int, Entry> Entries = new();

    internal static readonly Dictionary<Component, QObject> _componentToObject = new();

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

            // TODO: refactor
            PlayerManager.Destroy();
            ProbeManager.Destroy();
            QShipManager.Destroy();
            QSectorManager.Destroy();
            OrbManager.Destroy();
        };
        QSceneManager.OnPostSceneLoad += (originalScene, loadScene) =>
        {
            if (!NetworkManager.IsConnected) return;
            if (!loadScene.IsGameScene()) return;

            Delay.RunWhen(() => LateInitializerManager.isDoneInitializing && WakeUpManager.AllScenesSame && !WakeUpManager.HostWaitingForPlayers, () =>
            {
                PlayerManager.Create();
                ProbeManager.Create();
                QShipManager.Create();
                QSectorManager.Create();
                OrbManager.Create();
            });
        };

        // leave if not in game scene
        QSceneManager.OnPreSceneLoad += (originalScene, loadScene) =>
        {
            if (!loadScene.IsGameScene()) NetworkManager.Disconnect();
        };
    }

    #region utils

    public static T GetQObject<T>(this Component component) where T : QObject, new()
    {
        if (!WakeUpManager.AllQObjectsCreated) throw new Exception($"tried to get {typeof(T)} from {component} when not all qobjects created");
        if (!_componentToObject.TryGetValue(component, out var qObject)) throw new ArgumentException($"could not find {typeof(T)} for {component}");
        if (qObject is not T t) throw new ArgumentException($"could not find {typeof(T)} for {component} (got {qObject.GetType()} instead)");
        return t;
    }

    public static T GetQObject<T>(this int id) where T : QObject, new()
    {
        if (!WakeUpManager.AllQObjectsCreated) throw new Exception($"tried to get {typeof(T)} from id {id} when not all qobjects created");
        var entry = Entries[typeof(T).Hash()];
        if (!entry.QObjects.TryGetValue(id, out var qObject)) throw new ArgumentException($"could not find {typeof(T)} for id {id}");
        return (T)qObject;
    }

    #endregion
}