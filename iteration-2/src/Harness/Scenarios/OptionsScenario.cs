using System.ComponentModel.DataAnnotations;
using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.Core.Data;
using Harness.Scenarios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: TraceId(OptionsScopes.ColdFirstValue, "options.cold.first-value", "options")]
[assembly: TraceId(OptionsScopes.Value, "options.ioptions-value.x10k", "options")]
[assembly: TraceId(OptionsScopes.Monitor, "options.monitor-current-value.x10k", "options")]
[assembly: TraceId(OptionsScopes.Snapshot, "options.snapshot-new-scope.x1k", "options")]
[assembly: TraceId(OptionsScopes.Validate, "options.validate-data-annotations.x1k", "options")]
[assembly: TraceId(OptionsScopes.Registration, "options.registration-configure-validate.x1k", "options")]
[assembly: TraceId(OptionsScopes.FactoryCreate, "options.factory-create-poco50.x1k", "options")]
[assembly: TraceId(OptionsScopes.MonitorGet, "options.monitor-get-named.x10k", "options")]
[assembly: TraceId(OptionsScopes.ConfigureFromConfiguration, "options.configure-from-configuration-first-value.x100", "options")]
[assembly: TraceId(OptionsScopes.ChangeTokenSource, "options.configuration-change-token-source.x10k", "options")]

namespace Harness.Scenarios;

public static class OptionsScopes
{
    public const int ColdFirstValue = 1200;
    public const int Value = 1210;
    public const int Monitor = 1211;
    public const int Snapshot = 1212;
    public const int Validate = 1213;
    public const int Registration = 1214;
    public const int FactoryCreate = 1215;
    public const int MonitorGet = 1216;
    public const int ConfigureFromConfiguration = 1217;
    public const int ChangeTokenSource = 1218;
}

public sealed class ValidatedOptions
{
    [Range(1, 1000)]
    public int P05 { get; set; }

    [Required]
    [MinLength(3)]
    public string P02 { get; set; } = "";

    [Range(0.0, 1000.0)]
    public double P04 { get; set; }

    [Required]
    [StringLength(64)]
    public string P07 { get; set; } = "";
}

public sealed class OptionsScenario : IScenario
{
    private const int Batch = 10_000;

    private readonly IConfigurationRoot _configuration;
    private ServiceProvider _provider = null!;

    public OptionsScenario()
    {
        using var stream = new MemoryStream(ConfigDocument.Create(), writable: false);
        _configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();
    }

    public string Name => "options";

    public void Cold(Recorder recorder)
    {
        var services = new ServiceCollection();
        services.Configure<Poco50>(_configuration.GetSection("Poco"));
        services.AddOptions<ValidatedOptions>().Bind(_configuration.GetSection("Poco")).ValidateDataAnnotations();
        _provider = services.BuildServiceProvider();
        recorder.Cold(OptionsScopes.ColdFirstValue, () => Sink.Keep(_provider.GetRequiredService<IOptions<Poco50>>().Value));
    }

    public void Iteration(Recorder recorder)
    {
        var options = _provider.GetRequiredService<IOptions<Poco50>>();
        recorder.Measure(OptionsScopes.Value, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(options.Value);
            }
        });

        var monitor = _provider.GetRequiredService<IOptionsMonitor<Poco50>>();
        recorder.Measure(OptionsScopes.Monitor, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(monitor.CurrentValue);
            }
        });

        var provider = _provider;
        recorder.Measure(OptionsScopes.Snapshot, 1000, () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                using var scope = provider.CreateScope();
                Sink.Keep(scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<Poco50>>().Value);
            }
        });

        recorder.Measure(OptionsScopes.Validate, 1000, () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                using var scope = provider.CreateScope();
                Sink.Keep(scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ValidatedOptions>>().Value);
            }
        });

        recorder.Measure(OptionsScopes.Registration, 1000, () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                var services = new ServiceCollection();
                services.Configure<Poco50>(o => o.P01 = 1);
                services.AddOptions<ValidatedOptions>().Configure(o => o.P05 = 5).Validate(o => o.P05 > 0);
                Sink.Keep(services);
            }
        });

        var factory = provider.GetRequiredService<IOptionsFactory<Poco50>>();
        recorder.Measure(OptionsScopes.FactoryCreate, 1000, () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                Sink.Keep(factory.Create(Options.DefaultName));
            }
        });

        recorder.Measure(OptionsScopes.MonitorGet, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(monitor.Get(Options.DefaultName));
            }
        });

        var section = _configuration.GetSection("Poco");
        recorder.Measure(OptionsScopes.ConfigureFromConfiguration, 100, () =>
        {
            for (var i = 0; i < 100; i++)
            {
                var services = new ServiceCollection();
                services.Configure<Poco50>(section);
                using var configured = services.BuildServiceProvider();
                Sink.Keep(configured.GetRequiredService<IOptions<Poco50>>().Value);
            }
        });

        var tokenSource = new ConfigurationChangeTokenSource<Poco50>(section);
        recorder.Measure(OptionsScopes.ChangeTokenSource, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(tokenSource.GetChangeToken());
            }
        });
    }

    public void Dispose()
    {
        _provider.Dispose();
        (_configuration as IDisposable)?.Dispose();
    }
}
