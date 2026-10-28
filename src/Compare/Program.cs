using System.CommandLine;
using Compare;

var raw = new Option<DirectoryInfo>("--raw") { Description = "Raw results directory", DefaultValueFactory = _ => new DirectoryInfo(Path.Combine("results", "raw")) };
var summary = new Option<DirectoryInfo>("--summary") { Description = "Summary output directory", DefaultValueFactory = _ => new DirectoryInfo(Path.Combine("results", "summary")) };
var matrix = new Option<FileInfo>("--matrix") { Description = "matrix/versions.json", DefaultValueFactory = _ => new FileInfo(Path.Combine("matrix", "versions.json")) };
var minPercent = new Option<double>("--min-percent") { Description = "Smallest change TraceDiff reports", DefaultValueFactory = _ => 5 };
var root = new RootCommand("Aggregate .ember traces per matrix point and compare them against the baseline") { raw, summary, matrix, minPercent };
root.SetAction(parse =>
{
    var points = MatrixPoints.Load(parse.GetValue(matrix)!);
    var runs = RunLoader.Load(parse.GetValue(raw)!, points);
    if (runs.Count == 0)
    {
        Console.Error.WriteLine($"no .ember files under {parse.GetValue(raw)!.FullName}");
        return 1;
    }

    var writer = new SummaryWriter(parse.GetValue(summary)!, points, parse.GetValue(minPercent));
    writer.Write(runs);
    return 0;
});
return root.Parse(args).Invoke();
