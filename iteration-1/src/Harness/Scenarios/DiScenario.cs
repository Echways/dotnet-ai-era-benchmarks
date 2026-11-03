using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.Scenarios;
using Microsoft.Extensions.DependencyInjection;

[assembly: TraceId(DiScopes.ColdBuild100, "di.cold.build100", "di")]
[assembly: TraceId(DiScopes.ColdFirstResolve, "di.cold.first-resolve-depth5", "di")]
[assembly: TraceId(DiScopes.Build100, "di.build100.x100", "di")]
[assembly: TraceId(DiScopes.Build1000, "di.build1000.x10", "di")]
[assembly: TraceId(DiScopes.ValidateOnBuild, "di.validate-on-build100.x100", "di")]
[assembly: TraceId(DiScopes.FirstResolve, "di.first-resolve-depth5.x100", "di")]
[assembly: TraceId(DiScopes.Transient, "di.transient.x10k", "di")]
[assembly: TraceId(DiScopes.Scoped, "di.scoped.x10k", "di")]
[assembly: TraceId(DiScopes.Singleton, "di.singleton.x10k", "di")]
[assembly: TraceId(DiScopes.OpenGeneric, "di.open-generic.x10k", "di")]
[assembly: TraceId(DiScopes.Enumerable, "di.enumerable20.x10k", "di")]
[assembly: TraceId(DiScopes.CreateScope, "di.create-scope-dispose.x10k", "di")]

namespace Harness.Scenarios;

public static class DiScopes
{
    public const int ColdBuild100 = 1000;
    public const int ColdFirstResolve = 1001;
    public const int Build100 = 1010;
    public const int Build1000 = 1011;
    public const int ValidateOnBuild = 1012;
    public const int FirstResolve = 1013;
    public const int Transient = 1020;
    public const int Scoped = 1021;
    public const int Singleton = 1022;
    public const int OpenGeneric = 1023;
    public const int Enumerable = 1024;
    public const int CreateScope = 1025;
}

public sealed class DiScenario : IScenario
{
    private const int Batch = 10_000;
    private const int FreshProviders = 100;

    private readonly (Type Service, Type Implementation)[] _registrations = SlotTypes.Create(1000);
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public DiScenario()
    {
        _provider = DiGraph.CreateServices().BuildServiceProvider();
        _scope = _provider.CreateScope();
    }

    public string Name => "di";

    public void Cold(Recorder recorder)
    {
        ServiceProvider? provider = null;
        recorder.Cold(DiScopes.ColdBuild100, () => provider = BuildSlots(100, validate: false));
        provider!.Dispose();
        using var graph = DiGraph.CreateServices().BuildServiceProvider();
        recorder.Cold(DiScopes.ColdFirstResolve, () => Sink.Keep(graph.GetRequiredService<Depth1>()));
    }

    public void Iteration(Recorder recorder)
    {
        recorder.Measure(DiScopes.Build100, 100, () =>
        {
            for (var i = 0; i < 100; i++)
            {
                using var provider = BuildSlots(100, validate: false);
                Sink.Keep(provider);
            }
        });

        recorder.Measure(DiScopes.Build1000, 10, () =>
        {
            for (var i = 0; i < 10; i++)
            {
                using var provider = BuildSlots(1000, validate: false);
                Sink.Keep(provider);
            }
        });

        recorder.Measure(DiScopes.ValidateOnBuild, 100, () =>
        {
            for (var i = 0; i < 100; i++)
            {
                using var provider = BuildSlots(100, validate: true);
                Sink.Keep(provider);
            }
        });

        var fresh = new ServiceProvider[FreshProviders];
        for (var i = 0; i < fresh.Length; i++)
        {
            fresh[i] = DiGraph.CreateServices().BuildServiceProvider();
        }

        recorder.Measure(DiScopes.FirstResolve, FreshProviders, () =>
        {
            foreach (var provider in fresh)
            {
                Sink.Keep(provider.GetRequiredService<Depth1>());
            }
        });

        foreach (var provider in fresh)
        {
            provider.Dispose();
        }

        var root = _provider;
        var scoped = _scope.ServiceProvider;
        recorder.Measure(DiScopes.Transient, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(root.GetRequiredService<TransientService>());
            }
        });

        recorder.Measure(DiScopes.Scoped, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(scoped.GetRequiredService<ScopedService>());
            }
        });

        recorder.Measure(DiScopes.Singleton, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(root.GetRequiredService<SingletonService>());
            }
        });

        recorder.Measure(DiScopes.OpenGeneric, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(root.GetRequiredService<IRepository<Order>>());
            }
        });

        recorder.Measure(DiScopes.Enumerable, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(root.GetRequiredService<IEnumerable<IPlugin>>());
            }
        });

        recorder.Measure(DiScopes.CreateScope, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                using var scope = root.CreateScope();
                Sink.Keep(scope);
            }
        });
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    private ServiceProvider BuildSlots(int count, bool validate)
    {
        var services = new ServiceCollection();
        for (var i = 0; i < count; i++)
        {
            var (service, implementation) = _registrations[i];
            services.AddTransient(service, implementation);
        }

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = validate, ValidateScopes = validate });
    }
}
