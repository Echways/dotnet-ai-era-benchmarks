using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;
using StaticMetrics;

var source = new Option<DirectoryInfo>("--source") { Description = "Library src directory", Required = true };
var tests = new Option<DirectoryInfo?>("--tests") { Description = "Library tests directory" };
var output = new Option<FileInfo>("--out") { Description = "JSON output", Required = true };
var root = new RootCommand("Method-level and line metrics of a C# source tree") { source, tests, output };
root.SetAction(parse =>
{
    var src = SourceMetrics.Measure(parse.GetValue(source)!);
    var test = parse.GetValue(tests) is { Exists: true } testDirectory ? SourceMetrics.Measure(testDirectory) : null;
    var node = new JsonObject
    {
        ["source"] = src.ToJson(includeMethods: true),
        ["tests"] = test?.ToJson(includeMethods: false),
    };
    var file = parse.GetValue(output)!;
    file.Directory?.Create();
    File.WriteAllText(file.FullName, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"{parse.GetValue(source)!.FullName}: {src.Files} files, {src.CodeLines} code lines, {src.Methods.Count} methods");
    return 0;
});
return root.Parse(args).Invoke();
