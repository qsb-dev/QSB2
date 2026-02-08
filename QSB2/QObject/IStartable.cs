using System;
using System.Collections.Generic;
using OWML.Common;
using QSB2.WakeUpSync;

namespace QSB2.QObject;

public interface IStartable
{
    public void Start();
}

public static class StartableManager
{
    public static readonly List<IStartable> Startables = new();
    
    public static void Start()
    {
        foreach (var startable in Startables)
        {
            try
            {
                startable.Start();
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString(), MessageType.Error);
            }
        }
    }
}