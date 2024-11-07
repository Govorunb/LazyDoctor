using JetBrains.Annotations;

namespace DesktopApp.Test;

[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class TestClassAttribute : Attribute;
