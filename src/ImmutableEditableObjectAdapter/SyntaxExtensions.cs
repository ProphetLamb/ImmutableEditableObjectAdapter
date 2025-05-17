namespace ImmutableEditableObjectAdapter;

public static class SyntaxExtensions
{
    public static string GlobalQualifiedTypeName(this ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    public static string FullNamespaceName(this INamespaceSymbol ns)
    {
        List<string> nsList = [];
        while (ns is not null && !ns.IsGlobalNamespace)
        {
            nsList.Add(ns.Name);
            ns = ns.ContainingNamespace;
        }

        nsList.Reverse();
        return string.Join(".", nsList);
    }

    /// <summary>Determines the trace from the node to the <see cref="NamespaceDeclarationSyntax"/>, by traversing the parents of the node.</summary>
    /// <param name="node">The node</param>
    /// <typeparam name="T">The type of parents allowed in the trace. Throws if the type of any parent doesnt match.</typeparam>
    /// <exception cref="InvalidOperationException">The type of a node is not assignable to <typeparamref name="T"/>.</exception>
    public static (NamespaceDeclarationSyntax, ImmutableArray<T>) GetHierarchy<T>(this CSharpSyntaxNode node)
        where T : MemberDeclarationSyntax
    {
        var nesting = ImmutableArray.CreateBuilder<T>(16);
        SyntaxNode? p = node;
        while ((p = p?.Parent) is not null)
        {
            switch (p)
            {
                case T member:
                    nesting.Add(member);
                    break;
                case NamespaceDeclarationSyntax ns:
                    return (ns, nesting.ToImmutable());
                default:
                    throw new InvalidOperationException($"{p.GetType().Name} is not allowed in the hierarchy.");
            }
        }

        throw new InvalidOperationException("No namespace declaration found.");
    }

    /// <summary>Returns the type name of the node inside the model.</summary>
    /// <param name="model">The model</param>
    /// <param name="node">The node in the scope of the model.</param>
    public static string? GetTypeName(this SemanticModel model, SyntaxNode node)
    {
        // Are we a type?
        var typeInfo = model.GetTypeInfo(node);
        if (typeInfo.Type is not null)
        {
            return typeInfo.Type.ToDisplayString();
        }

        var decl = model.GetDeclaredSymbol(node);
        // Are we of a type?
        if (decl?.ContainingType is not null)
        {
            return decl.ContainingType.ToDisplayString();
        }

        // Do we have any symbol at all?
        return decl?.ToDisplayString();
    }

    /// <summary>Traverses all roots inside the <see cref="Compilation"/>.</summary>
    /// <param name="comp">The compilation to filter.</param>
    /// <param name="predicate">The filter for relevant nodes.</param>
    /// <param name="transform">The final transformation to apply to relevant nodes.</param>
    /// <typeparam name="T">The type of elements produces by the transformation.</typeparam>
    /// <returns>All filtered and transformed nodes inside any root inside the <see cref="Compilation"/>.</returns>
    public static IEnumerable<T> CollectSyntax<T>(
        this Compilation comp,
        Func<SyntaxNode, CancellationToken, bool> predicate,
        Func<Compilation, SyntaxNode, CancellationToken, T> transform
    )
    {
        foreach (var tree in comp.SyntaxTrees)
        {
            CancellationToken ct = new();
            if (tree.TryGetRoot(out var root))
            {
                Stack<SyntaxNode> stack = new(64);
                stack.Push(root);

                SyntaxNode node;
                while ((node = stack.Pop()) is not null)
                {
                    foreach (var child in node.ChildNodesAndTokens())
                    {
                        if (child.IsNode)
                        {
                            stack.Push((SyntaxNode)child!);
                        }
                    }

                    if (predicate(node, ct))
                    {
                        yield return transform(comp, node, ct);
                    }

                    if (ct.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }
    }
}
