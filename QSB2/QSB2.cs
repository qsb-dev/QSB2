using System.Linq;
using System.Reflection;
using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;

namespace QSB2;

public class QSB2 : ModBehaviour
{
    public static QSB2 Instance;


    public static string QSBVersion => Instance.ModHelper.Manifest.Version;

    public static string GameVersion =>
        // ignore the last patch numbers like the title screen does
        Application.version.Split('.').Take(3).Join(delimiter: ".");

    public static bool DLCInstalled => EntitlementsManager.IsDlcOwned() == EntitlementsManager.AsyncOwnershipStatus.Owned;


    public void Awake()
    {
        Instance = this;
        // You won't be able to access OWML's mod helper in Awake.
        // So you probably don't want to do anything here.
        // Use Start() instead.
    }

    public void Start()
    {
        // Starting here, you'll have access to OWML's mod helper.
        ModHelper.Console.WriteLine($"My mod {nameof(QSB2)} is loaded!", MessageType.Success);

        new Harmony("JohnCorby.QSB2").PatchAll(Assembly.GetExecutingAssembly());

        // Example of accessing game code.
        OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen); // We start on title screen
        LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
    }

    public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
    {
        if (newScene != OWScene.SolarSystem) return;
        ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);
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