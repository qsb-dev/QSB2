using QSB2.Messaging;
using QSB2.SectorSync;
using UnityEngine;

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
                // TODO: currently we just do not tell other players at all whether we exist yet.
                //       im hoping with the lifecycle plans thatll be okay because all players MUST exist in game and be loaded before things happen
                if (connection.Player) GameObject.Destroy(connection.Player.gameObject);
                connection.Player = null;
            }
            
            QSectorManager.Uninit();
        };

        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            // TODO: i think good lifecycle is to wait for late init done before touching anything

            if (loadScene != OWScene.SolarSystem) return;
            
            foreach (var connection in NetworkManager.Connections.Values)
            {
                connection.Player = new GameObject().AddComponent<Player.Player>();
                connection.Player.Connection = connection;
            }
            
            QSectorManager.Init();
        };

    }
}