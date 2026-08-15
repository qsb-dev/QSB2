using QSB2.Messaging;

namespace QSB2.Patches;

public abstract class QPatch(QPatchWhen when)
{
    public readonly QPatchWhen When = when;

    /// <summary>
    /// are we currently receiving a message?
    /// </summary>
    protected static bool Receiving => MessageManager.Receiving;
}

public enum QPatchWhen
{
    Immediately,
    OnConnected,
    OnQObjectsCreated
}