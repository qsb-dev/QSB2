using System.Collections.Generic;
using UnityEngine;

namespace QSB2.Authority;

[RequireComponent(typeof(QObject.QObject))]
public class HasOwner : MonoBehaviour
{
    public int Owner;
    public List<int> OwnerQueue = new();
}
