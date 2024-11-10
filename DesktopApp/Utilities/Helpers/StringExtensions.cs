namespace DesktopApp.Utilities.Helpers;

internal static class StringExtensions
{
    public static string Repeat(this string template, int times)
    {
        return string.Create(template.Length * times, template, (copyTo, toCopy) =>
        {
            for (var i = 0; i < copyTo.Length; i += toCopy.Length)
            {
                toCopy.CopyTo(copyTo[i..]);
            }
        });
    }

    public static string Repeat(this char c, int times) => new string(c, times);
}
