// https://partner.steamgames.com/doc/api/ISteamNetworkingSockets
// https://partner.steamgames.com/doc/api/steamnetworkingtypes

using System;

namespace SteamTransport;

public class Settings
{
    /// <summary>
    /// logs will verbosely go here. must be set
    ///
    /// my policy is to log every potential error here, but otherwise ignore it. then if its an actual issue i do OnClientError and handle it properly.
    /// </summary>
    public Action<string> Log = _ => { };

    /// <summary>
    /// if set, will use an ip address and port for listening/connecting
    /// </summary>
    public bool UseIpAddress = false;

    /// <summary>
    /// timeout in ms when connecting, and timeout before detecting a loss in connection.
    /// after-connection timeout seems to be around 0-10 more seconds than specified.
    /// </summary>
    // default from steam https://github.com/ValveSoftware/GameNetworkingSockets/blob/master/src/steamnetworkingsockets/clientlib/csteamnetworkingsockets.cpp#L76
    public int Timeout = 10000;

    /// <summary>
    /// whether or not to simulate fake packet loss, lag, reorder, and dup
    /// </summary>
    public bool DoFakeNetworkErrors;
}