using System;
using System.Collections.Generic;

namespace QSB2;

public class Connection(int id)
{
    public int ID = id;
    public float RTT;
    public Player.Player Player; // null when player isnt set up and in da world

    public readonly Dictionary<Type, int> QObjectsCreated = new();
    public OWScene Scene = OWScene.TitleScreen;
    public int LoadCounter;
    public float Time;
}