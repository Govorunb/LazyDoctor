using DesktopApp.Data;
using DesktopApp.Data.Operators;
using DesktopApp.Data.Recruitment;
using DesktopApp.Recruitment;

namespace DesktopApp.Utilities.Helpers;

internal static class SplatHelpers
{
    // ReSharper disable InconsistentNaming
    public static IReadonlyDependencyResolver LOCATOR => EnsureRegistered(Locator.Current);
    public static IMutableDependencyResolver SERVICES => EnsureRegistered(Locator.CurrentMutable);
    // ReSharper restore InconsistentNaming

    private static bool _registered;
    private static void Register()
    {
        if (_registered)
            return;

        LogHost.Default.Warn($"Registering {nameof(SplatHelpers)}");

        SplatRegistrations.RegisterConstant(new HttpClient());

        SplatRegistrations.RegisterConstant<IDataSource<GachaTable>>(
            new JsonDataSource<GachaTable>("gacha_table.json")
        );

        SplatRegistrations.RegisterLazySingleton<OperatorRepository>();
        SplatRegistrations.RegisterLazySingleton<RecruitableOperators>();
        SplatRegistrations.RegisterLazySingleton<TagsDataSource>();
        SplatRegistrations.RegisterLazySingleton<RecruitmentFilter>();

        SplatRegistrations.RegisterLazySingleton<RecruitTab>();
        SplatRegistrations.RegisterLazySingleton<MainWindowViewModel>();

        SplatRegistrations.SetupIOC();

        _registered = true;
    }

    private static T EnsureRegistered<T>(T thing)
    {
        if (!_registered)
            Register();
        return thing;
    }
}
