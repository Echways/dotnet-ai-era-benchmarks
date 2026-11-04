# Package selection

Fixed before any measurement. Snapshot of nuget.org downloads taken on 2026-11-04: [`nuget-snapshot.csv`](nuget-snapshot.csv) (3937 packages, ranked by total downloads), the decision for every package down to the 30th eligible one: [`package-decisions.csv`](package-decisions.csv).

## Rule

1. Candidates: packages that nuget.org lists under the owners `dotnetframework`, `aspnet` or `EntityFramework`, which is how Microsoft publishes the libraries of `dotnet/runtime`, `dotnet/aspnetcore` and `dotnet/efcore`. The search service returns at most 3,000 results per owner, ordered by total downloads, which is far deeper than the cut.
2. Ranking: total downloads over all versions as reported by the search service on the snapshot date.
3. Source: the package must be built from a project named like the package that exists in `dotnet/runtime` (`src/libraries/<id>/src`), `dotnet/aspnetcore` (`src/**/src/<id>.csproj`) or `dotnet/efcore` (`src/<id>`) at the latest `v10.0.x` tag. The repository URL in the nuspec is not used: since 10.0 it points to the `dotnet/dotnet` source-build repository for every package.
4. Current: the latest stable version of the package is from the 10.0 line or later. Packages whose last release is an older stand-alone version (for example `System.Linq` 4.3.0) are in-box today, the code on nuget.org is not the code that runs, and the versions studied here do not exist as packages.
5. Eligible: the nupkg has an implementation assembly for a current runtime or `netstandard2.x`. Contract-only packages, analyzers, tools and metapackages are excluded with the reason.
6. Cut: the first 30 eligible packages by rank. A package that fails the feasibility gate stays in the list, marked `not traceable`, with the reason.

Downloads overcount CI restores and transitive pulls; "most used" here means "most restored".

The 30th eligible package is at rank 76. No `dotnet/efcore` package and only one `dotnet/aspnetcore` package (`Microsoft.Extensions.ObjectPool`) fall inside that cut. EF Core therefore has no treated window in this iteration, and the EF Core rows of section 7 of the design do not apply.

## Selected packages

Methods are the instrumentable methods of the call graph at `v10.0.14` ([`../callgraph/v10.0.14`](../callgraph/v10.0.14)).

| rank | package | version | downloads | repository | operations | methods | note |
|---:|---|---|---:|---|---:|---:|---|
| 1 | Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.14 | 13,699,129,534 | runtime | 4 | 67 |  |
| 2 | Microsoft.Extensions.Logging.Abstractions | 10.0.14 | 13,332,009,960 | runtime | 3 | 63 |  |
| 3 | Microsoft.Extensions.Primitives | 10.0.14 | 12,922,218,745 | runtime | 3 | 68 |  |
| 4 | Microsoft.Extensions.Options | 10.0.14 | 11,805,211,058 | runtime | 3 | 71 |  |
| 5 | System.Diagnostics.DiagnosticSource | 10.0.14 | 10,449,700,379 | runtime | 3 | 196 |  |
| 7 | Microsoft.Extensions.Configuration.Abstractions | 10.0.14 | 10,169,926,376 | runtime | 2 | 7 |  |
| 9 | Microsoft.Extensions.DependencyInjection | 10.0.14 | 8,390,787,614 | runtime | 2 | 210 |  |
| 10 | Microsoft.Extensions.Logging | 10.0.14 | 8,327,705,789 | runtime | 3 | 47 |  |
| 11 | Microsoft.Extensions.Configuration | 10.0.14 | 7,416,325,431 | runtime | 2 | 69 |  |
| 12 | Microsoft.Extensions.Configuration.Binder | 10.0.14 | 7,228,005,674 | runtime | 1 | 41 |  |
| 13 | Microsoft.Extensions.FileProviders.Abstractions | 10.0.14 | 7,000,310,185 | runtime | 1 | 4 |  |
| 15 | System.Text.Json | 10.0.14 | 6,626,815,505 | runtime | 4 | 2204 |  |
| 17 | System.Text.Encodings.Web | 10.0.14 | 6,371,854,263 | runtime | 1 | 74 |  |
| 20 | Microsoft.Extensions.Hosting.Abstractions | 10.0.14 | 5,857,433,654 | runtime | 1 | 2 |  |
| 21 | System.Security.Cryptography.ProtectedData | 10.0.14 | 5,559,625,381 | runtime | 1 | 14 | not traceable: Windows only; the entry operations throw PlatformNotSupportedException on the Linux stand |
| 23 | Microsoft.Bcl.AsyncInterfaces | 10.0.14 | 5,082,250,015 | runtime | 0 |  | not traceable: the net10.0 assembly only forwards to in-box types; the sources compile for netstandard2.0 and net462 only |
| 27 | System.Configuration.ConfigurationManager | 10.0.14 | 4,787,761,218 | runtime | 1 | 874 |  |
| 29 | System.Collections.Immutable | 10.0.14 | 4,371,574,102 | runtime | 3 | 433 |  |
| 32 | Microsoft.Extensions.Caching.Memory | 10.0.14 | 4,159,614,705 | runtime | 2 | 63 |  |
| 33 | Microsoft.Extensions.FileSystemGlobbing | 10.0.14 | 4,143,910,463 | runtime | 1 | 50 |  |
| 39 | Microsoft.Extensions.Caching.Abstractions | 10.0.14 | 4,086,715,195 | runtime | 1 | 17 |  |
| 41 | Microsoft.Extensions.FileProviders.Physical | 10.0.14 | 4,061,398,259 | runtime | 1 | 15 |  |
| 48 | Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.14 | 3,958,250,730 | runtime | 1 | 8 |  |
| 51 | Microsoft.Extensions.DependencyModel | 10.0.14 | 3,840,078,642 | runtime | 1 | 74 |  |
| 52 | Microsoft.Extensions.Configuration.FileExtensions | 10.0.14 | 3,817,614,307 | runtime | 1 | 5 |  |
| 55 | System.Text.Encoding.CodePages | 10.0.14 | 3,757,459,201 | runtime | 1 | 41 |  |
| 62 | System.Reflection.Metadata | 10.0.14 | 3,659,575,239 | runtime | 2 | 728 |  |
| 68 | System.Security.Permissions | 10.0.14 | 3,375,784,471 | runtime | 1 | 10 | not traceable: the public types are code access security stubs; the entry operations do no work |
| 70 | Microsoft.Extensions.Configuration.Json | 10.0.14 | 3,310,006,565 | runtime | 1 | 11 |  |
| 76 | Microsoft.Extensions.ObjectPool | 10.0.14 | 3,076,844,406 | aspnetcore | 1 | 6 |  |

## Excluded before the cut

| rank | package | version | downloads | reason |
|---:|---|---|---:|---|
| 6 | Microsoft.NETCore.Platforms | 7.0.4 | 10,404,913,506 | latest version is not from the current line |
| 8 | System.Runtime.CompilerServices.Unsafe | 6.1.2 | 8,787,674,669 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 14 | System.Threading.Tasks.Extensions | 4.6.3 | 6,997,343,802 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 16 | System.Memory | 4.6.3 | 6,604,088,746 | latest version is not from the current line |
| 18 | System.Buffers | 4.6.1 | 6,239,504,467 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 19 | Microsoft.CSharp | 4.7.0 | 5,873,837,896 | latest version is not from the current line |
| 22 | Microsoft.NETCore.Targets | 5.0.0 | 5,230,087,004 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 24 | System.Security.Cryptography.Cng | 5.0.0 | 4,993,653,450 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 25 | System.Security.Principal.Windows | 5.0.0 | 4,965,697,911 | latest version is not from the current line |
| 26 | System.Runtime | 4.3.1 | 4,949,096,500 | latest version is not from the current line |
| 28 | System.Security.AccessControl | 6.0.1 | 4,505,102,430 | latest version is not from the current line |
| 30 | System.Reflection.TypeExtensions | 4.7.0 | 4,353,709,083 | latest version is not from the current line |
| 31 | System.IO | 4.3.0 | 4,168,014,017 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 34 | System.Text.Encoding | 4.3.0 | 4,143,680,955 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 35 | System.Reflection.Emit | 4.7.0 | 4,138,281,253 | latest version is not from the current line |
| 36 | System.Reflection.Emit.Lightweight | 4.7.0 | 4,102,809,264 | latest version is not from the current line |
| 37 | System.Reflection | 4.3.0 | 4,097,806,393 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 38 | System.Runtime.Extensions | 4.3.1 | 4,096,669,806 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 40 | System.Threading.Tasks | 4.3.0 | 4,070,472,295 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 42 | System.Reflection.Primitives | 4.3.0 | 4,046,345,369 | latest version is not from the current line |
| 43 | System.Resources.ResourceManager | 4.3.0 | 4,030,956,800 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 44 | System.Globalization | 4.3.0 | 4,028,451,591 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 45 | System.Threading | 4.3.0 | 4,004,244,366 | latest version is not from the current line |
| 46 | System.Collections | 4.3.0 | 3,995,075,694 | latest version is not from the current line |
| 47 | System.Diagnostics.Debug | 4.3.0 | 3,975,791,791 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 49 | System.Linq | 4.3.0 | 3,886,355,344 | latest version is not from the current line |
| 50 | NETStandard.Library | 2.0.3 | 3,851,674,352 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 53 | System.Runtime.InteropServices | 4.3.0 | 3,790,015,841 | latest version is not from the current line |
| 54 | Microsoft.Win32.Registry | 5.0.0 | 3,790,006,510 | latest version is not from the current line |
| 56 | System.Linq.Expressions | 4.3.0 | 3,755,836,314 | latest version is not from the current line |
| 57 | System.Reflection.Emit.ILGeneration | 4.7.0 | 3,751,048,813 | latest version is not from the current line |
| 58 | System.Text.RegularExpressions | 4.3.1 | 3,710,300,735 | latest version is not from the current line |
| 59 | System.Runtime.Handles | 4.3.0 | 3,691,648,973 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 60 | System.Net.Http | 4.3.4 | 3,679,999,090 | latest version is not from the current line |
| 61 | System.Reflection.Extensions | 4.3.0 | 3,672,029,469 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 63 | System.IO.FileSystem | 4.3.0 | 3,603,990,777 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 64 | System.IO.FileSystem.Primitives | 4.3.0 | 3,587,258,530 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 65 | System.ObjectModel | 4.3.0 | 3,585,367,447 | latest version is not from the current line |
| 66 | System.ComponentModel.Annotations | 5.0.0 | 3,519,722,978 | latest version is not from the current line |
| 67 | System.Text.Encoding.Extensions | 4.3.0 | 3,383,710,867 | latest version is not from the current line |
| 69 | System.Xml.ReaderWriter | 4.3.1 | 3,334,375,189 | latest version is not from the current line |
| 71 | runtime.native.System | 4.3.1 | 3,288,301,128 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 72 | System.Runtime.InteropServices.RuntimeInformation | 4.3.0 | 3,151,621,085 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 73 | System.Xml.XDocument | 4.3.0 | 3,122,055,422 | latest version is not from the current line |
| 74 | System.Diagnostics.Tools | 4.3.0 | 3,114,259,886 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |
| 75 | System.Security.Cryptography.Algorithms | 4.3.1 | 3,099,020,359 | project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag |

## Entry operations

Written by the author per package from its primary documented usage, fixed here before measurement. This is the one judgment step of the pipeline and is a threat to validity. An entry names `Type.Method` and matches every overload; `Type.#ctor` matches constructors. Each operation is a unit of Stage 1 and of call-chain reachability in Stage 2; the call graph records which operations reach every method.

| package | operation | entries | what it does |
|---|---|---|---|
| Microsoft.Extensions.DependencyInjection.Abstractions | activator.create-instance | `ActivatorUtilities.CreateInstance` | Create an object resolving constructor arguments from a provider |
| Microsoft.Extensions.DependencyInjection.Abstractions | activator.factory | `ActivatorUtilities.CreateFactory` | Compile a factory for a type with extra arguments |
| Microsoft.Extensions.DependencyInjection.Abstractions | collection.add | `ServiceDescriptor.Singleton`, `ServiceDescriptor.Describe`, `ServiceDescriptor.Transient` | Register services in a collection |
| Microsoft.Extensions.DependencyInjection.Abstractions | provider.get-required | `ServiceProviderServiceExtensions.GetRequiredService`, `ServiceProviderServiceExtensions.GetServices` | Resolve services through the extension methods |
| Microsoft.Extensions.Logging.Abstractions | logger-message.define | `LoggerMessage.Define`, `LoggerMessage.DefineScope` | Create cached log delegates |
| Microsoft.Extensions.Logging.Abstractions | logger.extension | `LoggerExtensions.LogInformation`, `LoggerExtensions.LogError`, `LoggerExtensions.BeginScope` | Write through the Log* extension methods |
| Microsoft.Extensions.Logging.Abstractions | logger.null | `NullLogger.Log`, `NullLogger.IsEnabled`, `NullLoggerFactory.CreateLogger` | Disabled logging |
| Microsoft.Extensions.Primitives | string-values | `StringValues.Concat`, `StringValues.ToString`, `StringValues.Equals` | Combine and compare header-like value sets |
| Microsoft.Extensions.Primitives | string-segment | `StringSegment.IndexOf`, `StringSegment.Split`, `StringSegment.Equals`, `StringSegment.Trim` | Slice and search without allocating |
| Microsoft.Extensions.Primitives | change-token | `ChangeToken.OnChange`, `CompositeChangeToken.RegisterChangeCallback` | Subscribe to change notifications |
| Microsoft.Extensions.Options | factory.create | `OptionsFactory.Create` | Create and configure an options instance |
| Microsoft.Extensions.Options | monitor.get | `OptionsMonitor.Get`, `OptionsMonitor.get_CurrentValue`, `OptionsManager.Get` | Read options through the monitor and manager |
| Microsoft.Extensions.Options | registration | `OptionsServiceCollectionExtensions.Configure`, `OptionsBuilder.Configure`, `OptionsBuilder.Validate` | Register option configuration |
| System.Diagnostics.DiagnosticSource | activity | `ActivitySource.StartActivity`, `Activity.Stop`, `Activity.SetTag` | Start, tag and stop an activity |
| System.Diagnostics.DiagnosticSource | metrics | `Counter.Add`, `Histogram.Record`, `Meter.CreateCounter` | Record measurements |
| System.Diagnostics.DiagnosticSource | listener | `DiagnosticListener.Write`, `DiagnosticListener.IsEnabled` | Publish a diagnostic event |
| Microsoft.Extensions.Configuration.Abstractions | path | `ConfigurationPath.Combine`, `ConfigurationPath.GetSectionKey`, `ConfigurationPath.GetParentPath` | Compose configuration paths |
| Microsoft.Extensions.Configuration.Abstractions | extensions | `ConfigurationExtensions.GetConnectionString`, `ConfigurationExtensions.GetRequiredSection`, `ConfigurationExtensions.Exists` | Read sections through the extension methods |
| Microsoft.Extensions.DependencyInjection | build | `ServiceCollectionContainerBuilderExtensions.BuildServiceProvider`, `ServiceProvider.GetService`, `ServiceProvider.CreateScope` | Build a provider and resolve through it |
| Microsoft.Extensions.DependencyInjection | scope | `ServiceProviderEngineScope.GetService`, `ServiceProviderEngineScope.Dispose` | Resolve and dispose in a scope |
| Microsoft.Extensions.Logging | factory | `LoggerFactory.Create`, `LoggerFactory.CreateLogger`, `LoggerFactory.Dispose` | Create a factory and loggers |
| Microsoft.Extensions.Logging | log | `Logger.Log`, `Logger.IsEnabled`, `Logger.BeginScope` | Write a message through the factory logger |
| Microsoft.Extensions.Logging | registration | `LoggingServiceCollectionExtensions.AddLogging`, `LoggingBuilderExtensions.SetMinimumLevel` | Register logging in a collection |
| Microsoft.Extensions.Configuration | build | `ConfigurationBuilder.Build`, `ConfigurationManager.GetSection`, `ConfigurationManager.GetChildren` | Build a configuration root |
| Microsoft.Extensions.Configuration | read | `ConfigurationRoot.get_Item`, `ConfigurationRoot.GetSection`, `ConfigurationSection.get_Value`, `ConfigurationRoot.GetChildren` | Read values and sections |
| Microsoft.Extensions.Configuration.Binder | bind | `ConfigurationBinder.Bind`, `ConfigurationBinder.Get`, `ConfigurationBinder.GetValue` | Bind configuration onto objects |
| Microsoft.Extensions.FileProviders.Abstractions | null-provider | `NullFileProvider.GetFileInfo`, `NullFileProvider.GetDirectoryContents`, `NullFileProvider.Watch` | Do-nothing provider |
| System.Text.Json | serialize | `JsonSerializer.Serialize` | Serialize an object to a string |
| System.Text.Json | deserialize | `JsonSerializer.Deserialize` | Deserialize an object from a string |
| System.Text.Json | document | `JsonDocument.Parse`, `JsonElement.GetProperty` | Parse into a read-only DOM |
| System.Text.Json | reader-writer | `Utf8JsonReader.Read`, `Utf8JsonWriter.WriteString`, `Utf8JsonWriter.WriteNumber` | Forward-only reader and writer |
| System.Text.Encodings.Web | encode | `TextEncoder.Encode`, `TextEncoder.EncodeUtf8`, `TextEncoder.FindFirstCharacterToEncode` | Encode strings for output contexts |
| Microsoft.Extensions.Hosting.Abstractions | environment | `HostEnvironmentEnvExtensions.IsDevelopment`, `HostEnvironmentEnvExtensions.IsEnvironment` | Environment checks |
| System.Security.Cryptography.ProtectedData | protect | `ProtectedData.Protect`, `ProtectedData.Unprotect` | Protect and unprotect bytes |
| System.Configuration.ConfigurationManager | app-settings | `ConfigurationManager.get_AppSettings`, `ConfigurationManager.GetSection` | Read application settings |
| System.Collections.Immutable | array | `ImmutableArray.Add`, `ImmutableArray.CreateRange`, `ImmutableArray.CreateBuilder` | Immutable arrays |
| System.Collections.Immutable | dictionary | `ImmutableDictionary.Add`, `ImmutableDictionary.SetItem`, `ImmutableDictionary.TryGetValue` | Immutable dictionaries |
| System.Collections.Immutable | list-set | `ImmutableList.Add`, `ImmutableHashSet.Add`, `ImmutableSortedDictionary.Add` | Immutable lists and sets |
| Microsoft.Extensions.Caching.Memory | get-set | `MemoryCache.TryGetValue`, `MemoryCache.CreateEntry`, `MemoryCache.Remove`, `CacheEntry.Dispose` | Read, create and remove entries |
| Microsoft.Extensions.Caching.Memory | maintenance | `MemoryCache.Clear`, `MemoryCache.Compact`, `MemoryCache.GetCurrentStatistics` | Clear, compact and read statistics |
| Microsoft.Extensions.FileSystemGlobbing | match | `Matcher.Execute`, `Matcher.AddInclude`, `Matcher.AddExclude` | Glob a directory tree |
| Microsoft.Extensions.Caching.Abstractions | extensions | `CacheExtensions.Set`, `CacheExtensions.TryGetValue`, `CacheExtensions.Get` | Extension methods over IMemoryCache |
| Microsoft.Extensions.FileProviders.Physical | provider | `PhysicalFileProvider.GetFileInfo`, `PhysicalFileProvider.GetDirectoryContents` | Read the file system through the provider |
| Microsoft.Extensions.Options.ConfigurationExtensions | configure | `OptionsConfigurationServiceCollectionExtensions.Configure`, `NamedConfigureFromConfigurationOptions.#ctor`, `ConfigurationChangeTokenSource.GetChangeToken` | Bind options to configuration |
| Microsoft.Extensions.DependencyModel | context | `DependencyContextJsonReader.Read`, `DependencyContextWriter.Write` | Read and write .deps.json |
| Microsoft.Extensions.Configuration.FileExtensions | load | `FileConfigurationProvider.Load`, `FileConfigurationExtensions.SetBasePath` | Load file configuration |
| System.Text.Encoding.CodePages | encoding | `CodePagesEncodingProvider.GetEncoding` | Resolve a legacy code page encoding |
| System.Reflection.Metadata | reader | `PEReaderExtensions.GetMetadataReader`, `MetadataReader.GetTypeDefinition`, `MetadataReader.GetString`, `BlobReader.ReadCompressedInteger` | Read assembly metadata |
| System.Reflection.Metadata | builder | `MetadataBuilder.AddTypeDefinition`, `BlobBuilder.WriteCompressedInteger` | Emit assembly metadata |
| System.Security.Permissions | principal | `PrincipalPermission.Demand`, `PrincipalPermission.IsUnrestricted` | Code access security stubs |
| Microsoft.Extensions.Configuration.Json | load | `JsonConfigurationProvider.Load`, `JsonConfigurationFileParser.Parse` | Load a JSON configuration file |
| Microsoft.Extensions.ObjectPool | pool | `DefaultObjectPool.Get`, `DefaultObjectPool.Return`, `DefaultObjectPoolProvider.Create` | Rent and return pooled objects |
