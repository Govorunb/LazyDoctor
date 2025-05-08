using Avalonia.Controls;
using Avalonia.Input;

namespace DesktopApp.Utilities;

internal static class UiWorkarounds
{
    public static void CalendarDatePickerScrollShouldResetTimeComponent()
    {
        // picking the date manually from the calendar resets the time to midnight
        // scrolling on the field in/decrements the date but keeps the time component (which is inconsistent - and, for me, inconvenient)
        InputElement.PointerWheelChangedEvent.AddClassHandler<CalendarDatePicker>(
            (calendar, _) => calendar.SelectedDate = calendar.SelectedDate?.Date,
            handledEventsToo: true);
    }
}
