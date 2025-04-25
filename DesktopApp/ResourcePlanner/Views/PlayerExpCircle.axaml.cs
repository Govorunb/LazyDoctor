using System.Reflection;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DesktopApp.Data.Player;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Controls.Primitives;

namespace DesktopApp.ResourcePlanner.Views;

public sealed partial class PlayerExpCircle : ReactiveUserControl<PlayerExpData>
{
    private static readonly FieldInfo _aniVisField = typeof(ProgressRing).GetField("_animatedVisualSource", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly Type _compHandler = typeof(ProgressRingAnimatedVisual).GetNestedType("CustomCompHandler", BindingFlags.NonPublic)!;
    private static readonly FieldInfo _handlerField = typeof(ProgressRingAnimatedVisual).GetField("_handler", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo _durationField = _compHandler.GetField("_duration", BindingFlags.NonPublic | BindingFlags.Instance)!;
    public PlayerExpCircle()
    {
        InitializeComponent();
        Ring.TemplateApplied += (_, e) =>
        {
            // genius move really
            // default has a *two second* long animation
            var aniVis = e.NameScope.Get<ProgressRingAnimatedVisual>("AnimatedVisual");
            var handler = _handlerField.GetValue(aniVis);
            _durationField.SetValue(handler, 0.15f);
        };
    }
}
