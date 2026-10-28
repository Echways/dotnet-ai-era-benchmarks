using System.Collections;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using EmberTrace.Metadata;
using EmberTrace.Sessions;

namespace Harness.Core;

public static class RunMetadata
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public static JsonObject Build(
        RunOptions options,
        string scenario,
        string traceFile,
        IReadOnlyList<LoadedAssembly> assemblies,
        ITraceMetadataProvider scopeNames,
        IReadOnlyCollection<ScopeRecord> records,
        TraceSession session,
        DateTimeOffset startedUtc)
    {
        var (sha, dirty) = GitRevision();
        return new JsonObject
        {
            ["schema"] = 1,
            ["label"] = options.Label,
            ["scenario"] = scenario,
            ["trace_file"] = Path.GetFileName(traceFile),
            ["pid"] = Environment.ProcessId,
            ["iterations"] = options.Iterations,
            ["warmup"] = options.Warmup,
            ["include_new"] = options.IncludeNew,
            ["started_utc"] = startedUtc.ToString("O"),
            ["finished_utc"] = DateTimeOffset.UtcNow.ToString("O"),
            ["git_sha"] = sha,
            ["git_dirty"] = dirty,
            ["runtime"] = RuntimeNode(),
            ["machine"] = MachineNode(),
            ["dotnet_env"] = DotnetEnvironment(),
            ["assemblies"] = new JsonArray(assemblies.Select(AssemblyNode).ToArray()),
            ["trace"] = new JsonObject
            {
                ["event_count"] = session.EventCount,
                ["dropped_events"] = session.DroppedEvents,
                ["dropped_chunks"] = session.DroppedChunks,
                ["was_overflow"] = session.WasOverflow,
                ["duration_ms"] = session.DurationMs,
            },
            ["scopes"] = new JsonArray(records.Select(r => ScopeNode(r, scopeNames)).ToArray()),
        };
    }

    public static void Write(JsonObject node, string path) =>
        File.WriteAllText(path, node.ToJsonString(Indented) + Environment.NewLine);

    private static JsonNode AssemblyNode(LoadedAssembly assembly) => new JsonObject
    {
        ["name"] = assembly.Name,
        ["version"] = assembly.Version,
        ["informational_version"] = assembly.InformationalVersion,
        ["family"] = assembly.Family,
        ["expected_major"] = assembly.ExpectedMajor,
        ["matches"] = assembly.Matches,
    };

    private static JsonNode ScopeNode(ScopeRecord record, ITraceMetadataProvider names)
    {
        var sorted = record.AllocatedBytes.Order().ToArray();
        var median = sorted.Length == 0 ? 0 : sorted[sorted.Length / 2];
        return new JsonObject
        {
            ["scope_id"] = record.ScopeId,
            ["name"] = names.TryGet(record.ScopeId, out var meta) ? meta.Name : record.ScopeId.ToString(),
            ["category"] = meta.Category,
            ["kind"] = record.Kind.ToString(),
            ["ops_per_batch"] = record.OperationsPerBatch,
            ["batches"] = sorted.Length,
            ["alloc_bytes_median"] = median,
            ["alloc_bytes_min"] = sorted.Length == 0 ? 0 : sorted[0],
            ["alloc_bytes_max"] = sorted.Length == 0 ? 0 : sorted[^1],
            ["alloc_bytes_total"] = sorted.Sum(),
            ["alloc_bytes_per_op_median"] = record.OperationsPerBatch == 0 ? 0 : (double)median / record.OperationsPerBatch,
        };
    }

    private static JsonObject RuntimeNode() => new()
    {
        ["framework_description"] = RuntimeInformation.FrameworkDescription,
        ["runtime_identifier"] = RuntimeInformation.RuntimeIdentifier,
        ["os_description"] = RuntimeInformation.OSDescription,
        ["os_architecture"] = RuntimeInformation.OSArchitecture.ToString(),
        ["process_architecture"] = RuntimeInformation.ProcessArchitecture.ToString(),
        ["environment_version"] = Environment.Version.ToString(),
        ["corelib_informational_version"] = typeof(object).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion,
        ["server_gc"] = GCSettings.IsServerGC,
        ["gc_latency_mode"] = GCSettings.LatencyMode.ToString(),
        ["tiered_compilation"] = AppContext.TryGetSwitch("System.Runtime.TieredCompilation", out var tiered) ? tiered : null,
        ["tiered_pgo"] = AppContext.TryGetSwitch("System.Runtime.TieredPGO", out var pgo) ? pgo : null,
        ["stopwatch_frequency"] = Stopwatch.Frequency,
    };

    private static JsonObject MachineNode() => new()
    {
        ["machine_name"] = Environment.MachineName,
        ["processor_count"] = Environment.ProcessorCount,
        ["cpu_model"] = CpuModel(),
        ["affinity_mask"] = OperatingSystem.IsLinux() || OperatingSystem.IsWindows() ? (long)Process.GetCurrentProcess().ProcessorAffinity : null,
    };

    private static JsonObject DotnetEnvironment()
    {
        var node = new JsonObject();
        foreach (var entry in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().OrderBy(e => (string)e.Key, StringComparer.Ordinal))
        {
            var key = (string)entry.Key;
            if (key.StartsWith("DOTNET_", StringComparison.Ordinal) || key.StartsWith("COMPlus_", StringComparison.Ordinal))
            {
                node[key] = entry.Value?.ToString();
            }
        }

        return node;
    }

    private static string? CpuModel()
    {
        if (OperatingSystem.IsLinux() && File.Exists("/proc/cpuinfo"))
        {
            var line = File.ReadLines("/proc/cpuinfo").FirstOrDefault(l => l.StartsWith("model name", StringComparison.Ordinal));
            return line?[(line.IndexOf(':') + 1)..].Trim();
        }

        return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
    }

    private static (string? Sha, bool? Dirty) GitRevision()
    {
        var root = FindRepositoryRoot(AppContext.BaseDirectory);
        if (root is null)
        {
            return (null, null);
        }

        var sha = RunGit(root, "rev-parse HEAD");
        var status = RunGit(root, "status --porcelain --untracked-files=no");
        return (sha, status is null ? null : status.Length > 0);
    }

    private static string? FindRepositoryRoot(string start)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }
        }

        return null;
    }

    private static string? RunGit(string workingDirectory, string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 ? output : null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }
}
