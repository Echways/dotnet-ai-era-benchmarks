using System.CommandLine;
using System.Globalization;
using EmberTrace;

namespace Compare;

public static class MethodsCommand
{
    public static Command Create()
    {
        var traces = new Option<DirectoryInfo>("--traces") { Description = "Glass-box output directory with <tag>/<scenario>-<pid>.ember", DefaultValueFactory = _ => new DirectoryInfo(Path.Combine("results", "glassbox")) };
        var output = new Option<FileInfo>("--out") { Description = "Per-process inclusive and self time per method, CSV", DefaultValueFactory = _ => new FileInfo(Path.Combine("results", "glassbox", "methods.csv")) };
        var command = new Command("methods", "Export inclusive and self time per method for every glass-box process") { traces, output };
        command.SetAction(parse =>
        {
            var rows = Rows(parse.GetValue(traces)!).ToList();
            if (rows.Count == 0)
            {
                Console.Error.WriteLine($"no .ember traces under {parse.GetValue(traces)!.FullName}");
                return 1;
            }

            File.WriteAllText(parse.GetValue(output)!.FullName, MethodTimes.WriteCsv(rows));
            Console.WriteLine($"[compare] {rows.Count} method rows -> {parse.GetValue(output)!.FullName}");
            return 0;
        });
        return command;
    }

    private static IEnumerable<string> Rows(DirectoryInfo traces)
    {
        foreach (var tagDirectory in traces.EnumerateDirectories().OrderBy(d => d.Name, StringComparer.Ordinal))
        {
            foreach (var file in tagDirectory.EnumerateFiles("*.ember").Where(f => !f.Name.EndsWith(".cold.ember", StringComparison.Ordinal)).OrderBy(f => f.Name, StringComparer.Ordinal))
            {
                var stem = Path.GetFileNameWithoutExtension(file.Name);
                var dash = stem.LastIndexOf('-');
                var hasPid = dash > 0 && int.TryParse(stem.AsSpan(dash + 1), out _);
                var scenario = hasPid ? stem[..dash] : stem;
                var pid = hasPid ? int.Parse(stem.AsSpan(dash + 1), CultureInfo.InvariantCulture) : 0;

                foreach (var method in MethodTimes.Read(TraceFormat.Read(file.FullName)))
                {
                    yield return method.CsvRow(tagDirectory.Name, scenario, pid);
                }
            }
        }
    }
}
