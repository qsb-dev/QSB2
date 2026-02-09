using System;
using System.Collections.Generic;
using System.Linq;
using QSB2.Messaging;
using QSB2.Player;
using QSB2.QObject;
using QSB2.SectorSync;
using QSB2.ShipSync;
using QSB2.WakeUpSync;
using Telepathy;
using UnityEngine;

namespace QSB2;

public static class NetworkManager
{
    public static readonly Client _client = new(1024);
    public static readonly Server _server = new(1024);

    public static bool IsConnected => _client.Connected;
    public static bool IsHost => _server.Active;

    static NetworkManager()
    {
        _server.OnConnected = (id, _) =>
        {
            Logger.Log($"server connected {id}");
            _serverClients.Add(id);

            var hostJoining = Connections.Count == 0;
            // new player knows nothing. fill them in
            new IdentifyMessage
            {
                QSBVersion = QSB2.QSBVersion,
                GameVersion = QSB2.GameVersion,
                DLCInstalled = QSB2.DLCInstalled,
                CanJoin = WakeUpManager.CanJoin || hostJoining,
                Connections = Connections.Values.Select(x => (x.ID, x.Scene, x.LoadCounter)).ToArray(),
            }.Send(id);
        };
        _server.OnDisconnected = id =>
        {
            Logger.Log($"server disconnected {id}");
            _serverClients.Remove(id);

            if (Connections.ContainsKey(id)) // mightve been kicked = no send join message
                // they cant say they left because they left. so we do it
                new LeaveMessage
                {
                    ID = id
                }.Send(-1);
        };
        _server.OnData = MessageManager.OnServerData;

        _client.OnConnected = () =>
        {
            Logger.Log("client connected");
            Application.runInBackground = true;
            WakeUpManager.CanJoin = true; // let us join on title screen
        };
        _client.OnDisconnected = () =>
        {
            Logger.Log("client disconnected");
            // just clear out everything, i dont care
            Connections.Clear();
            foreach (var entry in QObjectManager.Entries.Values)
            {
                entry.QObjects.Clear();
                entry.NextId = 0;
            }
            TickableManager.Tickables.Clear();
        };
        _client.OnData = MessageManager.OnData;
    }

    public static string Address;

    public static void Host()
    {
        var split = Address.Split(':');
        _server.Start(int.Parse(split[1]));
        Connect();
    }

    public static void Connect()
    {
        var split = Address.Split(':');
        _client.Connect(split[0], int.Parse(split[1]));
    }

    public static void Disconnect()
    {
        _client.Disconnect();
        _server.Stop();
    }

    public static void Tick()
    {
        _client.Tick(100);
        _server.Tick(100);
    }

    public static readonly List<int> _serverClients = new();
    public static int LocalID = -1;
    public static readonly Dictionary<int, Connection> Connections = new();
    public static Connection LocalConnection => Connections[LocalID];
}