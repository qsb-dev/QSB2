using System.Linq;
using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using QSB2.Player;
using QSB2.SectorSync;
using QSB2.ShipSync;
using QSB2.Utility;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

namespace QSB2;

public class QSB2 : ModBehaviour
{
    public static QSB2 Instance;
    public static Harmony Harmony;

    #region versioning

    public static string QSBVersion => Instance.ModHelper.Manifest.Version;

    public static string GameVersion =>
        // ignore the last patch numbers like the title screen does
        Application.version.Split('.').Take(3).Join(delimiter: ".");

    public static bool DLCInstalled => EntitlementsManager.IsDlcOwned() == EntitlementsManager.AsyncOwnershipStatus.Owned;

    #endregion

    public void Awake()
    {
        Instance = this;
        Harmony = new Harmony("JohnCorby.QSB2");

        Gizmos.CameraFilter = _ => true;


        QSceneManager.OnPreSceneLoad += (originalScene, loadScene) =>
        {
            if (!NetworkManager.Connected) return;
            if (!originalScene.IsInGameScene()) return;

            PlayerManager.Destroy();
            QShipManager.Destroy();
            QSectorManager.Destroy();
        };
        QSceneManager.OnPostSceneLoad += (originalScene, loadScene) =>
        {
            if (!NetworkManager.Connected) return;
            if (!loadScene.IsInGameScene()) return;

            Delay.RunWhen(() => LateInitializerManager.isDoneInitializing, () =>
            {
                PlayerManager.Create();
                QShipManager.Create();
                QSectorManager.Create();
            });
        };
    }

    public void Start()
    {
        Logger.Log("qsb loaded", MessageType.Success);
    }

    public override void SetupTitleMenu(ITitleMenuManager titleManager)
    {
        titleManager.CreateTitleButton("Host").OnSubmitAction += NetworkManager.Host;
        titleManager.CreateTitleButton("Connect").OnSubmitAction += NetworkManager.Connect;
    }

    private void Update()
    {
        NetworkManager.Tick();
    }
}