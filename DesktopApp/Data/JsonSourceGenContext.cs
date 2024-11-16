using System.Text.Json.Serialization;
using DesktopApp.Data.Operators;
using DesktopApp.Data.Recruitment;
using DesktopApp.Recruitment;
using JetBrains.Annotations;

namespace DesktopApp.Data;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RawTagData))]
[JsonSerializable(typeof(Operator))]
[JsonSerializable(typeof(GachaTable))]
[JsonSerializable(typeof(Dictionary<string, Operator>))]
[JsonSerializable(typeof(UserPrefs.UserPrefsData))]
[JsonSerializable(typeof(RecruitmentPrefsData))]
[UsedImplicitly]
public sealed partial class JsonSourceGenContext : JsonSerializerContext;
