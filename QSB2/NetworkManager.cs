using System;
using System.Collections.Generic;
using System.Reflection;
using OWML.Common;
using QSB2.Messaging;
using QSB2.QObject;
using Telepathy;
using UnityEngine;

namespace QSB2;

public static class NetworkManager
{
    public static readonly Client _client = new(1024);
    public static readonly Server _server = new(1024);

    public static bool Connected => _client.Connected;
    public static bool IsHost => _server.Active;

    public static void Host()
    {
        _server.Start(1337);
        Connect();

        _server.OnConnected = (id, _) =>
        {
            Logger.Log($"server connected {id}");

            _serverClients.Add(id);
            new IdentifyMessage
            {
                QSBVersion = QSB2.QSBVersion,
                GameVersion = QSB2.GameVersion,
                DLCInstalled = QSB2.DLCInstalled,
            }.Send(id);

            // new player knows nothing. fill them in
            new SyncMessage
            {
                IDs = _serverClients
            }.Send(id);
        };
        _server.OnDisconnected = id =>
        {
            Logger.Log($"server disconnected {id}");

            _serverClients.Remove(id);
            // they cant say they left because they left. so we do it
            new LeaveMessage
            {
                ID = id
            }.Send(-1);
        };
        _server.OnData = MessageManager.OnServerData;
    }

    public static void Connect()
    {
        Application.runInBackground = true;

        _client.Connect("localhost", 1337);
        _client.OnConnected = () =>
        {
            Logger.Log("client connected");
            QSB2.Harmony.PatchAll(Assembly.GetExecutingAssembly());
        };
        _client.OnDisconnected = () =>
        {
            Logger.Log("client disconnected");
            QSB2.Harmony.UnpatchSelf();
        };
        _client.OnData = MessageManager.OnData;
    }

    public static void Disconnect()
    {
        _client.Disconnect();
        _server.Stop();
    }

    public static void Tick()
    {
        TickableManager.Tick();
        _client.Tick(100);
        _server.Tick(100);
    }

    public static readonly List<int> _serverClients = new();
    public static int LocalID = -1;
    public static readonly Dictionary<int, Connection> Connections = new();
}