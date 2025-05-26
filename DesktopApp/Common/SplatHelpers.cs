using System.Diagnostics;
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
using Constants = DesktopApp.Data.Constants;

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

        Debug.WriteLine($"Registering {nameof(SplatHelpers)}");

        // infra/plumbing
        SplatRegistrations.RegisterConstant(TimeProvider.System); // for unit tests, register the mock TimeProvider after this one
        RegisterLogging();

        SplatRegistrations.RegisterLazySingleton<IAppData, AppData>();
        SplatRegistrations.RegisterLazySingleton<UserPrefs>();
        SplatRegistrations.RegisterLazySingleton<TimeUtilsService>();
        SplatRegistrations.RegisterLazySingleton<GithubAkavache>();
        SplatRegistrations.RegisterLazySingleton<GithubDataAdapter>();

        // theoretically manual OCR (with OpenCvSharp) could be better with enough time investment since we know text layout/constraints
        // but until something else requires OpenCV, there's very little ROI on the >50MB dependency
#if USE_OPENCV
        SplatRegistrations.RegisterLazySingleton<IRecruitTagsOCR, OpenCvTagsOCR>();
#else
        SplatRegistrations.RegisterLazySingleton<WinRtOCR>();
        SplatRegistrations.RegisterLazySingleton<IRecruitTagsOCR, WinRtTagsOCR>();
#endif

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

        // view models
        SplatRegistrations.RegisterLazySingleton<RecruitPage>();
        SplatRegistrations.RegisterLazySingleton<ResourcePlannerPage>();
        SplatRegistrations.RegisterLazySingleton<SettingsPage>();
        SplatRegistrations.RegisterLazySingleton<MainWindowViewModel>();

        SplatRegistrations.SetupIOC();
    }

    private static void RegisterLogging()
    {
        var fileLogLevelSwitch = new LoggingLevelSwitch();
        SERVICES.RegisterConstant(fileLogLevelSwitch, "file");
        if (Constants.IsDev)
            return;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(formatProvider: CultureInfo.CurrentCulture)
            .WriteTo.File(AppData.GetFullPath($"{Constants.LogsAppDataPath}/.log"),
                formatProvider: CultureInfo.CurrentCulture,
                rollingInterval: RollingInterval.Day,
                levelSwitch: fileLogLevelSwitch)
            .CreateLogger();
        SERVICES.UseSerilogFullLogger();
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
