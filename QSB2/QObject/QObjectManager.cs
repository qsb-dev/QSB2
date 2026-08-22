using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OWML.Common;
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

    private static readonly List<QObjectBuilder> _builders = new();

    static QObjectManager()
    {
        foreach (var type in typeof(QObject).GetDerivedTypes())
            Entries.Add(type.Hash(), new(type));

        foreach (var type in typeof(QObjectBuilder).GetDerivedTypes())
            _builders.Add((QObjectBuilder)Activator.CreateInstance(type));

        QSceneManager.OnPreSceneLoad += (originalScene, loadScene) =>
        {
            if (!loadScene.IsGameScene())
            {
                NetworkManager.Disconnect();
                return;
            }

            if (!originalScene.IsGameScene()) return;

            QSB2.Instance.StartCoroutine(BuildersDestroy());
        };

        QSceneManager.OnPostSceneLoad += (originalScene, loadScene) =>
        {
            if (!loadScene.IsGameScene()) return;

            QSB2.Instance.StartCoroutine(BuildersCreate());
        };
    }

    // idk if spreading these over multiple frames will ever be necessary
    private static IEnumerator BuildersCreate()
    {
        // wait until all the right flags are good before we create our things. coordinates with wake up sync
        yield return new WaitUntil(() => LateInitializerManager.isDoneInitializing && WakeUpManager.AllScenesSame && !WakeUpManager.HostWaitingForPlayers);

        var sw = Stopwatch.StartNew();
        foreach (var builder in _builders)
        {
            if (!NetworkManager.IsConnected) yield break;
            
            try
            {
                builder.Create();
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString(), MessageType.Error);
            }

            if (sw.Elapsed.TotalSeconds > 1 / 15f)
            {
                sw.Restart();
                Logger.Log("NEXT FRAME");
                yield return null;
            }
        }
    }

    private static IEnumerator BuildersDestroy()
    {
        var sw = Stopwatch.StartNew();
        foreach (var builder in _builders)
        {
            if (!NetworkManager.IsConnected) yield break;
            
            try
            {
                builder.Destroy();
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString(), MessageType.Error);
            }

            if (sw.Elapsed.TotalSeconds > 1 / 15f)
            {
                sw.Restart();
                Logger.Log("NEXT FRAME");
                yield return null;
            }
        }
    }

    #region utils

    public static IEnumerable<T> GetQObjects<T>() where T : QObject => Entries[typeof(T).Hash()].QObjects.Values.Cast<T>();

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