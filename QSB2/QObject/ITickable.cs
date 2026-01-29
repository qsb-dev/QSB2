using System.Collections.Generic;

namespace QSB2.QObject;

public interface ITickable
{
    public void Tick();
}

public static class TickableManager
{
    public static readonly List<ITickable> Tickables = new();
    
    public static void Tick()
    {
        foreach (var tickable in Tickables)
        {
            tickable.Tick();
        }
    }
}