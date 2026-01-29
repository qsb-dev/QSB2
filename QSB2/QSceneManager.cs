using OWML.Common;
using QSB2.Utility;

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
            OnPreSceneLoad?.QSafeInvoke(originalScene, loadScene);
        };
        LoadManager.OnCompleteSceneLoad += (originalScene, loadScene) =>
        {
            Logger.Log($"POST SCENE LOAD ({originalScene} -> {loadScene})", MessageType.Info);
            OnPostSceneLoad?.QSafeInvoke(originalScene, loadScene);
        };
    }

    public static bool IsInGameScene(this OWScene scene) => scene is OWScene.SolarSystem or OWScene.EyeOfTheUniverse;
}