from eligibility import eligibility, implementation_frameworks, repository_key


def test_repository_key_maps_the_three_source_repositories():
    assert repository_key("https://github.com/dotnet/runtime") == "runtime"
    assert repository_key("https://github.com/dotnet/aspnetcore.git") == "aspnetcore"
    assert repository_key("git@github.com:dotnet/efcore.git") == "efcore"


def test_repository_key_rejects_other_repositories_and_empty_urls():
    assert repository_key("https://github.com/dotnet/corefx") == ""
    assert repository_key("") == ""


def test_implementation_frameworks_reads_lib_and_runtime_specific_assets():
    files = ["lib/net10.0/A.dll", "lib/net10.0/A.xml", "runtimes/win/lib/net8.0/A.dll", "ref/net10.0/A.dll"]
    assert implementation_frameworks(files) == {"net10.0", "net8.0"}


def test_package_with_modern_implementation_is_eligible():
    assert eligibility(["lib/net10.0/A.dll", "lib/netstandard2.0/A.dll"], ["Dependency"]).eligible


def test_package_with_only_reference_assemblies_is_contract_only():
    verdict = eligibility(["ref/net10.0/A.dll"], [])
    assert not verdict.eligible
    assert verdict.reason == "contract only"


def test_package_without_assemblies_is_a_metapackage():
    verdict = eligibility(["A.nuspec", "_rels/.rels"], [])
    assert verdict.reason == "metapackage"


def test_analyzer_package_is_excluded():
    verdict = eligibility(["analyzers/dotnet/cs/A.dll"], [])
    assert verdict.reason == "analyzer or source generator"


def test_package_with_only_framework_implementation_is_excluded():
    verdict = eligibility(["lib/net462/A.dll"], [])
    assert verdict.reason == "no implementation for current runtimes"


def test_tool_package_is_excluded():
    assert eligibility(["tools/net10.0/any/A.dll"], ["DotnetTool"]).reason == "tool or template"
