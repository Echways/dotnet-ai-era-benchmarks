using Harness.Core;
using Harness.Scenarios;
using Harness.TrackB.Scenarios;

var registrations = new List<ScenarioRegistration>
{
    new("stj", () => new JsonScenario()),
    new("channels", () => new ChannelsScenario()),
    new("immutable", () => new ImmutableScenario()),
    new("diagnostics", () => new DiagnosticsScenario()),
    new("text", () => new TextScenario()),
    new("metadata", () => new MetadataScenario()),
    new("aspnetcore", () => new AspNetCoreScenario()),
    new("ef", () => new EfScenario()),
};

return HarnessCli.Run(
    args,
    "Track B harness: shared-framework libraries, measured as library + runtime (net10.0 vs net11.0)",
    registrations,
    ["runtime", "msext", "ef", "aspnetcore"],
    options =>
    [
        new FamilyExpectation("runtime", ["System.Text.Json", "System.Threading.Channels", "System.Collections.Immutable", "System.Diagnostics.DiagnosticSource", "System.Text.Encodings.Web", "System.Text.Encoding.CodePages", "System.Reflection.Metadata", "System.Private.CoreLib"], options.ExpectedMajors["runtime"]),
        new FamilyExpectation("msext", ["Microsoft.Extensions."], options.ExpectedMajors["msext"]),
        new FamilyExpectation("ef", ["Microsoft.EntityFrameworkCore", "Microsoft.Data.Sqlite"], options.ExpectedMajors["ef"]),
        new FamilyExpectation("aspnetcore", ["Microsoft.AspNetCore."], options.ExpectedMajors["aspnetcore"]),
        new FamilyExpectation("sqlitepclraw", ["SQLitePCLRaw"], null),
    ],
    typeof(Program).Assembly);
