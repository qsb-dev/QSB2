using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using QSB2.Patches;
using QSB2.QObject;
using QSB2.Utility;
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

    public override void Configure(IModConfig config)
    {
        NetworkManager.Address = config.GetSettingsValue<string>("Address");
        NetworkManager.UseIpAddress = config.GetSettingsValue<bool>("Use Ip Address");
        NetworkManager.DoFakeNetworkErrors = config.GetSettingsValue<int>("Do Fake Network Errors");
    }

    public void Start()
    {
        Instance = this;
        // new Harmony("JohnCorby.QSB2").PatchAll(Assembly.GetExecutingAssembly());

        // i want all static constructors running at the beginning instead of whenever we first reference
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);

        QPatchManager.Patch(QPatchWhen.Immediately);

        // Gizmos.CameraFilter = _ => true;
        gameObject.AddComponent<DebugGui>();

        Logger.Log("qsb loaded", MessageType.Success);

        if (Environment.GetCommandLineArgs().ElementAtOrDefault(1) == "autostart")
            StartCoroutine(AutoStart.Go());
    }

    public override void SetupTitleMenu(ITitleMenuManager titleManager)
    {
        titleManager.CreateTitleButton("Host", 0, true).OnSubmitAction += NetworkManager.Host;
        titleManager.CreateTitleButton("Connect", 1, true).OnSubmitAction += NetworkManager.Connect;
    }

    private void Update()
    {
        TickableManager.Tick();
        NetworkManager.Tick();
        WakeUpManager.Tick();

        // hack. move this to menu manager or something later. also rewrite when ui is good
        if (LoadManager.GetCurrentScene() == OWScene.TitleScreen)
        {
            var titleScreenManager = FindObjectOfType<TitleScreenManager>();
            titleScreenManager._newGameObject.SetActive(NetworkManager.IsConnected);
            titleScreenManager._resumeGameObject.SetActive(NetworkManager.IsConnected);
        }
    }
}