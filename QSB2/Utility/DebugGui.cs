using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MessagePack;
using QSB2.Messaging;
using QSB2.QObject;
using QSB2.SectorSync;
using QSB2.ShipSync;
using QSB2.WakeUpSync;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QSB2.Utility;

public class DebugGui : MonoBehaviour
{
    public static float _lastPingSend;

    private static List<int> _testList = new();

    private Vector2 _scrollPos;
    private bool _guiEnabled = true;
    private bool _showQObjects;
    public static bool ShowGizmos;

    private int _otherPlayerToTeleportTo;

    private void OnGUI()
    {
        if (!_guiEnabled) return;

        if (!NetworkManager.IsFullyConnected) return;

        _scrollPos = GUILayout.BeginScrollView(_scrollPos);

        GUILayout.Label($"host waiting for players = {WakeUpManager.HostWaitingForPlayers}");
        foreach (var id in NetworkManager.ConnectionIDs) // we want order in this list
        {
            var connection = NetworkManager.Connections[id];
            GUILayout.Label($"<color=cyan>PLAYER {connection.ID} <b>{connection.Name}</b></color>");
            GUILayout.Label($"rtt {connection.RTT * 1000:F1}ms");
            GUILayout.Label($"time {connection.Time} diff {connection.Time - NetworkManager.LocalConnection.Time}");
            GUILayout.Label($"scene {connection.Scene} counter {connection.LoadCounter}");
            if (_showQObjects)
                GUILayout.Label(connection.QObjectsCreated.Join(delimiter: "\n"));
            GUILayout.Label($"sectors: <b>player</b> {connection.QPlayer?.PositionSync?.Reference} | <b>probe</b> {connection.QProbe?.PositionSync?.Reference} | <b>ship</b> {QShip.Instance?.PositionSync?.Reference}");
        }

        GUILayout.Label(_testList.Join());

        GUILayout.EndScrollView();
    }

    private void Update()
    {
        if (!NetworkManager.IsFullyConnected) return;
        
        if (Keyboard.current.qKey.isPressed)
        {
            if (Keyboard.current.gKey.wasPressedThisFrame) _guiEnabled = !_guiEnabled;
            if (Keyboard.current.hKey.wasPressedThisFrame) _showQObjects = !_showQObjects;
            if (Keyboard.current.jKey.wasPressedThisFrame) ShowGizmos = !ShowGizmos;

            if (Keyboard.current.lKey.wasPressedThisFrame)
                StartCoroutine(TestList());

            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                var otherPlayers = NetworkManager.ConnectionIDs.Where(x => x != NetworkManager.LocalID).ToList();
                _otherPlayerToTeleportTo = (_otherPlayerToTeleportTo + 1) % otherPlayers.Count;
                var otherPlayer = otherPlayers[_otherPlayerToTeleportTo];
                new DebugTeleportRequestMessage().Send(otherPlayer);
            }
        }

        if (_guiEnabled)
        {
            if (Time.timeSinceLevelLoad < _lastPingSend || Time.timeSinceLevelLoad > _lastPingSend + 1)
            {
                _lastPingSend = Time.timeSinceLevelLoad;
                new PingMessage().Send(SendTo.All);
            }
        }
    }

    /// <summary>
    /// test data races between multiple clients
    /// </summary>
    private static IEnumerator TestList()
    {
        new ResetListMessage().Send(SendTo.All);

        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForSeconds(.3f);
            new AddListMessage
            {
                Value = NetworkManager.IsHost ? (i * 2) : (i * 2 + 1)
            }.Send(SendTo.All);
        }
    }

    [MessagePackObject]
    public class ResetListMessage : Message
    {
        public override void OnReceive(int from, int to)
        {
            _testList.Clear();
        }
    }

    [MessagePackObject]
    public class AddListMessage : Message
    {
        [Key(0)] public required int Value;

        public override void OnReceive(int from, int to)
        {
            _testList.Add(Value);
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

[MessagePackObject]
public class DebugTeleportRequestMessage : Message
{
    public override void OnReceive(int from, int to)
    {
        var qSector = NetworkManager.LocalConnection.QPlayer.RelativeToSector.QSector;
        var body = Locator.GetPlayerBody();
        var refBody = qSector.Component.GetOWRigidbody();

        var pos = body.GetPosition();
        new DebugTeleportResponseMessage
        {
            SectorId = qSector.ID,
            RelPos = refBody.transform.ToRelPos(pos),
            RelRot = refBody.transform.ToRelRot(body.GetRotation()),
            DegreesY = Locator.GetPlayerCameraController().GetDegreesY(),
            RelVel = refBody.ToRelVel(body.GetVelocity(), pos),
            RelAngVel = refBody.ToRelAngVel(body.GetAngularVelocity())
        }.Send(from);
    }
}

[MessagePackObject]
public class DebugTeleportResponseMessage : Message
{
    [Key(0)] public required int SectorId;
    [Key(1)] public required Vector3 RelPos;
    [Key(2)] public required Quaternion RelRot;
    [Key(3)] public required float DegreesY;
    [Key(4)] public required Vector3 RelVel;
    [Key(5)] public required Vector3 RelAngVel;

    public override void OnReceive(int from, int to)
    {
        var qSector = SectorId.GetQObject<QSector>();
        var body = Locator.GetPlayerBody();
        var refBody = qSector.Component.GetOWRigidbody();

        var pos = refBody.transform.FromRelPos(RelPos);
        body.SetPosition(pos);
        body.SetRotation(refBody.transform.FromRelRot(RelRot));
        Locator.GetPlayerCameraController().SetDegreesY(DegreesY);
        body.SetVelocity(refBody.FromRelVel(RelVel, pos));
        body.SetAngularVelocity(refBody.FromRelAngVel(RelAngVel));
    }
}