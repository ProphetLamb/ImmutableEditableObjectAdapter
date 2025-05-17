using System.Globalization;
using System.Text;

namespace ImmutableEditableObjectAdapter;

[Generator]
public class ImmutableEditableObjectAdapterGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var globalOptions = context.AnalyzerConfigOptionsProvider.Select((config, ct) =>
            config.GlobalOptions.Keys.ToDictionary(
                x => x,
                x => config.GlobalOptions.TryGetValue(x, out var value) ? value : null
            )
        );

        context.RegisterPostInitializationOutput(PostInitializationSource.AddSources);
        var adapterObjectInformation = context
            .SyntaxProvider.CreateSyntaxProvider(MaybeAdapterObject, CollectAdapterObjectInformation)
            .Where(x => x is not null);
        context.RegisterSourceOutput(adapterObjectInformation.Combine(globalOptions).Collect(), Generate!);
    }

    private static bool MaybeAdapterObject(SyntaxNode node, CancellationToken ct)
    {
        return node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 } d
            && d.Modifiers.All(m => !m.IsKind(SyntaxKind.AbstractKeyword))
            && d.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }

    private static void Generate(
        SourceProductionContext context,
        ImmutableArray<(EditableAdapterObjectContext, Dictionary<string, string?>)> items
    )
    {
        SrcBuilder source = new(2048);
        foreach (var (o, globalOptions) in items)
        {
            o.GenerateEditableObjectAdapter(source.Clear());
            context.AddSource($"{o.Declaration.Name}.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
            if (o.GenerateEditableObjectExtensions(source.Clear()))
            {
                context.AddSource(
                    $"{o.EditableObjectExtensionsName}.g.cs",
                    SourceText.From(source.ToString(), Encoding.UTF8)
                );
            }

            if (o.GenerateEditableObjectValueConverter(source.Clear()))
            {
                context.AddSource(
                    $"{o.EditableObjectValueConverterName}.g.cs",
                    SourceText.From(source.ToString(), Encoding.UTF8)
                );
            }
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

        var immutableEditableValueConverterType =
            CollectImmutableEditableValueConverterType(context, declarationSyntax);
        var contractTypeInfo = baseType.TypeArguments[0];
        var contractTypeProperties = contractTypeInfo
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Select(CollectAdapterPropertyInformation)
            .Where(x => x is not null)
            .ToImmutableArray();
        return new(
            Declaration: GetDeclaration(declarationSymbol),
            ContractTypeName: contractTypeInfo.GlobalQualifiedTypeName(),
            Properties: contractTypeProperties!,
            ImmutableEditableValueConverterType: immutableEditableValueConverterType
        );
    }

    private static TypeDeclaration? CollectImmutableEditableValueConverterType(
        GeneratorSyntaxContext context,
        ClassDeclarationSyntax declarationSyntax
    )
    {
        var immutableEditableValueConverterType = declarationSyntax
            .AttributeLists.SelectMany(x => x.Attributes)
            .Select(x => (Symbol: context.SemanticModel.GetTypeInfo(x).Type, Syntax: x))
            .Where(x => PostInitializationSource.ImmutableEditableValueConverterAttributeMetadataName.Equals(
                    x.Symbol?.MetadataName,
                    StringComparison.Ordinal
                )
            )
            .Select(x => x.Syntax.ArgumentList?.Arguments.FirstOrDefault())
            .Where(x => x is not null)
            .Select(x => x!.Expression as TypeOfExpressionSyntax)
            .Where(x => x is not null)
            .Select(x => context.SemanticModel.GetTypeInfo(x!.Type).Type)
            .Where(x => x is not null)
            .Select(x => GetDeclaration(x!))
            .FirstOrDefault();
        return immutableEditableValueConverterType;
    }

    private static TypeDeclaration GetDeclaration(ITypeSymbol typeSymbol)
    {
        return new(
            Name: typeSymbol.Name,
            QualifiedName: typeSymbol.GlobalQualifiedTypeName(),
            Namespace: typeSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : typeSymbol.ContainingNamespace.FullNamespaceName(),
            Accessibility: typeSymbol.DeclaredAccessibility switch
            {
                Accessibility.Private => "private",
                Accessibility.Internal => "internal",
                Accessibility.Protected => "protected",
                Accessibility.ProtectedAndInternal => "protected internal",
                Accessibility.Public => "public",
                _ => ""
            }
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
