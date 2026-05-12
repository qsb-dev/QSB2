using System;
using System.Collections.Generic;
using QSB2.PlayerSync;
using QSB2.ProbeSync;

namespace QSB2;

public class Connection(int id, string name)
{
    public int ID = id;
    public string Name = name;
    public float RTT;
    public QPlayer QPlayer; // null when player isnt set up and in da world
    public QProbe QProbe;

    public readonly Dictionary<Type, int> QObjectsCreated = new();
    public OWScene Scene = OWScene.TitleScreen;
    public int LoadCounter;
    public float Time;
}