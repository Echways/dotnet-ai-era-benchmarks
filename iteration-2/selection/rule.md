# Package selection

Fixed before any measurement. Snapshot of nuget.org downloads taken on {snapshot_date}: [`nuget-snapshot.csv`](nuget-snapshot.csv) ({snapshot_rows} packages, ranked by total downloads), the decision for every package down to the 30th eligible one: [`package-decisions.csv`](package-decisions.csv).

## Rule

1. Candidates: packages that nuget.org lists under the owners `dotnetframework`, `aspnet` or `EntityFramework`, which is how Microsoft publishes the libraries of `dotnet/runtime`, `dotnet/aspnetcore` and `dotnet/efcore`. The search service returns at most 3,000 results per owner, ordered by total downloads, which is far deeper than the cut.
2. Ranking: total downloads over all versions as reported by the search service on the snapshot date.
3. Source: the package must be built from a project named like the package that exists in `dotnet/runtime` (`src/libraries/<id>/src`), `dotnet/aspnetcore` (`src/**/src/<id>.csproj`) or `dotnet/efcore` (`src/<id>`) at the latest `v10.0.x` tag. The repository URL in the nuspec is not used: since 10.0 it points to the `dotnet/dotnet` source-build repository for every package.
4. Current: the latest stable version of the package is from the 10.0 line or later. Packages whose last release is an older stand-alone version (for example `System.Linq` 4.3.0) are in-box today, the code on nuget.org is not the code that runs, and the versions studied here do not exist as packages.
5. Eligible: the nupkg has an implementation assembly for a current runtime or `netstandard2.x`. Contract-only packages, analyzers, tools and metapackages are excluded with the reason.
6. Cut: the first 30 eligible packages by rank. A package that fails the feasibility gate stays in the list, marked `not traceable`, with the reason.

Downloads overcount CI restores and transitive pulls; "most used" here means "most restored".

The 30th eligible package is at rank {last_rank}. No `dotnet/efcore` package and only one `dotnet/aspnetcore` package (`Microsoft.Extensions.ObjectPool`) fall inside that cut. EF Core therefore has no treated window in this iteration, and the EF Core rows of section 7 of the design do not apply.

## Selected packages

Methods are the instrumentable methods of the call graph at `{graph_tag}` ([`../callgraph/{graph_tag}`](../callgraph/{graph_tag})).

