using System.CommandLine;
using Glassbox.Instrumenter;

var tag = new Option<string>("--tag") { Description = "dotnet/runtime tag, e.g. v7.0.20", Required = true };
var source = new Option<DirectoryInfo>("--source") { Description = "Sparse checkout of the tag", Required = true };
var methods = new Option<FileInfo>("--methods") { Description = "Type.Method list to instrument" };
var targetFile = new Option<FileInfo>("--targets") { Description = "methods.tsv written by the call graph tool; matched by library, file and line" };
var libraries = new Option<FileInfo>("--libraries") { Description = "Library list", Required = true };
var scenarios = new Option<FileInfo[]>("--scenario") { Description = "Harness scenario sources to rename for the runner", AllowMultipleArgumentsPerToken = true };
var output = new Option<DirectoryInfo>("--out") { Description = "Output directory", Required = true };
var root = new RootCommand("Rename and instrument dotnet/runtime library sources for the glass-box build") { tag, source, methods, targetFile, libraries, scenarios, output };
root.SetAction(parse =>
{
    var job = new InstrumentationJob(
        parse.GetValue(tag)!,
        parse.GetValue(source)!,
        parse.GetValue(methods) is { } methodsFile ? File.ReadAllLines(methodsFile.FullName).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#')).ToArray() : [],
        parse.GetValue(targetFile) is { } targetsFile ? TargetLocation.Read(targetsFile) : [],
        File.ReadAllLines(parse.GetValue(libraries)!.FullName).Select(l => l.Trim()).Where(l => l.Length > 0).ToArray(),
        parse.GetValue(scenarios) ?? [],
        parse.GetValue(output)!);
    return job.Run();
});
return root.Parse(args).Invoke();
