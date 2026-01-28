namespace QSB2;

public static class Logger
{
    public static void Log(string msg) => QSB2.Instance.ModHelper.Console.WriteLine(msg);
}