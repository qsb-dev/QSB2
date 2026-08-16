using System;
using QSB2.QObject;
using QSB2.Utility;

namespace QSB2.GeyserSync;

public class QGeyser : QObject<GeyserController>
{
    public override void Create()
    {
        Component._initTime = QGeyserBuilder.InitTimeRandom.Range(0f, Component._inactiveDuration);
        base.Create();
    }
}

public class QGeyserBuilder : QObjectBuilder<QGeyser, GeyserController>
{
    public static Random InitTimeRandom;

    public override void Create()
    {
        InitTimeRandom = new Random(NetworkManager.LocalConnection.LoadCounter); // should be the same between all clients. kinda dumb but should work

        base.Create();
    }
}

// no messages needed. uses time to start and stop geysers