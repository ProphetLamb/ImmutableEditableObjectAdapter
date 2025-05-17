using System.Globalization;
using System.Text;

namespace ImmutableEditableObjectAdapter;

[Generator]
public class ImmutableEditableObjectAdapterGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(PostInitializationSource.AddSources);
        var adapterObjectInformation = context
            .SyntaxProvider.CreateSyntaxProvider(MaybeAdapterObject, CollectAdapterObjectInformation)
            .Where(x => x is not null);
        context.RegisterSourceOutput(adapterObjectInformation.Collect(), Generate!);
    }

    private static bool MaybeAdapterObject(SyntaxNode node, CancellationToken ct)
    {
        return node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 } d
            && d.Modifiers.All(m => !m.IsKind(SyntaxKind.AbstractKeyword))
            && d.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }

    private static void Generate(SourceProductionContext context, ImmutableArray<EditableAdapterObjectContext> objects)
    {
        SrcBuilder source = new(2048);
        foreach (var o in objects)
        {
            o.GenerateEditableObjectAdapter(source);
            context.AddSource($"{o.Declaration.Name}.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
        }
    }

    private static EditableAdapterObjectContext? CollectAdapterObjectInformation(
        GeneratorSyntaxContext context,
        CancellationToken ct
    )
    {
        if (context.Node is not ClassDeclarationSyntax declarationSyntax)
        {
            return null;
        }

        if (context.SemanticModel.GetDeclaredSymbol(declarationSyntax, ct) is not { } declarationSymbol)
        {
            return null;
        }

        var baseType = declarationSymbol.BaseType;
        while (baseType is not null)
        {
            if (baseType.MetadataName.Equals(
                    PostInitializationSource.EditableObjectAdapterMetadataName,
                    StringComparison.Ordinal
                ))
            {
                break;
            }

            baseType = baseType.BaseType;
        }

        if (baseType is null)
        {
            return null;
        }

        var contractTypeInfo = baseType.TypeArguments[0];
        var contractTypeProperties = contractTypeInfo
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Select(CollectAdapterPropertyInformation)
            .Where(x => x is not null)
            .ToImmutableArray();
        return new(
            Declaration: GetDeclaration(declarationSymbol, declarationSyntax),
            ContractTypeName: GlobalQualifiedTypeName(contractTypeInfo),
            Properties: contractTypeProperties!
        );
    }

    private static TypeDeclaration GetDeclaration(ITypeSymbol typeSymbol, ClassDeclarationSyntax typeSyntax)
    {
        return new(
            Name: typeSymbol.Name,
            QualifiedName: GlobalQualifiedTypeName(typeSymbol),
            Namespace: typeSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : FullNamespace(typeSymbol.ContainingNamespace),
            Modifiers: string.Join(" ", typeSyntax.Modifiers.Select(m => m.Text))
        );
    }

    private static string GlobalQualifiedTypeName(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static string FullNamespace(INamespaceSymbol ns)
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

    private static EditableAdapterProperty? CollectAdapterPropertyInformation(IPropertySymbol context)
    {
        if (context.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        return new(
            Name: context.Name,
            TypeName: GlobalQualifiedTypeName(context.Type),
            Modifiers: context.DeclaredAccessibility.ToString().Replace("|", "").ToLower(CultureInfo.InvariantCulture)
        );
    }
}
