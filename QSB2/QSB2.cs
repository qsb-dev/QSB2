using System.Linq;
using System.Reflection;
using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using QSB2.QObject;
using QSB2.WakeUpSync;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

namespace QSB2;

public class QSB2 : ModBehaviour
{
    public static QSB2 Instance;

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
        new Harmony("JohnCorby.QSB2").PatchAll(Assembly.GetExecutingAssembly());

        Gizmos.CameraFilter = _ => true;

        QObjectManager.Init();
        WakeUpManager.Init();
    }

    public override void Configure(IModConfig config)
    {
        NetworkManager.IP = config.GetSettingsValue<string>("IP");
        NetworkManager.Port = config.GetSettingsValue<int>("Port");
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
        TickableManager.Tick();
        NetworkManager.Tick();
        WakeUpManager.Tick();
    }
}