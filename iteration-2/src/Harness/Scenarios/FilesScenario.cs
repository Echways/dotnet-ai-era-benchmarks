using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.Scenarios;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

[assembly: TraceId(FilesScopes.NullProvider, "files.null-provider.x10k", "files")]
[assembly: TraceId(FilesScopes.PhysicalFileInfo, "files.physical-get-file-info.x10k", "files")]
[assembly: TraceId(FilesScopes.PhysicalDirectory, "files.physical-get-directory-contents.x1k", "files")]
[assembly: TraceId(FilesScopes.Globbing, "files.matcher-execute-tree200.x100", "files")]

namespace Harness.Scenarios;

public static class FilesScopes
{
    public const int NullProvider = 1810;
    public const int PhysicalFileInfo = 1811;
    public const int PhysicalDirectory = 1812;
    public const int Globbing = 1813;
}

public sealed class FilesScenario : IScenario
{
    private const int Batch = 10_000;
    private const int Directories = 10;
    private const int FilesPerDirectory = 20;

    private readonly string _directory = Directory.CreateTempSubdirectory("harness-files-").FullName;
    private readonly string[] _relativeFiles;
    private readonly PhysicalFileProvider _physical;

    public FilesScenario()
    {
        var files = new List<string>();
        for (var d = 0; d < Directories; d++)
        {
            var directory = Path.Combine(_directory, $"d{d}");
            Directory.CreateDirectory(directory);
            for (var f = 0; f < FilesPerDirectory; f++)
            {
                var extension = f % 2 == 0 ? "txt" : "cs";
                File.WriteAllText(Path.Combine(directory, $"f{f}.{extension}"), "content");
                files.Add($"d{d}/f{f}.{extension}");
            }
        }

        _relativeFiles = files.ToArray();
        _physical = new PhysicalFileProvider(_directory);
    }

    public string Name => "files";

    public void Cold(Recorder recorder)
    {
    }

    public void Iteration(Recorder recorder)
    {
        var provider = new NullFileProvider();
        recorder.Measure(FilesScopes.NullProvider, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(provider.GetFileInfo("d0/f0.txt"));
                Sink.Keep(provider.GetDirectoryContents("d0"));
                Sink.Keep(provider.Watch("**/*.txt"));
            }
        });

        var physical = _physical;
        var files = _relativeFiles;
        recorder.Measure(FilesScopes.PhysicalFileInfo, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(physical.GetFileInfo(files[i % files.Length]));
            }
        });

        recorder.Measure(FilesScopes.PhysicalDirectory, 1000, () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                var count = 0;
                foreach (var entry in physical.GetDirectoryContents($"d{i % Directories}"))
                {
                    count += entry.IsDirectory ? 0 : 1;
                }

                Sink.Keep(count);
            }
        });

        var tree = new DirectoryInfoWrapper(new DirectoryInfo(_directory));
        recorder.Measure(FilesScopes.Globbing, 100, () =>
        {
            for (var i = 0; i < 100; i++)
            {
                var matcher = new Matcher();
                matcher.AddInclude("**/*.txt");
                matcher.AddExclude("d0/**");
                Sink.Keep(matcher.Execute(tree).Files.Count());
            }
        });
    }

    public void Dispose()
    {
        _physical.Dispose();
        Directory.Delete(_directory, recursive: true);
    }
}
