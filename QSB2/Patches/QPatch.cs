using QSB2.Messaging;

namespace QSB2.Patches;

public abstract class QPatch(QPatchWhen when)
{
    public readonly QPatchWhen When = when;

    /// <summary>
    /// are we in a remotely called context?
    /// </summary>
    protected static bool Remote => MessageManager.Remote;
}

public enum QPatchWhen
{
    Immediately,
    OnConnected,
    OnQObjectsCreated
}