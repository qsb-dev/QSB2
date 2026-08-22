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
    private bool _showMainView = true;
    private bool _showQObjects;
    public static bool ShowGizmos;
    public static bool ShowLabels;

    private int _otherPlayerToTeleportTo;

    private void OnGUI()
    {
        if (!NetworkManager.IsFullyConnected) return;

        if (_showMainView)
        {
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

        if (_labels.Count > 0 && Event.current.type == EventType.Repaint)
        {
            var style = new GUIStyle(GUI.skin.label);
            foreach (var (pos, label, size) in _labels)
            {
                style.fontSize = size;
                var rectSize = style.CalcSize(new GUIContent(label));
                GUI.Label(new Rect(pos - rectSize / 2, rectSize), label, style);
            }

            _labels.Clear();
        }
    }

    private void Update()
    {
        if (!NetworkManager.IsFullyConnected) return;

        if (Keyboard.current.qKey.isPressed)
        {
            if (Keyboard.current.gKey.wasPressedThisFrame) _showMainView = !_showMainView;
            if (Keyboard.current.bKey.wasPressedThisFrame) _showQObjects = !_showQObjects;
            if (Keyboard.current.hKey.wasPressedThisFrame) ShowGizmos = !ShowGizmos;
            if (Keyboard.current.nKey.wasPressedThisFrame) ShowLabels = !ShowLabels;

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

        if (_showMainView)
        {
            if (Time.timeSinceLevelLoad < _lastPingSend || Time.timeSinceLevelLoad > _lastPingSend + 1)
            {
                _lastPingSend = Time.timeSinceLevelLoad;
                new PingMessage().Send(SendTo.All);
            }
        }
    }

    private static readonly List<(Vector2 pos, string label, int size)> _labels = new();
    private const int MaxLabelSize = 15;
    private const float MaxLabelDistance = 150;

    public static void DrawLabel(Transform obj, string label)
    {
        var camera = Locator.GetPlayerCamera();

        if (camera == null)
        {
            return;
        }

        if (obj == null)
        {
            return;
        }

        var difference = obj.transform.position - camera.transform.position;

        if (Vector3.Dot(difference.normalized, camera.transform.forward) < 0)
        {
            return;
        }

        var cheapDistance = difference.sqrMagnitude;

        if (cheapDistance > MaxLabelDistance * MaxLabelDistance)
        {
            return;
        }

        var screenPosition = camera.WorldToScreenPoint(obj.position);
        var distance = screenPosition.z;

        if (distance <= 0.05f)
        {
            return;
        }

        if (distance > MaxLabelDistance)
        {
            return;
        }

        if (screenPosition.x < 0 || screenPosition.x > Screen.width)
        {
            return;
        }

        if (screenPosition.y < 0 || screenPosition.y > Screen.height)
        {
            return;
        }

        var mappedFontSize = (int)distance.Map(0, MaxLabelDistance, MaxLabelSize, 0, true);

        if (mappedFontSize <= 0)
        {
            return;
        }

        if (mappedFontSize > MaxLabelSize)
        {
            return;
        }

        // WorldToScreenPoint's (0,0) is at screen bottom left, GUI's (0,0) is at screen top left. grrrr
        screenPosition.y = Screen.height - screenPosition.y;
        _labels.Add((screenPosition, label, mappedFontSize));
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