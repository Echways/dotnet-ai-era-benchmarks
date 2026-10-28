using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.NewOnly;

[assembly: TraceId(NewOnlyScopes.KeyedSingleton, "newonly.keyed-singleton.x10k", "newonly")]
[assembly: TraceId(NewOnlyScopes.KeyedTransient, "newonly.keyed-transient.x10k", "newonly")]
[assembly: TraceId(NewOnlyScopes.KeyedEnumerable, "newonly.keyed-enumerable10.x10k", "newonly")]
[assembly: TraceId(NewOnlyScopes.SourceGenBind, "newonly.bind-poco50-source-generated.x1k", "newonly")]

namespace Harness.NewOnly;

public static class NewOnlyScopes
{
    public const int KeyedSingleton = 1900;
    public const int KeyedTransient = 1901;
    public const int KeyedEnumerable = 1902;
    public const int SourceGenBind = 1910;
}

public static class NewOnlyScenarios
{
    public static IReadOnlyList<ScenarioRegistration> All { get; } =
    [
        new("keyed-di", () => new KeyedServicesScenario(), NewOnly: true),
#if HARNESS_BINDER_SOURCEGEN
        new("binder-sourcegen", () => new SourceGeneratedBinderScenario(), NewOnly: true),
#else
        new("binder-sourcegen", null, NewOnly: true, Unavailable: "the 8.0 binder generator emits path-based InterceptsLocation, rejected by the C# 14 compiler (CS9270)"),
#endif
    ];
}
