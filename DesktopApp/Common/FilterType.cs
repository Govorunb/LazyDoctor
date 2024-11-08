using System.ComponentModel;

namespace DesktopApp.Common;

public enum FilterType
{
    [Description("No filtering. Result rows show matching operators.")]
    Show,
    [Description("Result rows will omit matching operators.")]
    Hide,
    [Description("Show only result rows with matching operators.")]
    Require,
    [Description("Hide result rows that contain any matching operators.")]
    Exclude,
}
