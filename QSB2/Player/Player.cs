using QSB2.Authority;
using QSB2.PositionSync;
using UnityEngine;

namespace QSB2.Player;

// players are special in that they create/destroy their linked object, and they can be created and destroyed mid game

// TODO: still figuring out whether i wanna keep these across reloads to hold all player state, or whether to match the lifecycle of the actual player object

public class Player : QObject.QObject
{
    public Connection Connection;

    protected override void Start()
    {
        gameObject.AddComponent<PositionSync.PositionSync>();
        gameObject.AddComponent<RelativeToSector>();
        gameObject.AddComponent<HasOwner>().Owner = Connection.ID;

        base.Start();
        // TODO: do terrible things and match object id with player id. or just separate player state this object (see above todo)

        if (NetworkManager.LocalID == Connection.ID)
        {
            // we own. grab local guy
            UnityComponent = Locator.GetPlayerTransform();
            
            Logger.Log($"local player {Connection.ID} synced");
        }
        else
        {
            // create player object
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityComponent = go.GetComponent<Transform>();
            
            Logger.Log($"remote player {Connection.ID} synced");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (NetworkManager.LocalID == Connection.ID) return;
        // remove player object
        Destroy(UnityComponent.gameObject);
    }
}