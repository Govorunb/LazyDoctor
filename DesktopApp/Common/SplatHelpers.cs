using System.Globalization;
using DesktopApp.Common.OCR;
using DesktopApp.Data;
using DesktopApp.Data.GitHub;
using DesktopApp.Data.Operators;
using DesktopApp.Data.Recruitment;
using DesktopApp.Data.Stages;
using DesktopApp.Recruitment;
using DesktopApp.Recruitment.Processing;
using DesktopApp.ResourcePlanner;
using DesktopApp.Settings;
using Serilog;
using Serilog.Core;
using Splat.Serilog;

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

        _registered = true; // re-entry

        LogHost.Default.Warn($"Registering {nameof(SplatHelpers)}");

        // infra/plumbing
        SplatRegistrations.RegisterConstant(TimeProvider.System); // for unit tests, register the mock TimeProvider after this one
        SplatRegistrations.RegisterLazySingleton<IAppData, AppData>();
        SplatRegistrations.RegisterLazySingleton<UserPrefs>();
        SplatRegistrations.RegisterLazySingleton<TimeUtilsService>();
        SplatRegistrations.RegisterLazySingleton<GithubAkavache>();
        SplatRegistrations.RegisterLazySingleton<GithubDataAdapter>();

        // TODO: move this out
        // + figure out load order w/ log level prefs, there's some places where we log before prefs are loaded (e.g. while loading prefs)
        // in the future, prefs will always load before data sources (they will depend on prefs for which repo to use)
        var fileLogLevelSwitch = new LoggingLevelSwitch();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(formatProvider: CultureInfo.CurrentCulture)
            .WriteTo.File(AppData.GetFullPath("logs/.log"),
                formatProvider: CultureInfo.CurrentCulture,
                rollingInterval: RollingInterval.Day,
                levelSwitch: fileLogLevelSwitch)
            .CreateLogger();
        SERVICES.UseSerilogFullLogger();

        SplatRegistrations.RegisterLazySingleton<WinRtOCR>();

        // data sources
        // could be registered with an attribute but that needs a whole source gen (aot/generics)
        RegisterTable<ZoneTable>("excel/zone_table.json");
        RegisterTable<OperatorTable>("excel/character_table.json");
        RegisterTable<StageTable>("excel/stage_table.json");
        RegisterTable<GachaTable>("excel/gacha_table.json");
        RegisterTable<GameConstants>("excel/gamedata_const.json");
        SplatRegistrations.RegisterLazySingleton<OperatorRepository>();
        SplatRegistrations.RegisterLazySingleton<RecruitableOperators>();
        SplatRegistrations.RegisterLazySingleton<TagsDataSource>();
        SplatRegistrations.RegisterLazySingleton<StageRepository>();
        SplatRegistrations.RegisterLazySingleton<WeeklyStages>();

        // processing
        SplatRegistrations.RegisterLazySingleton<TagParsingUtils>();
        SplatRegistrations.RegisterLazySingleton<RecruitmentFilter>();

        // theoretically manual OCR (with OpenCvSharp) could be better with enough time investment since we know text layout/constraints
        // but until something else requires OpenCV, there's very little ROI on the >50MB dependency
#if USE_OPENCV
        SplatRegistrations.RegisterLazySingleton<IRecruitTagsOCR, OpenCvTagsOCR>();
#else
        SplatRegistrations.RegisterLazySingleton<IRecruitTagsOCR, WinRtTagsOCR>();
#endif

        // view models
        SplatRegistrations.RegisterLazySingleton<RecruitPage>();
        SplatRegistrations.RegisterLazySingleton<ResourcePlannerPage>();
        SplatRegistrations.RegisterLazySingleton<SettingsPage>();
        SplatRegistrations.RegisterLazySingleton<MainWindowViewModel>();

        SplatRegistrations.SetupIOC();
    }

    private static void RegisterTable<TTable>(string path)
    {
        SERVICES.RegisterLazySingleton<IDataSource<TTable>>(
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
