using Glassbox.Instrumenter;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Instrumenter.Tests;

public class ScopeInjectorTests
{
    private static readonly CSharpParseOptions Options = new(LanguageVersion.Preview, preprocessorSymbols: ["NET"]);

    private static (string Text, IReadOnlyList<Diagnostic> Errors, Dictionary<string, int> Hits) Inject(string source, params (int Line, int Ordinal, string Id)[] targets)
    {
        var tree = CSharpSyntaxTree.ParseText(source, Options);
        var hits = targets.ToDictionary(t => t.Id, _ => 0);
        var injector = new ScopeInjector(new HashSet<string>(), targets.ToDictionary(t => (t.Line, t.Ordinal), t => t.Id), hits, [], "v0");
        var rewritten = CSharpSyntaxTree.ParseText(injector.Visit(tree.GetRoot())!.ToFullString(), Options);
        var errors = rewritten.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        return (rewritten.ToString(), errors, hits);
    }

    [Fact]
    public void A_block_bodied_method_gets_a_scope_as_its_first_statement()
    {
        var (text, errors, hits) = Inject("class A\n{\n    void Run()\n    {\n        Work();\n    }\n}\n", (3, 0, "M:A.Run"));

        Assert.Empty(errors);
        Assert.Equal(1, hits["M:A.Run"]);
        Assert.Contains("Tracer.Id(\"M:A.Run\")", text);
    }

    [Fact]
    public void An_expression_bodied_method_becomes_a_block_that_returns_the_value()
    {
        var (text, errors, _) = Inject("class A\n{\n    int Run() => 1;\n}\n", (3, 0, "M:A.Run"));

        Assert.Empty(errors);
        Assert.Contains("return 1;", text);
    }

    [Fact]
    public void A_throwing_expression_body_keeps_its_throw()
    {
        var (text, errors, _) = Inject("class A\n{\n    static void Fail() => throw new System.InvalidOperationException();\n}\n", (3, 0, "M:A.Fail"));

        Assert.Empty(errors);
        Assert.Contains("throw new System.InvalidOperationException();", text);
    }

    [Fact]
    public void Preprocessor_branches_inside_an_expression_body_survive()
    {
        var source = "class A\n{\n    static bool Flag =>\n#if NETFRAMEWORK\n        false;\n#else\n        true;\n#endif\n\n    int Other() => 1;\n}\n";

        var (text, errors, _) = Inject(source, (3, 0, "M:A.get_Flag"));

        Assert.Empty(errors);
        Assert.Contains("#endif", text);
        Assert.Contains("return", text);
    }

    [Fact]
    public void A_constructor_and_an_accessor_are_instrumented()
    {
        var source = "class A\n{\n    int _v;\n    A() { _v = 1; }\n    int V { get => _v; set { _v = value; } }\n}\n";

        var (text, errors, hits) = Inject(source, (4, 0, "M:A.#ctor"), (5, 0, "M:A.get_V"), (5, 1, "M:A.set_V"));

        Assert.Empty(errors);
        Assert.All(hits.Values, count => Assert.Equal(1, count));
        Assert.Contains("return _v;", text);
    }

    [Fact]
    public void An_expression_bodied_property_becomes_a_getter_with_a_scope()
    {
        var (text, errors, _) = Inject("class A\n{\n    int _v;\n    int V => _v;\n}\n", (4, 0, "M:A.get_V"));

        Assert.Empty(errors);
        Assert.Contains("get", text);
        Assert.Contains("Tracer.Scope", text);
    }

    [Fact]
    public void Two_declarations_on_one_line_are_told_apart_by_ordinal()
    {
        var source = "class A\n{\n    int _v;\n    int V { get { return _v; } set { _v = value; } }\n}\n";

        var (text, errors, hits) = Inject(source, (4, 1, "M:A.set_V"));

        Assert.Empty(errors);
        Assert.Equal(1, hits["M:A.set_V"]);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(text, "Tracer.Scope"));
        Assert.True(text.IndexOf("Tracer.Scope", StringComparison.Ordinal) > text.IndexOf("set", StringComparison.Ordinal));
    }

    [Fact]
    public void An_iterator_is_left_alone()
    {
        var (text, _, hits) = Inject("class A\n{\n    System.Collections.Generic.IEnumerable<int> Run()\n    {\n        yield return 1;\n    }\n}\n", (3, 0, "M:A.Run"));

        Assert.DoesNotContain("Tracer.Scope", text);
        Assert.Equal(0, hits["M:A.Run"]);
    }

    [Fact]
    public void An_async_method_gets_an_async_scope()
    {
        var (text, errors, _) = Inject("class A\n{\n    async System.Threading.Tasks.Task Run()\n    {\n        await System.Threading.Tasks.Task.Yield();\n    }\n}\n", (3, 0, "M:A.Run"));

        Assert.Empty(errors);
        Assert.Contains("await using var __et = global::EmberTrace.Tracer.ScopeAsync", text);
    }
}
