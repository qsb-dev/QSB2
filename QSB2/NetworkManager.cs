using System.Collections.Generic;
using QSB2.Messaging;
using Telepathy;
using UnityEngine;

namespace QSB2;

public static class NetworkManager
{
    public static readonly Client _client = new(1024);
    public static readonly Server _server = new(1024);

    public static bool Connected = _client.Connected;
    public static bool IsHost = _server.Active;

    public static void Host()
    {
        IsHost = true;
        _server.Start(1337);
        Connect();

        _server.OnConnected = (id, _) =>
        {
            ServerClients.Add(id);
            new JoinMessage
            {
                QSBVersion = QSB2.QSBVersion,
                GameVersion = QSB2.GameVersion,
                DLCInstalled = QSB2.DLCInstalled,
                ID = id,
            }.Send(id);
        };
        _server.OnDisconnected = id => ServerClients.Remove(id);
        _server.OnData = MessageManager.OnServerData;
    }

    public static void Connect()
    {
        Application.runInBackground = true;

        _client.Connect("localhost", 1337);
        _client.OnConnected = () => Connected = true;
        _client.OnDisconnected = () => Connected = false;
        _client.OnData = MessageManager.OnData;
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

    public static readonly List<int> ServerClients = new();
    public static int LocalID = -1;
}