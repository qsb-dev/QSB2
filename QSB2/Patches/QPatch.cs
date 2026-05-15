namespace QSB2.Patches;

public abstract class QPatch(QPatchWhen when)
{
    public readonly QPatchWhen When = when;

    /// <summary>
    /// are we in a remotely called context?
    /// </summary>
    public static bool Remote;
}

public enum QPatchWhen
{
    Immediately,
    OnConnected,
    OnQObjectsCreated
}