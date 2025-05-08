using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace DesktopApp.ResourcePlanner.Views;

public sealed partial class ResourcePlannerPageView : ReactiveUserControl<ResourcePlannerPage>
{
    public ResourcePlannerPageView()
    {
        InitializeComponent();
        this.WhenActivated(_ =>
        {
            this.BindValidation(ViewModel, vm => vm.Setup!.TargetStageCode, v => v.StageErrors);
            this.BindValidation(ViewModel, vm => vm.Setup!.TargetDate, v => v.DateErrors);
        });
    }

    private object? DateErrors
    {
        get => DataValidationErrors.GetErrors(InitialDatePicker);
        [UsedImplicitly]
        set
        {
            var errors = ParseErrors(value)?.ToArray();
            DataValidationErrors.SetErrors(InitialDatePicker, errors);
            DataValidationErrors.SetErrors(TargetDatePicker, errors);
        }
    }

    private object? StageErrors
    {
        get => DataValidationErrors.GetErrors(StagePicker);
        [UsedImplicitly]
        set => DataValidationErrors.SetErrors(StagePicker, ParseErrors(value));
    }

    private static IEnumerable<object>? ParseErrors(object? value)
    {
        return value switch
        {
            string s when !string.IsNullOrWhiteSpace(s)
                => s.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                    .Distinct(),
            _ => null,
        };
    }
}
