using System;
using System.Collections.Generic;

namespace QSB2;

public class Connection(int id)
{
    public int ID = id;
    public Player.Player Player; // null when player isnt set up and in da world

    public readonly HashSet<Type> QObjectsCreated = new();
    public OWScene Scene;
    public int LoadCounter;
}