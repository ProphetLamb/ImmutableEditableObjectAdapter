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
        context.RegisterSourceOutput(
            adapterObjectInformation.Combine(globalOptions).Collect(),
            GenerateAdapterObjects!
        );

        var valueConverterInformation = context
            .SyntaxProvider.CreateSyntaxProvider(MaybeValueConverter, CollectValueConverterInformation)
            .Where(x => x is not null);
        context.RegisterSourceOutput(
            valueConverterInformation.Combine(globalOptions).Collect(),
            GenerateValueConverters!
        );
    }

    private static bool MaybeAdapterObject(SyntaxNode node, CancellationToken ct)
    {
        return node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 } d
            && d.Modifiers.All(m => !m.IsKind(SyntaxKind.AbstractKeyword))
            && d.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }

    private static bool MaybeValueConverter(SyntaxNode node, CancellationToken ct)
    {
        return node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 } d
            && d.Modifiers.All(m => !m.IsKind(SyntaxKind.AbstractKeyword))
            && d.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))
            && d.BaseList.Types.Any(x =>
                x.Type is NameSyntax n && n.GetIdentifier().Text.EndsWith("IValueConverter", StringComparison.Ordinal)
            );
    }

    private static void GenerateAdapterObjects(
        SourceProductionContext context,
        ImmutableArray<(EditableAdapterObjectContext, Dictionary<string, string?>)> items
    )
    {
        SrcBuilder source = new(2048);
        foreach (var (o, globalOptions) in items)
        {
            o.GenerateEditableObjectAdapter(source.Clear());
            context.AddSource($"{o.Type.Name}.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
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

    private void GenerateValueConverters(
        SourceProductionContext context,
        ImmutableArray<(EditableAdapterValueConverterContext, Dictionary<string, string?>)> items
    )
    {
        SrcBuilder source = new(2048);
        foreach (var (valueConverter, globalOptions) in items)
        {
            var o = valueConverter.AdapterObject with { ImmutableEditableValueConverterType = valueConverter.Type };
            if (o.GenerateEditableObjectValueConverter(source.Clear()))
            {
                context.AddSource(
                    $"{o.EditableObjectValueConverterName}.g.cs",
                    SourceText.From(source.ToString(), Encoding.UTF8)
                );
            }
        }
    }

    private EditableAdapterValueConverterContext? CollectValueConverterInformation(
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

        if (CollectImmutableEditableValueConverterTypeofType(context, declarationSyntax) is not
            {
            } editableValueConverterType)
        {
            return null;
        }

        if (CollectAdapterObjectInformationWithoutValueConverter(editableValueConverterType) is not { } adapterObject)
        {
            return null;
        }

        return new(Type: GetDeclaration(declarationSymbol), AdapterObject: adapterObject);
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

        if (CollectAdapterObjectInformationWithoutValueConverter(declarationSymbol) is not { } adapterObject)
        {
            return null;
        }

        if (CollectImmutableEditableValueConverterTypeofType(context, declarationSyntax) is { } converterTypeSymbol
            && converterTypeSymbol
                .GetAttributes()
                .All(d => !PostInitializationSource.ImmutableEditableValueConverterAttributeMetadataName.Equals(
                        d.AttributeClass?.MetadataName,
                        StringComparison.Ordinal
                    )
                ))
        {
            return adapterObject with { ImmutableEditableValueConverterType = GetDeclaration(converterTypeSymbol) };
        }

        return adapterObject;
    }

    private static EditableAdapterObjectContext? CollectAdapterObjectInformationWithoutValueConverter(
        ITypeSymbol declarationSymbol
    )
    {
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
            Type: GetDeclaration(declarationSymbol),
            ContractTypeName: contractTypeInfo.GlobalQualifiedTypeName(),
            Properties: contractTypeProperties!,
            ImmutableEditableValueConverterType: null
        );
    }

    private static ITypeSymbol? CollectImmutableEditableValueConverterTypeofType(
        GeneratorSyntaxContext context,
        ClassDeclarationSyntax declarationSyntax
    )
    {
        var attributeTypes = declarationSyntax
            .AttributeLists.SelectMany(x => x.Attributes)
            .Where(x => x
                .Name.GetIdentifier()
                .Text.AsSpan()
                .Contains(
                    PostInitializationSource.ImmutableEditableValueConverterAttributeShortName.AsSpan(),
                    StringComparison.Ordinal
                )
            )
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
            .Where(x => x is not null);
        return attributeTypes.FirstOrDefault();
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
