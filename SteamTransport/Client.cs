using Steamworks;
using System;

namespace SteamTransport;

public class Client
{
    public Action OnConnected;
    public Action<ArraySegment<byte>> OnData;
    public Action<string> OnDisconnected;


    private Settings _settings;
    private Steamworks.Callback<SteamNetConnectionStatusChangedCallback_t> _onStatusChanged;

    public Client(Settings settings)
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
                    IsConnecting = true;
                    IsConnected = false;
                    break;
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    IsConnecting = false;
                    IsConnected = true;
                    OnConnected?.Invoke();
                    break;
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    var result = SteamNetworkingSockets.CloseConnection(_conn, t.m_info.m_eEndReason, t.m_info.m_szEndDebug, false);
                    if (result != true)
                    {
                        _settings.Log($"[warn] close returned {result}");
                    }

                    IsConnecting = false;
                    IsConnected = false;
                    OnDisconnected?.Invoke(t.m_info.m_szEndDebug);
                    break;
            }
        });
    }

    public bool IsConnecting;
    public bool IsConnected;

    private HSteamNetConnection _conn;


    public void Connect(string address)
    {
        var options = Util.MakeOptions(_settings);

        if (_settings.UseIpAddress)
        {
            var steamAddr = new SteamNetworkingIPAddr();
            var parsed = steamAddr.ParseString(address);
            if (!parsed)
            {
                OnDisconnected?.Invoke($"couldnt parse address {address} when connecting"); // will show error box
                return;
            }

            _conn = SteamNetworkingSockets.ConnectByIPAddress(ref steamAddr, options.Length, options);
            _settings.Log($"connecting to {steamAddr.ToDebugString()}");
        }
        else
        {
            var identity = new SteamNetworkingIdentity();
            var parsed = ulong.TryParse(address, out var steamId);
            if (!parsed)
            {
                OnDisconnected?.Invoke($"couldnt parse address {address} when connecting"); // will show error box
                return;
            }

            identity.SetSteamID64(steamId);

            _conn = SteamNetworkingSockets.ConnectP2P(ref identity, 0, options.Length, options);
            _settings.Log($"connecting to {identity.ToDebugString()}");
        }
        
        if (_conn == HSteamNetConnection.Invalid)
            _settings.Log($"[warn] connect returned invalid"); // TODO: this is a connect fail
    }

    public void Send(ArraySegment<byte> segment, int channelId)
    {
        var result = _conn.Send(segment, channelId);
        if (result != EResult.k_EResultOK)
        {
            _settings.Log($"[warn] send returned {result}");
        }
    }

    public void Receive()
    {
        var ppOutMessages = new IntPtr[Util.MaxMessages];
        var numMessages = SteamNetworkingSockets.ReceiveMessagesOnConnection(_conn, ppOutMessages, ppOutMessages.Length);
        for (var i = 0; i < numMessages; i++)
        {
            var (segment, channelId) = Util.Receive(ppOutMessages[i]);
            OnData?.Invoke(segment);
        }
    }

    public void Flush()
    {
        var result = SteamNetworkingSockets.FlushMessagesOnConnection(_conn);
        if (result != EResult.k_EResultOK && result != EResult.k_EResultIgnored) // flush does ignored when connecting. dont log cuz spam
        {
            _settings.Log($"[warn] flush returned {result}");
        }
    }

    public void Close()
    {
        // theres a weird case where we arent doing an intentional disconnect but there isnt a status change disonnect either
        // not sure whats going on there, but ill slap a stack trace on it

        _settings.Log($"client close\n{Environment.StackTrace}");
        var result = SteamNetworkingSockets.CloseConnection(_conn, 0, "client closed connection", false);
        if (result != true)
        {
            _settings.Log($"[warn] client close returned {result}");
        }

        IsConnecting = false;
        IsConnected = false;
        // its not an error for us to close ourselves intentionally
        // but we do it anyway cuz above comment
        OnDisconnected?.Invoke("client closed connection for unknown reason! turn on \"[DEBUG] Debug Mode\" and \"[DEBUG] Hook Debug Logs\", try again, and report all logs shown!");

        _onStatusChanged.Dispose();
    }
}