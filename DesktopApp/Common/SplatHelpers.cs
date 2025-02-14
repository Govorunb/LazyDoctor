using DesktopApp.Common.OCR;
using DesktopApp.Data;
using DesktopApp.Data.GitHub;
using DesktopApp.Data.Operators;
using DesktopApp.Data.Recruitment;
using DesktopApp.Data.Stages;
using DesktopApp.Recruitment;
using DesktopApp.Recruitment.Processing;
using DesktopApp.Settings;

namespace DesktopApp.Common;

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

        // infra/plumbing
        SplatRegistrations.RegisterConstant(TimeProvider.System); // for unit tests, register the mock TimeProvider after this one
        SplatRegistrations.RegisterLazySingleton<IAppData, AppData>();
        SplatRegistrations.RegisterLazySingleton<GithubAkavache>();
        SplatRegistrations.RegisterLazySingleton<GithubDataAdapter>();
        // DebugLogger is registered automatically by default, but our debug log gets spammed by MonoMod from HotAvalonia
        SplatRegistrations.RegisterConstant<ILogger>(new ConsoleLogger());
        SplatRegistrations.RegisterLazySingleton<WinRtOCR>();

        // data sources
        SplatRegistrations.RegisterLazySingleton<UserPrefs>();
        RegisterTable<GachaTable>("excel/character_table.json");
        RegisterTable<GachaTable>("excel/gacha_table.json");
        RegisterTable<GameConstants>("excel/gamedata_const.json");
        SplatRegistrations.RegisterLazySingleton<OperatorRepository>();
        SplatRegistrations.RegisterLazySingleton<RecruitableOperators>();
        SplatRegistrations.RegisterLazySingleton<TagsDataSource>();
        SplatRegistrations.RegisterLazySingleton<StageRepository>();
        SplatRegistrations.RegisterLazySingleton<WeeklyStages>();

        // processing
        SplatRegistrations.RegisterLazySingleton<TextParsingUtils>();
        SplatRegistrations.RegisterLazySingleton<RecruitmentFilter>();
        // "temporary"
        // theoretically manual OCR (with OpenCvSharp) could be better with enough time investment since we know text layout/constraints
        // but until something else requires OpenCV, there's very little ROI on the >50MB dependency
        // SplatRegistrations.RegisterLazySingleton<IRecruitTagsOCR, OpenCvTagsOCR>();
        SplatRegistrations.RegisterLazySingleton<IRecruitTagsOCR, WinRtTagsOCR>();

        // view models
        SplatRegistrations.RegisterLazySingleton<RecruitPage>();
        SplatRegistrations.RegisterLazySingleton<SettingsPage>();
        SplatRegistrations.RegisterLazySingleton<MainWindowViewModel>();

        SplatRegistrations.SetupIOC();

        _registered = true;
    }

    private static void RegisterTable<TTable>(string path)
    {
        Locator.CurrentMutable.RegisterLazySingleton<IDataSource<TTable>>(
            () => LOCATOR.GetService<GithubDataAdapter>()!
                .GetDataSource<TTable>(path)
        );
    }

    private static T EnsureRegistered<T>(T thing)
    {
        if (!_registered)
            Register();
        return thing;
    }
}
