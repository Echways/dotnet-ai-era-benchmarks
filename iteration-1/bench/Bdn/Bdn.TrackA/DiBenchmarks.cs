using BenchmarkDotNet.Attributes;
using Harness.Scenarios;
using Microsoft.Extensions.DependencyInjection;

namespace Bdn.TrackA;

[MemoryDiagnoser]
public class DiBenchmarks
{
    private readonly (Type Service, Type Implementation)[] _slots = SlotTypes.Create(100);
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;

    [GlobalSetup]
    public void Setup()
    {
        _provider = DiGraph.CreateServices().BuildServiceProvider();
        _scope = _provider.CreateScope();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Benchmark]
    public object Transient() => _provider.GetRequiredService<TransientService>();

    [Benchmark]
    public object Scoped() => _scope.ServiceProvider.GetRequiredService<ScopedService>();

    [Benchmark]
    public object Singleton() => _provider.GetRequiredService<SingletonService>();

    [Benchmark]
    public object OpenGeneric() => _provider.GetRequiredService<IRepository<Order>>();

    [Benchmark]
    public object Enumerable20() => _provider.GetRequiredService<IEnumerable<IPlugin>>();

    [Benchmark]
    public void CreateScopeDispose()
    {
        using var scope = _provider.CreateScope();
    }

    [Benchmark]
    public void Build100() => Build(validate: false).Dispose();

    [Benchmark]
    public void ValidateOnBuild100() => Build(validate: true).Dispose();

    [Benchmark]
    public object FirstResolveDepth5()
    {
        using var provider = DiGraph.CreateServices().BuildServiceProvider();
        return provider.GetRequiredService<Depth1>();
    }

    private ServiceProvider Build(bool validate)
    {
        var services = new ServiceCollection();
        foreach (var (service, implementation) in _slots)
        {
            services.AddTransient(service, implementation);
        }

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = validate, ValidateScopes = validate });
    }
}
