using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.ReactiveUI;

namespace DesktopApp;

public sealed class ViewLocator : IDataTemplate, IViewLocator
{
    private static readonly Dictionary<Type, Type> _registry = [];

    [RequiresUnreferencedCode("Accesses all types in assembly")]
    public static void RegisterViews(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        foreach (var controlType in assembly.GetTypes())
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
        if (TryGetControl(vmType) is { } control)
        {
            control.DataContext = param;
            return control;
        }

        return new TextBlock { Text = "No view for: " + vmType.Name };
    }

    private static Control? TryGetControl(Type vmType)
    {
        if (_registry.TryGetValue(vmType, out var registered))
            return Activator.CreateInstance(registered) as Control;

        var controlBaseType = typeof(ReactiveUserControl<>).MakeGenericType(vmType);
        if (LOCATOR.GetService(controlBaseType) is Control view)
            return view;

        var name = vmType.FullName!;
        if (name.EndsWith("ViewModel", StringComparison.Ordinal))
            name = name.Replace("ViewModel", "View", StringComparison.Ordinal);
        else
            name += "View";

        if (vmType.Name.StartsWith("Design", StringComparison.Ordinal))
            name = name.Replace(".Design", ".", StringComparison.Ordinal);

        var controlType = Type.GetType(name);
        return controlType?.IsAssignableTo(typeof(Control)) == true
            ? (Control)Activator.CreateInstance(controlType)!
            : null;
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }

    public IViewFor? ResolveView<T>(T? viewModel, string? contract = null)
    {
        if (!Match(viewModel)) return null;
        return Build(viewModel) as IViewFor;
    }
}
