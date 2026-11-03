using Harness.Core;
using Harness.Core.Data;
using Microsoft.Extensions.Configuration;

namespace Harness.NewOnly;

public sealed class SourceGeneratedBinderScenario : IScenario
{
    private IConfigurationRoot _root = null!;

    public string Name => "binder-sourcegen";

    public void Cold(Recorder recorder)
    {
        using var stream = new MemoryStream(ConfigDocument.Create(), writable: false);
        _root = new ConfigurationBuilder().AddJsonStream(stream).Build();
    }

    public void Iteration(Recorder recorder)
    {
        var poco = _root.GetSection("Poco");
        recorder.Measure(NewOnlyScopes.SourceGenBind, 1000, () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                Sink.Keep(poco.Get<Poco50>());
            }
        });
    }

    public void Dispose() => (_root as IDisposable)?.Dispose();
}
