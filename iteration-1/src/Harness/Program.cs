using Harness.Core;
using Harness.Scenarios;

var registrations = new List<ScenarioRegistration>
{
    new("di", () => new DiScenario()),
    new("config", () => new ConfigScenario()),
    new("options", () => new OptionsScenario()),
    new("logging", () => new LoggingScenario()),
    new("cache", () => new CacheScenario()),
#if HARNESS_EF
    new("ef", () => new EfScenario()),
#else
    new("ef", null, Unavailable: "EF Core of this major does not target net10.0; it is measured in Track B"),
#endif
};
#if HARNESS_NEWONLY
registrations.AddRange(Harness.NewOnly.NewOnlyScenarios.All);
#endif

return HarnessCli.Run(
    args,
    "Track A harness: Microsoft.Extensions.* and EF Core hot paths on the net10.0 runtime",
    registrations,
    ["msext", "ef"],
    options =>
    [
        new FamilyExpectation("msext", ["Microsoft.Extensions."], options.ExpectedMajors["msext"]),
        new FamilyExpectation("ef", ["Microsoft.EntityFrameworkCore", "Microsoft.Data.Sqlite"], options.ExpectedMajors["ef"]),
        new FamilyExpectation("sqlitepclraw", ["SQLitePCLRaw"], null),
    ],
    typeof(Program).Assembly);
