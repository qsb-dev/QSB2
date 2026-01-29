using System.Collections.Generic;
using UnityEngine;

namespace QSB2.Authority;

public class HasOwner<T>(T qObject) where T : QObject.QObject<T>
{
    public bool DoWeOwn => Owner == NetworkManager.LocalID;

    public int Owner;
    public List<int> OwnerQueue = new();
}