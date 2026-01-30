using MessagePack;
using OWML.Common;
using OWML.Utils;
using QSB2.Messaging;

namespace QSB2;

public static class QSceneManager
{
    /// <summary>
    /// runs before the scene is changed.
    /// happens before OnDestroy.
    /// </summary>
    public static event LoadManager.SceneLoadEvent OnPreSceneLoad;

    /// <summary>
    /// runs after the scene is changed.
    /// happens after Awake, but before Start.
    /// </summary>
    public static event LoadManager.SceneLoadEvent OnPostSceneLoad;

    static QSceneManager()
    {
        LoadManager.OnStartSceneLoad += (originalScene, loadScene) =>
        {
            Logger.Log($"PRE SCENE LOAD ({originalScene} -> {loadScene})", MessageType.Info);
            OnPreSceneLoad?.SafeInvoke(originalScene, loadScene);
        };
        LoadManager.OnCompleteSceneLoad += (originalScene, loadScene) =>
        {
            Logger.Log($"POST SCENE LOAD ({originalScene} -> {loadScene})", MessageType.Info);
            OnPostSceneLoad?.SafeInvoke(originalScene, loadScene);
        };

        OnPostSceneLoad += (originalScene, loadScene) =>
        {
            new SceneMessage
            {
                Scene = loadScene,
                LoadCounter = NetworkManager.LocalConnection.LoadCounter + 1,
            }.Send(-1);
        };
    }

    public static bool IsGameScene(this OWScene scene) => scene is OWScene.SolarSystem or OWScene.EyeOfTheUniverse;
}

[MessagePackObject]
public class SceneMessage : Message
{
    [Key(0)] public required OWScene Scene;
    [Key(1)] public required int LoadCounter;

    public override void OnReceive(int from, int to)
    {
        var connection = NetworkManager.Connections[from];
        connection.Scene = Scene;
        connection.LoadCounter = LoadCounter;
        Logger.Log($"player from in scene {Scene} counter {LoadCounter}");
    }
}