using System.CommandLine;
using Glassbox.Instrumenter;

var tag = new Option<string>("--tag") { Description = "dotnet/runtime tag, e.g. v7.0.20", Required = true };
var source = new Option<DirectoryInfo>("--source") { Description = "Sparse checkout of the tag", Required = true };
var methods = new Option<FileInfo>("--methods") { Description = "Type.Method list to instrument", Required = true };
var libraries = new Option<FileInfo>("--libraries") { Description = "Library list", Required = true };
var scenarios = new Option<FileInfo[]>("--scenario") { Description = "Harness scenario sources to rename for the runner", AllowMultipleArgumentsPerToken = true };
var output = new Option<DirectoryInfo>("--out") { Description = "Output directory", Required = true };
var root = new RootCommand("Rename and instrument dotnet/runtime library sources for the glass-box build") { tag, source, methods, libraries, scenarios, output };
root.SetAction(parse =>
{
    var job = new InstrumentationJob(
        parse.GetValue(tag)!,
        parse.GetValue(source)!,
        File.ReadAllLines(parse.GetValue(methods)!.FullName).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#')).ToArray(),
        File.ReadAllLines(parse.GetValue(libraries)!.FullName).Select(l => l.Trim()).Where(l => l.Length > 0).ToArray(),
        parse.GetValue(scenarios) ?? [],
        parse.GetValue(output)!);
    return job.Run();
});
return root.Parse(args).Invoke();
