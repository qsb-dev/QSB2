using HarmonyLib;
using MessagePack;
using QSB2.Messaging;
using UnityEngine;

namespace QSB2.Utility;

public class DebugGui : MonoBehaviour
{
    private void OnGUI()
    {
        foreach (var connection in NetworkManager.Connections.Values)
        {
            GUILayout.Label($"PLAYER {connection.ID}");
            GUILayout.Label($"\ttime {connection.Time} diff {connection.Time - NetworkManager.LocalConnection.Time}");
            GUILayout.Label($"\tscene {connection.Scene} counter {connection.LoadCounter}");
            GUILayout.Label($"\tcreated {connection.QObjectsCreated.Join()}");
        }
    }
}

// TODO: ping to each client, measure pong response time
[MessagePackObject]
public class PingMessage : Message
{
    public override void OnReceive(int from, int to)
    {
    }
}