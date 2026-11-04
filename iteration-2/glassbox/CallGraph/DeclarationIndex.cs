using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Glassbox.CallGraph;

public sealed class DeclarationIndex
{
    private readonly Dictionary<string, Declaration> _byId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _implementations = new(StringComparer.Ordinal);
    private readonly List<(INamedTypeSymbol Type, SemanticModel Model, SourceUnit Unit, TypeDeclarationSyntax Syntax)> _types = [];

    public IReadOnlyDictionary<string, Declaration> ById => _byId;

    public IEnumerable<(INamedTypeSymbol Type, SemanticModel Model, SourceUnit Unit, TypeDeclarationSyntax Syntax)> Types => _types;

    public IReadOnlySet<string> ImplementationsOf(string id) => _implementations.TryGetValue(id, out var set) ? set : EmptySet;

    private static readonly HashSet<string> EmptySet = [];

    public static DeclarationIndex Build(CSharpCompilation compilation, IReadOnlyDictionary<SyntaxTree, SourceUnit> units)
    {
        var index = new DeclarationIndex();
        foreach (var (tree, unit) in units)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                index.Collect(node, model, unit);
            }
        }

        index.AddImplicitConstructors();
        index.MapImplementations();
        return index;
    }

    private void Collect(SyntaxNode node, SemanticModel model, SourceUnit unit)
    {
        switch (node)
        {
            case TypeDeclarationSyntax type when model.GetDeclaredSymbol(type) is INamedTypeSymbol symbol:
                _types.Add((symbol, model, unit, type));
                break;
            case BaseMethodDeclarationSyntax method when model.GetDeclaredSymbol(method) is IMethodSymbol symbol:
                Add(symbol.PartialImplementationPart ?? symbol, method, model, unit);
                break;
            case AccessorDeclarationSyntax accessor when model.GetDeclaredSymbol(accessor) is IMethodSymbol symbol:
                Add(symbol, accessor, model, unit);
                break;
            case PropertyDeclarationSyntax { ExpressionBody: not null } property when model.GetDeclaredSymbol(property) is IPropertySymbol { GetMethod: { } getter }:
                Add(getter, property.ExpressionBody!, model, unit);
                break;
            case IndexerDeclarationSyntax { ExpressionBody: not null } indexer when model.GetDeclaredSymbol(indexer) is IPropertySymbol { GetMethod: { } getter }:
                Add(getter, indexer.ExpressionBody!, model, unit);
                break;
        }
    }

    private void Add(IMethodSymbol symbol, SyntaxNode node, SemanticModel model, SourceUnit unit)
    {
        if (symbol.IsAbstract || symbol.IsExtern || (!HasBody(node) && (node is not AccessorDeclarationSyntax || symbol.ContainingType.TypeKind == TypeKind.Interface)))
        {
            return;
        }

        var id = Declaration.IdOf(symbol);
        if (!_byId.ContainsKey(id))
        {
            _byId[id] = new Declaration(id, symbol.OriginalDefinition, node, model, unit, Declaration.KindOf(symbol));
        }
    }

    private static bool HasBody(SyntaxNode node) => node switch
    {
        BaseMethodDeclarationSyntax method => method.Body is not null || method.ExpressionBody is not null,
        AccessorDeclarationSyntax accessor => accessor.Body is not null || accessor.ExpressionBody is not null,
        _ => true,
    };

    private void AddImplicitConstructors()
    {
        foreach (var (type, _, unit, _) in _types)
        {
            foreach (var constructor in type.OriginalDefinition.Constructors.Where(c => c.IsImplicitlyDeclared))
            {
                var id = Declaration.IdOf(constructor);
                if (!_byId.ContainsKey(id))
                {
                    _byId[id] = new Declaration(id, constructor, null, null, unit, "ctor");
                }
            }
        }
    }

    private void MapImplementations()
    {
        foreach (var declaration in _byId.Values.ToArray())
        {
            for (var overridden = declaration.Symbol.OverriddenMethod; overridden is not null; overridden = overridden.OverriddenMethod)
            {
                Link(Declaration.IdOf(overridden), declaration.Id);
            }

            foreach (var explicitImplementation in declaration.Symbol.ExplicitInterfaceImplementations)
            {
                Link(Declaration.IdOf(explicitImplementation), declaration.Id);
            }
        }

        foreach (var (type, _, _, _) in _types)
        {
            foreach (var contract in type.AllInterfaces)
            {
                foreach (var member in contract.GetMembers().OfType<IMethodSymbol>())
                {
                    if (type.FindImplementationForInterfaceMember(member) is IMethodSymbol implementation)
                    {
                        var implementationId = Declaration.IdOf(implementation);
                        if (_byId.ContainsKey(implementationId))
                        {
                            Link(Declaration.IdOf(member), implementationId);
                        }
                    }
                }
            }
        }
    }

    private void Link(string baseId, string implementationId)
    {
        if (!_implementations.TryGetValue(baseId, out var set))
        {
            _implementations[baseId] = set = new HashSet<string>(StringComparer.Ordinal);
        }

        set.Add(implementationId);
    }
}
