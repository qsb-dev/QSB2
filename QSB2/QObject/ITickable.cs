using System;
using System.Collections.Generic;
using OWML.Common;

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
            try
            {
                tickable.Tick();
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString(), MessageType.Error);
            }
        }
    }
}