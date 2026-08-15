using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SteamTransport;

public class Server
{
    public Action<int> OnConnected;
    public Action<int, ArraySegment<byte>, int> OnData;
    public Action<int, string> OnDisconnected;


    private Settings _settings;
    private Steamworks.Callback<SteamNetConnectionStatusChangedCallback_t> _onStatusChanged;

    public Server(Settings settings)
    {
        _settings = settings;

        _onStatusChanged = Steamworks.Callback<SteamNetConnectionStatusChangedCallback_t>.Create(t =>
        {
            _settings.Log($"STATUS CHANGED for {t.m_info.m_szConnectionDescription}\n" +
                           $"  state = {t.m_info.m_eState}\n" +
                           $"  end = {(ESteamNetConnectionEnd)t.m_info.m_eEndReason} {t.m_info.m_szEndDebug}");
            // SteamNetworkingSockets.GetDetailedConnectionStatus(t.m_hConn, out var status, 1000);
            // _transport.Log(status);

            switch (t.m_info.m_eState)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                {
                    // max connections? meh
                    var result = SteamNetworkingSockets.AcceptConnection(t.m_hConn);
                    if (result != EResult.k_EResultOK)
                    {
                        _settings.Log($"[warn] accept {t.m_info.m_szConnectionDescription} returned {result}");
                    }

                    break;
                }
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    _conns.Add(t.m_hConn);
                    OnConnected?.Invoke((int)t.m_hConn.m_HSteamNetConnection);
                    break;
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                {
                    var result = SteamNetworkingSockets.CloseConnection(t.m_hConn, t.m_info.m_eEndReason, t.m_info.m_szEndDebug, false);
                    if (result != true)
                    {
                        _settings.Log($"[warn] close {t.m_info.m_szConnectionDescription} returned {result}");
                    }

                    _conns.Remove(t.m_hConn);
                    OnDisconnected?.Invoke((int)t.m_hConn.m_HSteamNetConnection, t.m_info.m_szEndDebug);
                    break;
                }
            }
        });
    }

    public bool IsListening;

    private HSteamListenSocket _listenSocket;

    // connection id is derived from uint to int cast here. seems to do unchecked cast and be fine
    private readonly List<HSteamNetConnection> _conns = new();

    public void StartListening(string address)
    {
        var options = Util.MakeOptions(_settings);

        if (_settings.UseIpAddress)
        {
            var steamAddr = new SteamNetworkingIPAddr();
            var parsed = steamAddr.ParseString(address);
            if (!parsed)
            {
                throw new Exception($"couldnt parse address {address} when listening");
            }

            _listenSocket = SteamNetworkingSockets.CreateListenSocketIP(ref steamAddr, options.Length, options);
            _settings.Log($"listening on {steamAddr.ToDebugString()}");
        }
        else
        {
            _listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, options.Length, options);
            _settings.Log($"listening on p2p");
        }
        
        if (_listenSocket == HSteamListenSocket.Invalid)
            throw new Exception("listen returned invalid");

        IsListening = true;
    }

    public void Send(int connectionId, ArraySegment<byte> segment, int channelId)
    {
        var conn = new HSteamNetConnection((uint)connectionId);

        var result = conn.Send(segment, channelId);
        if (result != EResult.k_EResultOK)
        {
            _settings.Log($"[warn] send {conn.ToDebugString()} returned {result}");
        }
    }

    public void Receive()
    {
        var ppOutMessages = new IntPtr[Util.MaxMessages];

        // TODO: if receive can result in disconnect, we must copy
        foreach (var conn in _conns)
        {
            var numMessages = SteamNetworkingSockets.ReceiveMessagesOnConnection(conn, ppOutMessages, ppOutMessages.Length);
            for (var i = 0; i < numMessages; i++)
            {
                var (segment, channelId) = Util.Receive(ppOutMessages[i]);
                OnData?.Invoke((int)conn.m_HSteamNetConnection, segment, channelId);
            }
        }
    }

    public void Flush()
    {
        foreach (var conn in _conns)
        {
            var result = SteamNetworkingSockets.FlushMessagesOnConnection(conn);
            if (result != EResult.k_EResultOK)
            {
                _settings.Log($"[warn] flush {conn.ToDebugString()} returned {result}");
            }
        }
    }

    public void Disconnect(int connectionId, string reason)
    {
        var conn = new HSteamNetConnection((uint)connectionId);
        _settings.Log($"disconnect {conn.ToDebugString()}");
        var result = SteamNetworkingSockets.CloseConnection(conn, 0, reason, false);
        if (result != true)
        {
            _settings.Log($"[warn] close {conn.ToDebugString()} returned {result}");
        }

        _conns.Remove(conn);
        OnDisconnected?.Invoke(connectionId, reason);
    }

    public void Close()
    {
        _settings.Log("stop server");
        // this calls ondisconnected, but it doesnt really need to. meh, it doesnt matter
        foreach (var conn in _conns.ToList()) Disconnect((int)conn.m_HSteamNetConnection, "server closed");
        var result = SteamNetworkingSockets.CloseListenSocket(_listenSocket);
        if (result != true)
        {
            _settings.Log($"[warn] stop server returned {result}");
        }

        IsListening = false;

        _onStatusChanged.Dispose();
    }
}