using System.Collections.Generic;
using System.Linq;
using QSB2.Messaging;
using QSB2.QObject;
using QSB2.WakeUpSync;
using SteamTransport;
using UnityEngine;

namespace QSB2;

public static class NetworkManager
{
    public static Client _client; // nonhost has this
    public static Server _server; // host has this

    public static bool IsConnected => (_client?.IsConnected ?? false) || (_server?.IsListening ?? false);
    public static bool IsHost => _server?.IsListening ?? false;

    public static string Address;

    public static void Host()
    {
        if (_server != null || _client != null) return; // TODO: tell player
        
        {
            _server = new(new() { Log = s => Logger.Log(($"[server] {s}")) });
            _server.OnConnected = (id) =>
            {
                Logger.Log($"server connected {id}");
                _serverClients.Add(id);

                // new player knows nothing. fill them in
                new IdentifyMessage
                {
                    QSBVersion = QSB2.QSBVersion,
                    GameVersion = QSB2.GameVersion,
                    DLCInstalled = QSB2.DLCInstalled,
                    CanJoin = WakeUpManager.CanJoin,
                    Connections = Connections.Values.Select(x => (x.ID, x.Name, x.Scene, x.LoadCounter)).ToArray(),
                    HostWaitingForPlayers = WakeUpManager.HostWaitingForPlayers,
                }.Send(id);
            };
            _server.OnDisconnected = (id, reason) =>
            {
                Logger.Log($"server disconnected {id} because {reason}");
                _serverClients.Remove(id);

                if (Connections.ContainsKey(id)) // mightve been kicked = no send join message
                    // they cant say they left because they left. so we do it
                    new LeaveMessage
                    {
                        ID = id
                    }.Send(-1);
            };
            _server.OnData = MessageManager.OnServerData;
        }
        
        _server.StartListening(Address);

        // host doesnt have client, so theyre a special connection here
        Connections.Add(0, new(0, StandaloneProfileManager.SharedInstance.currentProfile.profileName));
        ConnectionIDs.Add(0);
        LocalID = 0;
        // we will NOT send the join event here. might change that later

        WakeUpManager.CanJoin = true; // let us join on title screen

        Application.runInBackground = true;
    }

    public static void Connect()
    {
        if (_server != null || _client != null) return; // TODO: tell player
        
        {
            _client = new(new() { Log = s => Logger.Log($"[client] {s}") });
            _client.OnConnected = () =>
            {
                Logger.Log("client connected");
                Application.runInBackground = true;
            };
            _client.OnDisconnected = reason =>
            {
                Logger.Log($"client disconnected because {reason}");
                // just clear out everything, i dont care
                Connections.Clear();
                ConnectionIDs.Clear();
                foreach (var entry in QObjectManager.Entries.Values)
                {
                    entry.QObjects.Clear();
                    entry.NextId = 0;
                }

                TickableManager.Tickables.Clear();
                
                // client already closed
                _client = null;
            };
            _client.OnData = MessageManager.OnData;
        }
        
        _client.Connect(Address);
    }

    public static void Disconnect()
    {
        if (IsHost)
        {
            _serverClients.Clear();
            // just clear out everything, i dont care
            Connections.Clear();
            ConnectionIDs.Clear();
            foreach (var entry in QObjectManager.Entries.Values)
            {
                entry.QObjects.Clear();
                entry.NextId = 0;
            }

            TickableManager.Tickables.Clear();
        }

        _client?.Close();
        _client = null;
        _server?.Close();
        _server = null;
    }

    public static void Tick()
    {
        _client?.Receive();
        _client?.Flush();
        _server?.Receive();
        _server?.Flush();
    }

    public static readonly List<int> _serverClients = new();
    public static int LocalID = -1;
    public static readonly List<int> ConnectionIDs = new(); // for order
    public static readonly Dictionary<int, Connection> Connections = new();
    public static Connection LocalConnection => Connections[LocalID];
}