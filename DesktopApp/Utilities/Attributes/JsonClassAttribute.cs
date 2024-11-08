using JetBrains.Annotations;

namespace DesktopApp.Utilities.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
public sealed class JsonClassAttribute : Attribute;
