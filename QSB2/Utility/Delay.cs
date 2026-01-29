using System;

namespace QSB2.Utility;

public static class Delay
{
    public static Action<Action> FireOnNextUpdate => QSB2.Instance.ModHelper.Events.Unity.FireOnNextUpdate;
    public static Action<Action, int> FireInNUpdates => QSB2.Instance.ModHelper.Events.Unity.FireInNUpdates;
    public static Action<Func<bool>, Action> RunWhen => QSB2.Instance.ModHelper.Events.Unity.RunWhen;
}