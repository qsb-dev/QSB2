using HarmonyLib;
using MessagePack;
using QSB2.Messaging;
using QSB2.ShipSync;
using QSB2.WakeUpSync;
using UnityEngine;

namespace QSB2.Utility;

public class DebugGui : MonoBehaviour
{
    public static float _lastPingSend;

    private void OnGUI()
    {
        if (!NetworkManager.IsConnected) return;

        GUILayout.Label($"host waiting for players = {WakeUpManager.HostWaitingForPlayers}");
        foreach (var id in NetworkManager.ConnectionIDs) // we want order in this list
        {
            var connection = NetworkManager.Connections[id];
            GUILayout.Label($"PLAYER {connection.ID} {connection.Name}");
            GUILayout.Label($"\trtt {connection.RTT * 1000:F1}ms");
            GUILayout.Label($"\ttime {connection.Time} diff {connection.Time - NetworkManager.LocalConnection.Time}");
            GUILayout.Label($"\tscene {connection.Scene} counter {connection.LoadCounter}");
            GUILayout.Label($"\tcreated {connection.QObjectsCreated.Join()}");
            GUILayout.Label($"\tsectors: player {connection.Player?.PositionSync?.Reference} | probe {connection.Probe?.PositionSync?.Reference} | ship {QShip.Instance?.PositionSync?.Reference}");
        }

        if (Time.timeSinceLevelLoad < _lastPingSend || Time.timeSinceLevelLoad > _lastPingSend + 1)
        {
            _lastPingSend = Time.timeSinceLevelLoad;
            new PingMessage().Send(-1);
        }
    }
}

// ping to each client, measure pong response time
// (client A -> server -> client B -> server -> client A)
[MessagePackObject]
public class PingMessage : Message
{
    public override void OnReceive(int from, int to)
    {
        new PongMessage().Send(from);
    }
}

[MessagePackObject]
public class PongMessage : Message
{
    public override void OnReceive(int from, int to)
    {
        NetworkManager.Connections[from].RTT = Time.timeSinceLevelLoad - DebugGui._lastPingSend;
    }
}