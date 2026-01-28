using UnityEngine;

namespace QSB2.PositionSync;

public class RelativeToSector : MonoBehaviour
{
    private void Awake()
    {
        // just hardcode to relative to sun for now
        GetComponent<PositionSync>().Reference = Locator.GetSunTransform();
    }
}