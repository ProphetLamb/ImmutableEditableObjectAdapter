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
            if (PostInitializationSource.EditableObjectAdapterMetadataName.Equals(
                    baseType.MetadataName,
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
            ContractTypeName: contractTypeInfo.GlobalQualifiedTypeName(),
            Properties: contractTypeProperties!
        );
    }

    private static TypeDeclaration GetDeclaration(ITypeSymbol typeSymbol, ClassDeclarationSyntax typeSyntax)
    {
        return new(
            Name: typeSymbol.Name,
            QualifiedName: typeSymbol.GlobalQualifiedTypeName(),
            Namespace: typeSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : typeSymbol.ContainingNamespace.FullNamespaceName(),
            Modifiers: string.Join(" ", typeSyntax.Modifiers.Select(m => m.Text))
        );
    }

    private static EditableAdapterProperty? CollectAdapterPropertyInformation(IPropertySymbol context)
    {
        if (context.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        if (context.GetMethod is null)
        {
            return null;
        }

        return new(
            Name: context.Name,
            TypeName: context.Type.GlobalQualifiedTypeName(),
            Modifiers: context.DeclaredAccessibility.ToString().Replace("|", "").ToLower(CultureInfo.InvariantCulture)
        );
    }
}
