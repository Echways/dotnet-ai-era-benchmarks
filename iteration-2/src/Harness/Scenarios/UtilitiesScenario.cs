using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.Scenarios;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;

[assembly: TraceId(UtilitiesScopes.HostEnvironment, "utilities.host-environment-checks.x10k", "utilities")]
[assembly: TraceId(UtilitiesScopes.PoolGetReturn, "utilities.object-pool-get-return.x10k", "utilities")]
[assembly: TraceId(UtilitiesScopes.PoolCreate, "utilities.object-pool-create.x1k", "utilities")]
[assembly: TraceId(UtilitiesScopes.DependencyContextRead, "utilities.dependency-context-read.x100", "utilities")]
[assembly: TraceId(UtilitiesScopes.DependencyContextWrite, "utilities.dependency-context-write.x100", "utilities")]

namespace Harness.Scenarios;

public static class UtilitiesScopes
{
    public const int HostEnvironment = 1820;
    public const int PoolGetReturn = 1821;
    public const int PoolCreate = 1822;
    public const int DependencyContextRead = 1823;
    public const int DependencyContextWrite = 1824;
}

public sealed class PooledItem
{
    public int Uses { get; set; }
}

public sealed class StubHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;

    public string ApplicationName { get; set; } = "Harness";

    public string ContentRootPath { get; set; } = "/";

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

public sealed class UtilitiesScenario : IScenario
{
    private const int Batch = 10_000;
    private const int Documents = 100;

    private readonly byte[] _depsJson = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, $"{typeof(UtilitiesScenario).Assembly.GetName().Name}.deps.json"));

    public string Name => "utilities";

    public void Cold(Recorder recorder)
    {
    }

    public void Iteration(Recorder recorder)
    {
        var environment = new StubHostEnvironment();
        recorder.Measure(UtilitiesScopes.HostEnvironment, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(environment.IsDevelopment() ? 1 : 0);
                Sink.Keep(environment.IsEnvironment("Staging") ? 1 : 0);
            }
        });

        var provider = new DefaultObjectPoolProvider();
        var pool = provider.Create<PooledItem>();
        recorder.Measure(UtilitiesScopes.PoolGetReturn, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                var item = pool.Get();
                item.Uses++;
                pool.Return(item);
            }
        });

        recorder.Measure(UtilitiesScopes.PoolCreate, 1000, () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                Sink.Keep(new DefaultObjectPoolProvider().Create<PooledItem>());
            }
        });

        var bytes = _depsJson;
        recorder.Measure(UtilitiesScopes.DependencyContextRead, Documents, () =>
        {
            for (var i = 0; i < Documents; i++)
            {
                using var stream = new MemoryStream(bytes, writable: false);
                using var reader = new DependencyContextJsonReader();
                Sink.Keep(reader.Read(stream));
            }
        });

        DependencyContext context;
        using (var stream = new MemoryStream(bytes, writable: false))
        using (var reader = new DependencyContextJsonReader())
        {
            context = reader.Read(stream);
        }

        recorder.Measure(UtilitiesScopes.DependencyContextWrite, Documents, () =>
        {
            for (var i = 0; i < Documents; i++)
            {
                using var stream = new MemoryStream();
                new DependencyContextWriter().Write(context, stream);
                Sink.Keep(stream.Length);
            }
        });
    }

    public void Dispose()
    {
    }
}
