using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DesktopApp.ViewModels;

namespace DesktopApp;

public sealed class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        if (LOCATOR.GetService(typeof(IViewFor<>).MakeGenericType(param.GetType())) is { } view)
            return (Control)view;

        var name = param.GetType().FullName!
            .Replace("ViewModel", "View", StringComparison.Ordinal)
            .Replace(".Design", ".", StringComparison.Ordinal);

        if (Type.GetType(name) is { } type)
        {
            var control = (Control)Activator.CreateInstance(type)!;
            control.DataContext = param;
            return control;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
