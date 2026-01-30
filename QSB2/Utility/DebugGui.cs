using HarmonyLib;
using MessagePack;
using QSB2.Messaging;
using UnityEngine;

namespace QSB2.Utility;

public class DebugGui : MonoBehaviour
{
    private float _lastSend;

    private void OnGUI()
    {
        foreach (var connection in NetworkManager.Connections.Values)
        {
            GUILayout.Label($"PLAYER {connection.ID}");
            GUILayout.Label($"\ttime diff {connection.Time - NetworkManager.LocalConnection.Time}");
            GUILayout.Label($"\tscene {connection.Scene} counter {connection.LoadCounter}");
            GUILayout.Label($"\tcreated {connection.QObjectsCreated.Join()}");
        }
    }

    private void Update()
    {
        if (Time.time > _lastSend + 1)
        {
            _lastSend = Time.time;

            new TimeMessage
            {
                Time = TimeLoop.GetSecondsElapsed()
            }.Send(-1);
        }
    }
}

[MessagePackObject]
public class TimeMessage : Message
{
    [Key(0)] public required float Time;

    public override void OnReceive(int from, int to)
    {
        NetworkManager.Connections[from].Time = Time;
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