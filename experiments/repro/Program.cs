using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Configuration;

BenchmarkRunner.Run<BindBenchmark>(args: args);

[MemoryDiagnoser]
public class BindBenchmark
{
    private readonly IConfigurationSection _section = new ConfigurationBuilder()
        .AddInMemoryCollection(Enumerable.Range(0, 50).Select(i =>
            new KeyValuePair<string, string?>($"Poco:P{i:00}", "value")))
        .Build()
        .GetSection("Poco");

    [Benchmark]
    public Poco50? Bind() => _section.Get<Poco50>();
}

public class Poco50
{
    public string? P00 { get; set; } public string? P01 { get; set; } public string? P02 { get; set; } public string? P03 { get; set; } public string? P04 { get; set; }
    public string? P05 { get; set; } public string? P06 { get; set; } public string? P07 { get; set; } public string? P08 { get; set; } public string? P09 { get; set; }
    public string? P10 { get; set; } public string? P11 { get; set; } public string? P12 { get; set; } public string? P13 { get; set; } public string? P14 { get; set; }
    public string? P15 { get; set; } public string? P16 { get; set; } public string? P17 { get; set; } public string? P18 { get; set; } public string? P19 { get; set; }
    public string? P20 { get; set; } public string? P21 { get; set; } public string? P22 { get; set; } public string? P23 { get; set; } public string? P24 { get; set; }
    public string? P25 { get; set; } public string? P26 { get; set; } public string? P27 { get; set; } public string? P28 { get; set; } public string? P29 { get; set; }
    public string? P30 { get; set; } public string? P31 { get; set; } public string? P32 { get; set; } public string? P33 { get; set; } public string? P34 { get; set; }
    public string? P35 { get; set; } public string? P36 { get; set; } public string? P37 { get; set; } public string? P38 { get; set; } public string? P39 { get; set; }
    public string? P40 { get; set; } public string? P41 { get; set; } public string? P42 { get; set; } public string? P43 { get; set; } public string? P44 { get; set; }
    public string? P45 { get; set; } public string? P46 { get; set; } public string? P47 { get; set; } public string? P48 { get; set; } public string? P49 { get; set; }
}
