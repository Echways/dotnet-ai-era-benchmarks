using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using EmberTrace;
using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.TrackB.Scenarios;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: TraceId(AspNetCoreScopes.ColdStart, "aspnetcore.cold.start-kestrel", "aspnetcore")]
[assembly: TraceId(AspNetCoreScopes.ColdFirstRequest, "aspnetcore.cold.first-request", "aspnetcore")]
[assembly: TraceId(AspNetCoreScopes.HelloBatch, "aspnetcore.hello.batch400", "aspnetcore")]
[assembly: TraceId(AspNetCoreScopes.JsonBatch, "aspnetcore.json.batch400", "aspnetcore")]
[assembly: TraceId(AspNetCoreScopes.RoutesBatch, "aspnetcore.routes500.batch400", "aspnetcore")]
[assembly: TraceId(AspNetCoreScopes.HelloRequest, "aspnetcore.hello.request", "aspnetcore")]
[assembly: TraceId(AspNetCoreScopes.JsonRequest, "aspnetcore.json.request", "aspnetcore")]
[assembly: TraceId(AspNetCoreScopes.RoutesRequest, "aspnetcore.routes500.request", "aspnetcore")]

namespace Harness.TrackB.Scenarios;

public static class AspNetCoreScopes
{
    public const int ColdStart = 2300;
    public const int ColdFirstRequest = 2301;
    public const int HelloBatch = 2310;
    public const int JsonBatch = 2311;
    public const int RoutesBatch = 2312;
    public const int HelloRequest = 2320;
    public const int JsonRequest = 2321;
    public const int RoutesRequest = 2322;
}

public sealed record Greeting(string Message, int Code, DateTimeOffset At);

public sealed class AspNetCoreScenario : IScenario, IScenarioArtifacts
{
    private const int Middlewares = 10;
    private const int Routes = 500;
    private const int RequestsPerBatch = 400;
    private const int RequestsPerSecond = 4000;
    private const int MaxConcurrency = 32;

    private readonly string? _bombardier = FindExecutable("bombardier");
    private readonly JsonArray _bombardierResults = [];
    private readonly string[] _routePaths = Enumerable.Range(0, RequestsPerBatch).Select(i => $"/r{i * 37 % Routes}/items/{i}").ToArray();
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private long _lateSends;

    public string Name => "aspnetcore";

    public void Cold(Recorder recorder)
    {
        recorder.Cold(AspNetCoreScopes.ColdStart, () => _app = StartServer());
        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        _client = new HttpClient(new SocketsHttpHandler { MaxConnectionsPerServer = MaxConcurrency, PooledConnectionLifetime = Timeout.InfiniteTimeSpan })
        {
            BaseAddress = new Uri(address),
        };
        recorder.Cold(AspNetCoreScopes.ColdFirstRequest, () => Sink.Keep(_client.GetStringAsync("/hello").GetAwaiter().GetResult()));
    }

    public void Iteration(Recorder recorder)
    {
        Load(recorder, AspNetCoreScopes.HelloBatch, AspNetCoreScopes.HelloRequest, ["/hello"]);
        Load(recorder, AspNetCoreScopes.JsonBatch, AspNetCoreScopes.JsonRequest, ["/json"]);
        Load(recorder, AspNetCoreScopes.RoutesBatch, AspNetCoreScopes.RoutesRequest, _routePaths);
    }

    public void WriteArtifacts(string directory, string stem)
    {
        var node = new JsonObject
        {
            ["load_generator"] = _bombardier is null ? "builtin-httpclient-fixed-rate" : "bombardier",
            ["bombardier_path"] = _bombardier,
            ["requests_per_batch"] = RequestsPerBatch,
            ["target_rps"] = RequestsPerSecond,
            ["max_concurrency"] = MaxConcurrency,
            ["middlewares"] = Middlewares,
            ["routes"] = Routes,
            ["late_sends_over_2ms"] = _lateSends,
            ["bombardier_runs"] = _bombardierResults.DeepClone(),
        };
        File.WriteAllText(Path.Combine(directory, stem + ".load.json"), node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    public void Dispose()
    {
        _client.Dispose();
        _app.StopAsync().GetAwaiter().GetResult();
        _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private void Load(Recorder recorder, int batchScope, int requestScope, string[] paths)
    {
        if (_bombardier is null)
        {
            var record = recorder.Recording;
            recorder.MeasureProcessWide(batchScope, RequestsPerBatch, () =>
                Interlocked.Add(ref _lateSends, FixedRate(paths, record ? requestScope : null).GetAwaiter().GetResult()));
            return;
        }

        var url = new Uri(_client.BaseAddress!, paths[0]).ToString();
        string? output = null;
        recorder.MeasureProcessWide(batchScope, RequestsPerBatch, () => output = RunBombardier(url));
        if (recorder.Recording && output is not null)
        {
            var parsed = JsonNode.Parse(output);
            _bombardierResults.Add(new JsonObject { ["scope_id"] = batchScope, ["url"] = url, ["result"] = parsed?["result"]?.DeepClone() });
        }
    }

    private async Task<long> FixedRate(string[] paths, int? requestScope)
    {
        var ticksPerRequest = (double)Stopwatch.Frequency / RequestsPerSecond;
        var lateThreshold = Stopwatch.Frequency / 500;
        using var gate = new SemaphoreSlim(MaxConcurrency);
        var inflight = new Task[RequestsPerBatch];
        long late = 0;
        var start = Stopwatch.GetTimestamp();
        var sent = 0;
        while (sent < RequestsPerBatch)
        {
            var now = Stopwatch.GetTimestamp();
            var due = Math.Min(RequestsPerBatch, (int)((now - start) / ticksPerRequest) + 1);
            for (; sent < due; sent++)
            {
                if (now - (start + (long)(sent * ticksPerRequest)) > lateThreshold)
                {
                    late++;
                }

                await gate.WaitAsync().ConfigureAwait(false);
                inflight[sent] = Send(paths[sent % paths.Length], requestScope, gate);
            }

            if (sent < RequestsPerBatch)
            {
                await Task.Delay(1).ConfigureAwait(false);
            }
        }

        await Task.WhenAll(inflight).ConfigureAwait(false);
        return late;
    }

    private async Task Send(string path, int? requestScope, SemaphoreSlim gate)
    {
        try
        {
            if (requestScope is { } id)
            {
                await using (Tracer.ScopeAsync(id))
                {
                    await Fetch(path).ConfigureAwait(false);
                }
            }
            else
            {
                await Fetch(path).ConfigureAwait(false);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task Fetch(string path)
    {
        using var response = await _client.GetAsync(path, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"{path} returned {(int)response.StatusCode}");
        }

        Sink.Keep(response.Content.Headers.ContentLength ?? 0);
    }

    private string RunBombardier(string url)
    {
        var arguments = $"-c {MaxConcurrency} -n {RequestsPerBatch} -r {RequestsPerSecond} -l -p r -o json {url}";
        using var process = Process.Start(new ProcessStartInfo(_bombardier!, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException("bombardier did not start");
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"bombardier exited with {process.ExitCode}: {process.StandardError.ReadToEnd()}");
        }

        return output;
    }

    private static WebApplication StartServer()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        var app = builder.Build();
        for (var i = 0; i < Middlewares; i++)
        {
            var key = $"mw-{i}";
            app.Use((context, next) =>
            {
                context.Items[key] = context.Request.Path.HasValue;
                return next(context);
            });
        }

        app.MapGet("/hello", () => "Hello, World!");
        app.MapGet("/json", () => new Greeting("Hello, World!", 42, DateTimeOffset.UnixEpoch));
        for (var i = 0; i < Routes; i++)
        {
            var route = i;
            app.MapGet($"/r{i}/items/{{id:int}}", (int id) => route + id);
        }

        app.StartAsync().GetAwaiter().GetResult();
        return app;
    }

    private static string? FindExecutable(string name)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        return path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(directory => Path.Combine(directory, name))
            .FirstOrDefault(File.Exists);
    }
}
