using System;
using System.Collections.Generic;
using OWML.Common;
using QSB2.WakeUpSync;

namespace QSB2.QObject;

// TODO: this means theres a difference between "qobject exists" and "qobject is initialized (started)". its probably worth having another global flag to wait for
//       alternatively, we just do qsb1 style make create async and then again have a qobject exist vs qobject init thing...
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