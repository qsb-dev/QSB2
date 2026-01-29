using QSB2.SectorSync;
using QSB2.Utility;

namespace QSB2;

public class Connection(int id)
{
    public Player.Player Player; // null when player isnt set up and in da world
    public int ID = id;

    static Connection()
    {
        // TODO: dumb. move
        LoadManager.OnStartSceneLoad += (scene, loadScene) =>
        {
            if (scene != OWScene.SolarSystem) return;
            
            foreach (var connection in NetworkManager.Connections.Values)
            {
                connection.Player?.Destroy();
                connection.Player = null;
            }
            
            QSectorManager.Destroy();
        };

        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene != OWScene.SolarSystem) return;
            
            Delay.RunWhen(() => LateInitializerManager.isDoneInitializing, () =>
            {
                foreach (var connection in NetworkManager.Connections.Values)
                {
                    connection.Player = new();
                    connection.Player.Connection = connection;
                    connection.Player.Create();
                }
            
                QSectorManager.Create();
            });
        };

    }
}