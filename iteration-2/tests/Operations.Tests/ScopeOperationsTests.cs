using Xunit;

namespace Operations.Tests;

public class ScopeOperationsTests
{
    private static readonly string[] OperationStates = ["measured", "partial"];
    private static readonly string[] AllStates = ["measured", "partial", "extra", "pending", "unmeasurable"];

    [Fact]
    public void Every_scope_in_the_sources_has_a_row()
    {
        var listed = Iteration.ScopeRows.Where(r => r.Scope.Length > 0).Select(r => r.Scope).ToHashSet();

        Assert.Empty(Iteration.SourceScopes.Except(listed).Order());
    }

    [Fact]
    public void Every_scope_in_the_table_exists_in_the_sources()
    {
        var listed = Iteration.ScopeRows.Where(r => r.Scope.Length > 0).Select(r => r.Scope).ToHashSet();

        Assert.Empty(listed.Except(Iteration.SourceScopes).Order());
    }

    [Fact]
    public void Every_entry_operation_has_a_row()
    {
        var listed = Iteration.ScopeRows.Where(r => r.Operation.Length > 0).Select(r => r.Key).ToHashSet();

        Assert.Empty(Iteration.EntryOperations.Except(listed));
    }

    [Fact]
    public void Rows_name_only_operations_of_the_entry_table()
    {
        var known = Iteration.EntryOperations.ToHashSet();

        Assert.Empty(Iteration.ScopeRows.Where(r => r.Operation.Length > 0).Select(r => r.Key).Except(known));
    }

    [Fact]
    public void States_are_known()
    {
        Assert.Empty(Iteration.ScopeRows.Select(r => r.State).Except(AllStates));
    }

    [Fact]
    public void Scope_and_operation_columns_fit_the_state()
    {
        var invalid = Iteration.ScopeRows.Where(r => r.State switch
        {
            "measured" or "partial" => r.Scope.Length == 0 || r.Operation.Length == 0,
            "extra" => r.Scope.Length == 0 || r.Package.Length > 0 || r.Operation.Length > 0,
            _ => r.Scope.Length > 0 || r.Operation.Length == 0,
        });

        Assert.Empty(invalid);
    }

    [Fact]
    public void All_rows_of_an_operation_share_one_state()
    {
        var split = Iteration.ScopeRows
            .Where(r => r.Operation.Length > 0)
            .GroupBy(r => r.Key)
            .Where(g => g.Select(r => r.State).Distinct().Count() > 1)
            .Select(g => g.Key);

        Assert.Empty(split);
    }

    [Fact]
    public void Pending_and_unmeasurable_operations_have_exactly_one_row()
    {
        var repeated = Iteration.ScopeRows
            .Where(r => !OperationStates.Contains(r.State) && r.Operation.Length > 0)
            .GroupBy(r => r.Key)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        Assert.Empty(repeated);
    }

    [Fact]
    public void No_row_is_repeated()
    {
        var repeated = Iteration.ScopeRows.GroupBy(r => r).Where(g => g.Count() > 1).Select(g => g.Key);

        Assert.Empty(repeated);
    }

    [Fact]
    public void Unmeasurable_operations_belong_to_not_traceable_packages()
    {
        var outside = Iteration.ScopeRows
            .Where(r => r.State == "unmeasurable")
            .Select(r => r.Package)
            .Except(Iteration.NotTraceablePackages);

        Assert.Empty(outside);
    }
}
