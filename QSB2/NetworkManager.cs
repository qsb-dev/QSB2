using System.Collections.Generic;
using QSB2.Messaging;
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
            var hostJoining = _serverClients.Count == 0;

            _serverClients.Add(id);
            new IdentifyMessage
            {
                QSBVersion = QSB2.QSBVersion,
                GameVersion = QSB2.GameVersion,
                DLCInstalled = QSB2.DLCInstalled,
                CanJoin = WakeUpManager.CanJoin || hostJoining,
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

        _client.OnConnected = () =>
        {
            Logger.Log("client connected");
            Application.runInBackground = true;
        };
        _client.OnDisconnected = () =>
        {
            Logger.Log("client disconnected");
            // we disconnect = wont receive leave messages that clears this, so we gotta do it here
            Connections.Clear();
            // LocalID = -1;
        };
        _client.OnData = MessageManager.OnData;
    }

    public static string IP;
    public static int Port;

    public static void Host()
    {
        _server.Start(Port);
        Connect();
    }

    public static void Connect()
    {
        _client.Connect(IP, Port);
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
}