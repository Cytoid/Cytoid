using LunarConsolePlugin;

public static class ConsoleManager
{
    public static void enable()
    {
        if (LunarConsole.settings != null)
        {
            LunarConsole.settings.exceptionWarning.displayMode = ExceptionWarningDisplayMode.All;
            LunarConsole.UpdateSettings(LunarConsole.settings);
        }
    }

    public static void disable()
    {
        if (LunarConsole.settings != null)
        {
            LunarConsole.settings.exceptionWarning.displayMode = ExceptionWarningDisplayMode.None;
            LunarConsole.UpdateSettings(LunarConsole.settings);
        }
    }

    public static void show()
    {
        LunarConsole.Show();
    }
}
