using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.ReactiveUI;
using DesktopApp.ViewModels;

namespace DesktopApp;

public sealed class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Type> _registry = [];

    static ViewLocator()
    {
        foreach (var controlType in typeof(ViewLocator).Assembly.GetTypes())
        {
            if (controlType.BaseType is { IsConstructedGenericType: true } serviceType
                && serviceType.GetGenericTypeDefinition() == typeof(ReactiveUserControl<>))
            {
                var vmType = serviceType.GetGenericArguments()[0];
                _registry.Add(vmType, controlType);
                Locator.CurrentMutable.Register(() => Activator.CreateInstance(controlType)!, serviceType);
            }
        }
    }

    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var vmType = param.GetType();
        if (_registry.TryGetValue(vmType, out var registered))
            return Activator.CreateInstance(registered) as Control;

        if (LOCATOR.GetService(typeof(ReactiveUserControl<>).MakeGenericType(vmType)) is { } view)
            return (Control)view;

        var name = vmType.FullName!
            .Replace("ViewModel", "View", StringComparison.Ordinal)
            .Replace(".Design", ".", StringComparison.Ordinal);

        if (Type.GetType(name) is { } controlType && controlType.IsAssignableTo(typeof(Control)))
        {
            var control = (Control)Activator.CreateInstance(controlType)!;
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
