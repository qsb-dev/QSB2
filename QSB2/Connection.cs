using QSB2.Player;
using QSB2.SectorSync;
using QSB2.ShipSync;
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
            if (!NetworkManager.Connected) return;
            
            PlayerManager.Destroy();
            QShipManager.Destroy();
            QSectorManager.Destroy();
        };

        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene != OWScene.SolarSystem) return;
            if (!NetworkManager.Connected) return;
            
            Delay.RunWhen(() => LateInitializerManager.isDoneInitializing, () =>
            {
                PlayerManager.Create();
                QShipManager.Create();
                QSectorManager.Create();
            });
        };

    }
}