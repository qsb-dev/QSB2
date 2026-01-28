using UnityEngine;

namespace QSB2;

public class Connection
{
    public Player.Player Player; // null when player isnt set up and in da world
    public int ID;

    public Connection(int id)
    {
        ID = id;

        // TODO: dumb. move
        LoadManager.OnStartSceneLoad += (scene, loadScene) =>
        {
            if (Player)
                GameObject.Destroy(Player.gameObject);
            Player = null;
        };

        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            // TODO: i think good lifecycle is to wait for late init done before touching anything

            Player = new GameObject().AddComponent<Player.Player>();
            Player.Connection = this;
        };
    }
}