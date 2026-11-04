using Glassbox.CallGraph;
using Xunit;

namespace CallGraph.Tests;

public class CallGraphBuilderTests
{
    private static Glassbox.CallGraph.CallGraph Build(string source, params string[] entries) =>
        CallGraphBuilder.Build([new SourceUnit("Lib", "A.cs", "A.cs", source)], entries, CallGraphBuilder.PlatformReferences());

    private static string[] Reached(Glassbox.CallGraph.CallGraph graph) =>
        graph.Nodes.Select(n => string.Join('.', n.Id[2..].Split('(')[0].Split('.').TakeLast(2))).ToArray();

    [Fact]
    public void Calls_are_followed_transitively_and_depth_is_recorded()
    {
        var graph = Build("class A { public void Entry() => Middle(); void Middle() => Leaf(); void Leaf() { } void Unused() { } }", "A.Entry");

        Assert.Equal(["A.Entry", "A.Middle", "A.Leaf"], Reached(graph));
        Assert.Equal([0, 1, 2], graph.Nodes.Select(n => n.Depth));
        Assert.True(graph.Nodes[0].Entry);
    }

    [Fact]
    public void A_virtual_call_reaches_every_override_in_the_package()
    {
        var graph = Build(
            "abstract class Base { public abstract void Run(); } class One : Base { public override void Run() { } } class Two : Base { public override void Run() { } } class Host { public void Entry(Base b) => b.Run(); }",
            "Host.Entry");

        Assert.Contains("One.Run", Reached(graph));
        Assert.Contains("Two.Run", Reached(graph));
        Assert.All(graph.Nodes.Where(n => n.Id.Contains("One.Run") || n.Id.Contains("Two.Run")), n => Assert.True(n.Dispatched));
    }

    [Fact]
    public void An_interface_call_reaches_every_implementation_including_explicit_ones()
    {
        var graph = Build(
            "interface IRun { void Run(); } class One : IRun { public void Run() { } } class Two : IRun { void IRun.Run() { } } class Host { public void Entry(IRun r) => r.Run(); }",
            "Host.Entry");

        Assert.Contains("One.Run", Reached(graph));
        Assert.Equal(3, graph.Nodes.Count);
    }

    [Fact]
    public void Property_reads_and_writes_reach_the_matching_accessors()
    {
        var graph = Build(
            "class A { int _v; public int P { get { return Read(); } set { Write(); } } int Read() => _v; void Write() { } public void Entry() { P = 1; } public int Other() => P; }",
            "A.Entry");

        var ids = graph.Nodes.Select(n => n.Id).ToArray();
        Assert.Contains(ids, id => id.Contains("set_P"));
        Assert.DoesNotContain(ids, id => id.Contains("get_P"));
        Assert.Contains(ids, id => id.EndsWith("A.Write"));
    }

    [Fact]
    public void Compound_assignment_reaches_both_accessors()
    {
        var graph = Build("class A { public int P { get; set; } public void Entry() { P += 1; } }", "A.Entry");

        var ids = graph.Nodes.Select(n => n.Id).ToArray();
        Assert.Contains(ids, id => id.Contains("get_P"));
        Assert.Contains(ids, id => id.Contains("set_P"));
    }

    [Fact]
    public void Field_initializers_belong_to_the_constructors()
    {
        var graph = Build("class A { readonly B _b = Make(); public A() { } static B Make() => new B(); } class B { } class Host { public A Entry() => new A(); }", "Host.Entry");

        Assert.Contains("A.Make", Reached(graph));
    }

    [Fact]
    public void Implicit_constructors_are_nodes_when_the_type_has_initializers()
    {
        var graph = Build("class A { readonly int _x = Compute(); static int Compute() => 1; } class Host { public A Entry() => new A(); }", "Host.Entry");

        Assert.Contains("A.Compute", Reached(graph));
        Assert.Contains(graph.Nodes, n => n.Id.Contains("#ctor") && n.Id.Contains("A."));
    }

    [Fact]
    public void Method_groups_and_lambdas_count_as_calls()
    {
        var graph = Build("using System; class A { public void Entry() { Action a = Target; Action b = () => Other(); a(); b(); } void Target() { } void Other() { } }", "A.Entry");

        Assert.Contains("A.Target", Reached(graph));
        Assert.Contains("A.Other", Reached(graph));
    }

    [Fact]
    public void Foreach_reaches_the_enumerator_members_of_package_types()
    {
        var graph = Build(
            "using System.Collections.Generic; class Bag { public Cursor GetEnumerator() => new Cursor(); } struct Cursor { public bool MoveNext() => false; public int Current => 0; } class Host { public void Entry(Bag b) { foreach (var x in b) { } } }",
            "Host.Entry");

        Assert.Contains("Bag.GetEnumerator", Reached(graph));
        Assert.Contains("Cursor.MoveNext", Reached(graph));
    }

    [Fact]
    public void Calls_leaving_the_package_are_boundary_calls_and_not_nodes()
    {
        var graph = Build("class A { public string Entry() => System.Environment.NewLine + string.Join(\",\", new[] { \"a\" }); }", "A.Entry");

        Assert.Single(graph.Nodes);
        Assert.Contains(graph.Boundary, b => b.To.Contains("Join"));
    }

    [Fact]
    public void An_entry_that_matches_nothing_is_reported()
    {
        var graph = Build("class A { public void Entry() { } }", "A.Missing", "A.Entry");

        Assert.Equal(["operation: A.Missing"], graph.UnmatchedEntries);
        Assert.Single(graph.Nodes);
    }

    [Fact]
    public void Overloads_have_distinct_identities()
    {
        var graph = Build("class A { public void Entry() { Run(1); Run(\"x\"); } void Run(int i) { } void Run(string s) { } }", "A.Entry");

        var ids = graph.Nodes.Select(n => n.Id).ToArray();
        Assert.Contains("M:A.Run(System.Int32)", ids);
        Assert.Contains("M:A.Run(System.String)", ids);
    }

    [Fact]
    public void Entries_can_be_given_as_documentation_ids()
    {
        var graph = Build("class A { public void Entry(int i) { Leaf(); } public void Entry(string s) { } void Leaf() { } }", "M:A.Entry(System.Int32)");

        Assert.Equal(2, graph.Nodes.Count);
    }

    [Fact]
    public void Recursion_terminates()
    {
        var graph = Build("class A { public void Entry() => Loop(); void Loop() => Loop(); }", "A.Entry");

        Assert.Equal(2, graph.Nodes.Count);
    }

    [Fact]
    public void Each_node_records_the_operations_that_reach_it()
    {
        var graph = CallGraphBuilder.Build(
            [new SourceUnit("Lib", "A.cs", "A.cs", "class A { public void One() => Shared(); public void Two() => Shared(); void Shared() { } void Private() { } }")],
            [new EntryOperation("first", ["A.One"]), new EntryOperation("second", ["A.Two"])],
            "Lib",
            CallGraphBuilder.PlatformReferences());

        var shared = graph.Nodes.Single(n => n.Id.EndsWith("A.Shared"));
        Assert.Equal(["first", "second"], shared.Operations);
        Assert.Equal(["first"], graph.Nodes.Single(n => n.Id.EndsWith("A.One")).Operations);
        Assert.DoesNotContain(graph.Nodes, n => n.Id.EndsWith("A.Private"));
    }

    [Fact]
    public void Traversal_stops_at_the_package_boundary()
    {
        var graph = CallGraphBuilder.Build(
            [new SourceUnit("Lib", "A.cs", "A.cs", "class A { public void Entry() => new B().Work(); }"), new SourceUnit("Dependency", "B.cs", "B.cs", "class B { public void Work() { } }")],
            [new EntryOperation("only", ["A.Entry"])],
            "Lib",
            CallGraphBuilder.PlatformReferences());

        Assert.DoesNotContain(graph.Nodes, n => n.Id.Contains("B.Work"));
        Assert.Contains(graph.Boundary, b => b.To.Contains("B.Work"));
    }
}
