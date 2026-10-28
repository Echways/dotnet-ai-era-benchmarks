using Harness.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Harness.NewOnly;

public sealed class KeyedService;

public sealed class KeyedServicesScenario : IScenario
{
    private const int Batch = 10_000;
    private ServiceProvider _provider = null!;

    public string Name => "keyed-di";

    public void Cold(Recorder recorder)
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<KeyedService>("singleton");
        services.AddKeyedTransient<KeyedService>("transient");
        for (var i = 0; i < 10; i++)
        {
            services.AddKeyedTransient<KeyedService>("many");
        }

        _provider = services.BuildServiceProvider();
    }

    public void Iteration(Recorder recorder)
    {
        var provider = _provider;
        recorder.Measure(NewOnlyScopes.KeyedSingleton, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(provider.GetRequiredKeyedService<KeyedService>("singleton"));
            }
        });

        recorder.Measure(NewOnlyScopes.KeyedTransient, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(provider.GetRequiredKeyedService<KeyedService>("transient"));
            }
        });

        recorder.Measure(NewOnlyScopes.KeyedEnumerable, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(provider.GetKeyedServices<KeyedService>("many"));
            }
        });
    }

    public void Dispose() => _provider.Dispose();
}
