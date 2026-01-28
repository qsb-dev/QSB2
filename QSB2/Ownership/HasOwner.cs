using System.Collections.Generic;
using UnityEngine;

namespace QSB2.Authority;

public class HasOwner : MonoBehaviour
{
    public int Owner => OwnerQueue[0];
    public List<int> OwnerQueue = new();
}
