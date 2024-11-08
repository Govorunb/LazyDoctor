namespace DesktopApp.Utilities.Helpers;

internal static class StringExtensions
{
    public static string Repeat(this string template, int times)
    {
        return string.Create(template.Length * times, template, (span, toCopy) =>
        {
            for (var i = 0; i < span.Length; i += toCopy.Length)
            {
                for (var j = 0; j < toCopy.Length; j++)
                {
                    span[i + j] = toCopy[j];
                }
            }
        });
    }
}
