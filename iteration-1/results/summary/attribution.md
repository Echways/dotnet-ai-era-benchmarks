# Attribution

Classes: `cca` = pull request opened by the Copilot coding agent (GitHub app `copilot-swe-agent`, account id 198982749); `msft-human` = human author with `author_association` MEMBER/OWNER/COLLABORATOR; `community` = any other human; `bot` = any other GitHub app or bot; `no-pr` = commit not associated with a pull request; `pending` = not yet resolved through the API. Flags: `cca_coauthored` = a non-agent PR whose squash commit carries a `copilot-swe-agent` co-author trailer; `copilot_review_coauthored` (app id 175728472) and `copilot_cli_coauthored` (app id 223556219) are reported but not counted as agent work.

Range: v7.0.20..v10.0.14. Commits: 113.

| library | class | commits | PRs | additions | deletions | cca_coauthored | copilot review co-author |
|---|---|---:|---:|---:|---:|---:|---:|
| Caching | msft-human | 18 | 14 | 16265 | 19167 | 0 | 0 |
| Caching | community | 15 | 15 | 1867 | 891 | 0 | 0 |
| DependencyInjection | msft-human | 39 | 35 | 19328 | 20664 | 0 | 0 |
| DependencyInjection | community | 41 | 41 | 9362 | 2400 | 0 | 0 |

`author_association` is a weak proxy for Microsoft employment: private organisation membership is reported as CONTRIBUTOR, and non-employees can be MEMBER of the dotnet organisation. The `microsoft_email` column (commit author e-mail ends with `@microsoft.com`) disagrees with the class for {'msft-human': 17, 'community': 7} commits; treat `msft-human` vs `community` as indicative.

Line statistics source: {'git commit numstat': 113}.

## Hot methods (git log -L)

| method | file | commits | cca | cca_coauthored | msft-human | community | other |
|---|---|---:|---:|---:|---:|---:|---:|
| `CallSiteFactory.CreateCallSite` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs | 1 | 0 | 0 | 0 | 1 | 0 |
| `CallSiteFactory.TryCreateExact` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs | 3 | 0 | 0 | 0 | 3 | 0 |
| `CallSiteFactory.TryCreateOpenGeneric` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs | 2 | 0 | 0 | 0 | 2 | 0 |
| `CallSiteFactory.TryCreateEnumerable` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs | 8 | 0 | 0 | 1 | 7 | 0 |
| `CallSiteFactory.CreateConstructorCallSite` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs | 3 | 0 | 0 | 1 | 2 | 0 |
| `CallSiteFactory.GetCallSite` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs | 1 | 0 | 0 | 0 | 1 | 0 |
| `CallSiteRuntimeResolver.Resolve` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteRuntimeResolver.cs | 0 | 0 | 0 | 0 | 0 | 0 |
| `CallSiteRuntimeResolver.VisitCache` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteRuntimeResolver.cs | 0 | 0 | 0 | 0 | 0 | 0 |
| `ServiceProvider.GetService` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceProvider.cs | 3 | 0 | 0 | 0 | 3 | 0 |
| `ServiceProvider.CreateServiceAccessor` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceProvider.cs | 3 | 0 | 0 | 0 | 3 | 0 |
| `ServiceProvider.CreateScope` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceProvider.cs | 0 | 0 | 0 | 0 | 0 | 0 |
| `ServiceProviderEngineScope.GetService` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/ServiceProviderEngineScope.cs | 1 | 0 | 0 | 0 | 1 | 0 |
| `ServiceProviderEngineScope.CaptureDisposable` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/ServiceProviderEngineScope.cs | 1 | 0 | 0 | 1 | 0 | 0 |
| `ServiceProviderEngineScope.Dispose` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/ServiceProviderEngineScope.cs | 0 | 0 | 0 | 0 | 0 | 0 |
| `DynamicServiceProviderEngine.RealizeService` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/DynamicServiceProviderEngine.cs | 0 | 0 | 0 | 0 | 0 | 0 |
| `CallSiteValidator.ValidateCallSite` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteValidator.cs | 2 | 0 | 0 | 0 | 2 | 0 |
| `MemoryCache.SetEntry` | src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs | 2 | 0 | 0 | 1 | 1 | 0 |
| `MemoryCache.TryGetValue` | src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs | 3 | 0 | 0 | 1 | 2 | 0 |
| `MemoryCache.CreateEntry` | src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs | 2 | 0 | 0 | 1 | 1 | 0 |
| `MemoryCache.Remove` | src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs | 2 | 0 | 0 | 1 | 1 | 0 |
| `MemoryCache.Compact` | src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs | 2 | 0 | 0 | 0 | 2 | 0 |
| `MemoryCache.StartScanForExpiredItemsIfNeeded` | src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs | 0 | 0 | 0 | 0 | 0 | 0 |
| `CacheEntry.Dispose` | src/libraries/Microsoft.Extensions.Caching.Memory/src/CacheEntry.cs | 0 | 0 | 0 | 0 | 0 | 0 |
| `CacheEntry.CheckExpired` | src/libraries/Microsoft.Extensions.Caching.Memory/src/CacheEntry.cs | 0 | 0 | 0 | 0 | 0 | 0 |

## Performance labels and reverts

PRs labelled `tenet-performance` or `regression-from-last-release`: 6.
- DependencyInjection #99383 (community, area-Extensions-DependencyInjection;tenet-performance): Improve perf\allocs of ActivatorUtilities.CreateInstance() (#99383)
- DependencyInjection #91290 (community, area-Extensions-DependencyInjection;tenet-performance): Improve perf of ActivatorUtilities.CreateInstance() (#91290)
- DependencyInjection #91133 (msft-human, area-Extensions-DependencyInjection;os-android;tenet-performance): [DependencyInjection] introduce feature switch to disable S.R.E (#91133)
- DependencyInjection #90119 (community, area-Extensions-DependencyInjection;tenet-performance): For perf, use the new ConstructorInvoker APIs for ActivatorUtilities.CreateFactory (#90119)
- DependencyInjection #89573 (community, area-Extensions-DependencyInjection;tenet-performance): For perf, use the new ConstructorInvoker APIs for ActivatorUtilities.CreateFactory (#89573)
- DependencyInjection #85065 (community, arch-wasm;area-System.Reflection;tenet-performance): Use emit-based invoke for all of WASM including DI (#85065)

Revert commits: 1.
- DependencyInjection #89992 (msft-human): Revert "For perf, use the new ConstructorInvoker APIs for ActivatorUtilities.CreateFactory (#89573)" (#89992)

## Servicing churn in the 12 months after GA

| library | major | GA | window end | complete | days observed | patches | patches touching paths | commits |
|---|---:|---|---|---|---:|---:|---:|---:|
| DependencyInjection | 7 | 2022-11-08 | 2023-11-08 | True | 365 | 12 | 0 | 0 |
| DependencyInjection | 10 | 2025-11-11 | 2026-11-11 | False | 352 | 14 | 0 | 0 |
| Caching | 7 | 2022-11-08 | 2023-11-08 | True | 365 | 12 | 0 | 0 |
| Caching | 10 | 2025-11-11 | 2026-11-11 | False | 352 | 14 | 0 | 0 |

