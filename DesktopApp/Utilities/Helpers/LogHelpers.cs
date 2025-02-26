namespace DesktopApp.Utilities.Helpers;

internal static class LogHelpers
{
    public static T AndLog<T>(this T value, IEnableLogger x, LogLevel level, string message)
    {
        x.Log().Write(message, level);
        return value;
    }

    public static T AndLog<T>(this T value, IEnableLogger x, Exception exception, LogLevel level, string message)
    {
        x.Log().Write(exception, message, level);
        return value;
    }
}
