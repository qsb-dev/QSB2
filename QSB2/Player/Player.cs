using System;
using System.Collections.Generic;

namespace QSB2.Player;

// players are special in that they can be removed mid game

public class Player : QObject.QObject
{
    public static Player LocalInstance;

    public static readonly Dictionary<int, Player> Players = new();
    public static event Action OnPlayerAdded, OnPlayerRemoved;

    protected override void Start()
    {
        base.Start();

        Players.Add(ID, this);
        OnPlayerAdded?.Invoke();
    }

    protected override void OnDestroy()
    {
        OnPlayerRemoved?.Invoke();
        Players.Remove(ID);
    }
}