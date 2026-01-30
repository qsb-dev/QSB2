using MessagePack;
using QSB2.Messaging;

namespace QSB2.QObject.Verify;

/// <summary>
/// host sends over some data about world objects.
/// </summary>
[MessagePackObject]
public class QObjectsVerifyMessage : Message
{
    /*
     * qsb1 works as follows:
     * - host sends over hash of object count and type names to every guy
     * - if they dont match, guy asks for more info
     * - host sends dump of each object (paths, categorized by the type)
     * - guy compares paths and reports differences before disconnecting
     */
    
    public override void OnReceive(int from, int to)
    {
    }
}