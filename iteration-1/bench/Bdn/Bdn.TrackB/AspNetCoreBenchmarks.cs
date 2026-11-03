using System.Net;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bdn.TrackB;

public sealed record Greeting(string Message, int Code, DateTimeOffset At);

[MemoryDiagnoser]
public class AspNetCoreBenchmarks
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private int _cursor;

    [GlobalSetup]
    public void Setup()
    {
        InBoxCheck.Ensure();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        _app = builder.Build();
        for (var i = 0; i < 10; i++)
        {
            var key = $"mw-{i}";
            _app.Use((context, next) =>
            {
                context.Items[key] = context.Request.Path.HasValue;
                return next(context);
            });
        }

        _app.MapGet("/hello", () => "Hello, World!");
        _app.MapGet("/json", () => new Greeting("Hello, World!", 42, DateTimeOffset.UnixEpoch));
        for (var i = 0; i < 500; i++)
        {
            var route = i;
            _app.MapGet($"/r{i}/items/{{id:int}}", (int id) => route + id);
        }

        _app.StartAsync().GetAwaiter().GetResult();
        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client.Dispose();
        _app.StopAsync().GetAwaiter().GetResult();
    }

    [Benchmark]
    public string Hello() => _client.GetStringAsync("/hello").GetAwaiter().GetResult();

    [Benchmark]
    public string Json() => _client.GetStringAsync("/json").GetAwaiter().GetResult();

    [Benchmark]
    public string Routes500()
    {
        var i = _cursor++;
        return _client.GetStringAsync($"/r{i * 37 % 500}/items/{i}").GetAwaiter().GetResult();
    }
}
