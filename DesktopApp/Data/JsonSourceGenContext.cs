using System.Text.Json.Serialization;
using DesktopApp.Data.GitHub;
using DesktopApp.Data.Operators;
using DesktopApp.Data.Recruitment;
using DesktopApp.Recruitment;
using JetBrains.Annotations;

namespace DesktopApp.Data;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    // keep ctor assignments if not explicitly declared in json
    // e.g. a newly added ABCPrefsData property (with a new() initializer)
    //      should not deserialize to null if it's not present in an older prefs.json
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate
)]
[JsonSerializable(typeof(RawTagData))]
[JsonSerializable(typeof(Operator))]
[JsonSerializable(typeof(GachaTable))]
[JsonSerializable(typeof(Dictionary<string, Operator>))]
[JsonSerializable(typeof(UserPrefs.UserPrefsData))]
[JsonSerializable(typeof(RecruitmentPrefsData))]
[JsonSerializable(typeof(GithubFileStub))]
[JsonSerializable(typeof(GithubAkavache.HttpResponse))]
[UsedImplicitly]
public sealed partial class JsonSourceGenContext : JsonSerializerContext;
