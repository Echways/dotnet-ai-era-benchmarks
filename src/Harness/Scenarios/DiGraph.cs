using Microsoft.Extensions.DependencyInjection;

namespace Harness.Scenarios;

public static class DiGraph
{
    public static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddTransient<Depth1>();
        services.AddTransient<Depth2>();
        services.AddScoped<Depth3>();
        services.AddTransient<Depth4>();
        services.AddSingleton<Depth5>();
        services.AddTransient<TransientService>();
        services.AddScoped<ScopedService>();
        services.AddSingleton<SingletonService>();
        services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
        foreach (var plugin in PluginTypes.All)
        {
            services.AddTransient(typeof(IPlugin), plugin);
        }

        return services;
    }
}

public sealed class Depth1(Depth2 next)
{
    public Depth2 Next { get; } = next;
}

public sealed class Depth2(Depth3 next, TransientService transient)
{
    public Depth3 Next { get; } = next;

    public TransientService Transient { get; } = transient;
}

public sealed class Depth3(Depth4 next)
{
    public Depth4 Next { get; } = next;
}

public sealed class Depth4(Depth5 next, SingletonService singleton)
{
    public Depth5 Next { get; } = next;

    public SingletonService Singleton { get; } = singleton;
}

public sealed class Depth5;

public sealed class TransientService;

public sealed class ScopedService;

public sealed class SingletonService;

public sealed class Order;

public interface IRepository<T>
{
    Type EntityType { get; }
}

public sealed class Repository<T>(SingletonService shared) : IRepository<T>
{
    public SingletonService Shared { get; } = shared;

    public Type EntityType => typeof(T);
}

public interface IPlugin
{
    int Order { get; }
}

public sealed class Plugin<TFirst, TSecond> : IPlugin
{
    public int Order => typeof(TFirst).Name.Length + typeof(TSecond).Name.Length;
}

public static class PluginTypes
{
    public static readonly Type[] All = Enumerable.Range(0, 20)
        .Select(i => typeof(Plugin<,>).MakeGenericType(SlotTypes.Markers[i % SlotTypes.Markers.Length], SlotTypes.Markers[i / SlotTypes.Markers.Length]))
        .ToArray();
}
