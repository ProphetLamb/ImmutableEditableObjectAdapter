using System.Diagnostics;
using System.Text;

namespace ImmutableEditableObjectAdapter;

[Generator]
public class ImmutableEditableObjectAdapterGenerator : IIncrementalGenerator
{
    private const string EditableObjectAdapterMetadataName = "ImmutableEditableObjectAdapter`1";

    private const string EditableObjectAdapterDeclaration =
        """
        #nullable enable
        namespace System.ComponentModel
        {
            using global::System;
            using global::System.Runtime.CompilerServices;

            /// <summary>
            /// Provides the old, and new value of the <see cref="EditedEventHandler{TContract}"/>.
            /// </summary>
            /// <typeparam name="TContract">The type of the contract <c>record</c>.</typeparam>
            public sealed class EditedEventArgs<TContract> : EventArgs
            {
                public EditedEventArgs(
                    TContract oldValue,
                    TContract newValue
                )
                {
                    OldValue = oldValue;
                    NewValue = newValue;
                }

                public TContract OldValue { get; }
                public TContract NewValue { get; }
            }

            /// <summary>
            /// Represents the method that will handle the <see cref="ImmutableEditableObjectAdapter{TContract}.Edited"/> event of an <see cref="ImmutableEditableObjectAdapter{TContract}"/> instance.
            /// </summary>
            /// <typeparam name="TContract">The type of the contract <c>record</c>.</typeparam>
            public delegate void EditedEventHandler<TContract>(
                ImmutableEditableObjectAdapter<TContract> sender,
                EditedEventArgs<TContract> args
            ) where TContract : notnull;

            /// <summary>
            /// Derive a <c>sealed partial class</c> to generate a <see cref="IEditableObject"/> from a immutable state <c>record</c> type.
            /// <br/>
            /// Update the immutable state when the <see cref="Edited"/> event indicates the state is replaced.
            /// </summary>
            /// <typeparam name="TContract">The type of the contract <c>record</c>.</typeparam>
            public abstract class ImmutableEditableObjectAdapter<TContract>
                : IEditableObject, INotifyPropertyChanged, INotifyPropertyChanging
                where TContract : notnull
            {
                /// <inheritdoc />
                public event PropertyChangedEventHandler? PropertyChanged;

                /// <inheritdoc />
                public event PropertyChangingEventHandler? PropertyChanging;
                
                /// <summary>
                /// Occurs before <see cref="EndEdit"/> replaces the immutable state <c>record</c>.
                /// </summary>
                public event EditedEventHandler<TContract>? Edited;
                
                /// <inheritdoc />
                public abstract void BeginEdit();

                /// <inheritdoc />
                public abstract void CancelEdit();

                /// <inheritdoc />
                public abstract void EndEdit();
                
                /// <summary>
                /// Enumerate names of all changed properties during edit, and <see cref="Edited"/>.
                /// </summary>
                public abstract IEnumerable<string> ChangedProperties();
                
                /// <summary>
                /// Indicates whether the property with the name name has changed during edit, and <see cref="Edited"/>.
                /// </summary>
                public abstract bool IsPropertyChanged(string propertyName);

                protected virtual void OnPropertyChanging([CallerMemberName] string? propertyName = null)
                {
                    PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
                }

                protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }

                protected virtual void OnEdited(TContract oldValue, TContract newValue)
                {
                    Edited?.Invoke(this, new EditedEventArgs<TContract>(oldValue, newValue));
                }

                protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
                {
                    if (EqualityComparer<T>.Default.Equals(field, value)) return false;
                    OnPropertyChanging(propertyName);
                    field = value;
                    OnPropertyChanged(propertyName);
                    return true;
                }
            }
        }
        #nullable restore
        """;

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static context =>
            context.AddSource($"{nameof(EditableObjectAdapterDeclaration)}.g.cs",
                SourceText.From(EditableObjectAdapterDeclaration, Encoding.UTF8)));
        var adapterObjectInformation = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 } d
                    && d.Modifiers.All(m => !m.IsKind(SyntaxKind.AbstractKeyword))
                    && d.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)),
                static (context, ct) => CollectAdapterObjectInformation(context, ct)
            )
            .Where(x => x is not null);
        context.RegisterSourceOutput(adapterObjectInformation.Collect(),
            static (context, source) => Generate(context, source!));
    }

    private static void Generate(
        SourceProductionContext context,
        ImmutableArray<EditableAdapterObject> objects
    )
    {
        foreach (var o in objects)
        {
            SrcBuilder source = new(2048);
            using (source.NullableEnable())
            {
                var ns = string.IsNullOrEmpty(o.Namespace)
                    ? default(SrcBuilder.SrcBlock?)
                    : source.Decl($"namespace {o.Namespace}");

                source.Stmt("using global::System;")
                    .Stmt("using global::System.ComponentModel;")
                    .NL();

                source.Stmt("[global::System.Diagnostics.DebuggerDisplayAttribute(\"{DebuggerDisplay(),nq}\")]");
                using (source.Decl($"{o.Modifiers} class {o.Name}"))
                {
                    source.Stmt(
                        "[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]");
                    source.Stmt(
                        "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]");
                    source.Stmt($"private {o.ContractTypeName} _unedited;");
                    foreach (var flagStoreIndex in Enumerable.Range(1, o.Properties.Length / 64 + 1))
                    {
                        source.Stmt(
                            "[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]");
                        source.Stmt(
                            "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]");
                        source.Stmt($"private ulong _changedFlags{flagStoreIndex};");
                    }

                    using (source.Decl(
                        $"public {o.Name}({o.ContractTypeName} originalValue)"))
                    {
                        source.Stmt("_unedited = originalValue;");
                    }

                    using (source.Decl($"public {o.ContractTypeName} Unedited"))
                    {
                        source.Stmt("get => _unedited;");
                        using (source.Decl("set"))
                        {
                            source.Stmt("ThrowIfIsEditing();");
                            source.Stmt($"{o.ContractTypeName} oldValue = _unedited;");
                            foreach (var p in o.Properties)
                            {
                                source.Stmt(
                                    $"bool is{p.Name}Changed = !EqualityComparer<{p.TypeName}>.Default.Equals(oldValue.{p.Name}, value.{p.Name});");
                            }

                            foreach (var p in o.Properties)
                            {
                                source.Stmt(
                                    $"if (is{p.Name}Changed) OnPropertyChanging(nameof({p.Name}));");
                            }

                            source.Stmt("if (!SetField(ref _unedited, value)) return;");
                            foreach (var p in o.Properties)
                            {
                                source.Stmt(
                                    $"if (is{p.Name}Changed) OnPropertyChanged(nameof({p.Name}));");
                            }
                        }
                    }

                    foreach (var (p, index) in o.Properties.Select((x, i) => (x, i)))
                    {
                        source.Stmt(
                            "[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]");
                        source.Stmt(
                            "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]");
                        source.Stmt(
                            $"private {p.TypeName}{p.Nullable} _changed{p.Name} = default({p.TypeName}{p.Nullable})!;"
                        );

                        using (source.Decl($"public bool {p.Name}PropertyChanged"))
                        {
                            var flagStoreIndex = index / 64 + 1;
                            var flagIndex = (index + 1) % 64;
                            source.Stmt($"get => (_changedFlags{flagStoreIndex} & {1ul << flagIndex}ul) != 0ul;");
                            using (source.Decl("private set"))
                            {
                                source.Stmt(
                                    $"bool isChanged = value != ((_changedFlags{flagStoreIndex} & {1ul << flagIndex}ul) != 0ul);");
                                source.Stmt("if (isChanged) OnPropertyChanging();");
                                using (source.If("value"))
                                {
                                    source.Stmt($"_changedFlags{flagStoreIndex} |= {1ul << flagIndex}ul;");
                                }

                                using (source.Decl("else"))
                                {
                                    source.Stmt($"_changedFlags{flagStoreIndex} &= ~{1ul << flagIndex}ul;");
                                }

                                source.Stmt("if (isChanged) OnPropertyChanged();");
                            }
                        }

                        source.DocLine("inheritdoc", $"cref=\"{o.ContractTypeName}.{p.Name}\"");
                        using (source.Decl($"{p.Modifiers} {p.TypeName}{p.Nullable} {p.Name}"))
                        {
                            source.Stmt($"get => {p.Name}PropertyChanged ? _changed{p.Name} : Unedited.{p.Name};");
                            using (source.Decl("set"))
                            {
                                source.Stmt("ThrowIfNotEditing();");
                                source.Stmt($"{p.Name}PropertyChanged |= SetField(ref _changed{p.Name}, value);");
                            }
                        }
                    }

                    using (source.Decl("public override void BeginEdit()"))
                    {
                        source.Stmt("ThrowIfIsEditing();");
                        source.Stmt("SetEditing(true);");
                    }

                    using (source.Decl("public override void CancelEdit()"))
                    {
                        source.Stmt("ThrowIfNotEditing();");
                        source.Stmt("SetEditing(false);");
                        foreach (var p in o.Properties)
                        {
                            source
                                .Stmt($"bool is{p.Name}Changed = {p.Name}PropertyChanged;")
                                .Stmt($"{p.Name}PropertyChanged = false;")
                                .Stmt($"if (is{p.Name}Changed) OnPropertyChanging(nameof({p.Name}));")
                                .Stmt($"_changed{p.Name} = default({p.TypeName}{p.Nullable})!;")
                                .Stmt($"if (is{p.Name}Changed) OnPropertyChanged(nameof({p.Name}));");
                        }
                    }

                    using (source.Decl("public override void EndEdit()"))
                    {
                        source.Stmt("ThrowIfNotEditing();");
                        source.Stmt($"{o.ContractTypeName} unedited = _unedited;");
                        source.AppendIndent($"{o.ContractTypeName} edited = unedited with {{").Indent();
                        foreach (var p in o.Properties)
                        {
                            source.Stmt(
                                $"{p.Name} = {p.Name}PropertyChanged ? _changed{p.Name} : Unedited.{p.Name},");
                        }

                        source.Outdent().AppendLine("};");
                        source
                            .Stmt("OnEdited(unedited, edited);")
                            .Stmt("CancelEdit();")
                            .Stmt("SetField(ref _unedited, edited, nameof(Unedited));");
                    }

                    using (source.Decl("public override IEnumerable<string> ChangedProperties()"))
                    {
                        foreach (var p in o.Properties)
                        {
                            using (source.If($"{p.Name}PropertyChanged"))
                            {
                                source.Stmt($"yield return nameof({p.Name});");
                            }
                        }

                        source.Stmt("yield break;");
                    }

                    using (source.Decl("public override bool IsPropertyChanged(string propertyName)"))
                    {
                        foreach (var p in o.Properties)
                        {
                            using (source.If($"nameof({p.Name}).Equals(propertyName, StringComparison.Ordinal)"))
                            {
                                source.Stmt($"return {p.Name}PropertyChanged;");
                            }
                        }

                        source.Stmt("return false;");
                    }

                    using (source.Decl("public bool IsEditing()"))
                    {
                        source.Stmt("return (_changedFlags1 & 1ul) != 0ul;");
                    }

                    using (source.Decl("private void SetEditing(bool value)"))
                    {
                        using (source.If("value"))
                        {
                            source.Stmt($"_changedFlags1 |= {1ul}ul;");
                        }

                        using (source.Decl("else"))
                        {
                            source.Stmt($"_changedFlags1 &= ~{1ul}ul;");
                        }
                    }

                    using (source.Decl("private void ThrowIfIsEditing()"))
                    {
                        using (source.If("IsEditing()"))
                        {
                            source.Stmt(
                                $"throw new global::System.InvalidOperationException(\"{o.Name} is being edited. Cannot begin edit again, or modify 'Unmodified' before EndEdit(), or CancelEdit() is called.\");");
                        }
                    }

                    using (source.Decl("private void ThrowIfNotEditing()"))
                    {
                        using (source.If("!IsEditing()"))
                        {
                            source.Stmt(
                                $"throw new global::System.InvalidOperationException(\"{o.Name} is not being edited. Cannot edit properties, besides 'Unmodified', before BeginEdit() is called.\");");
                        }
                    }

                    using (source.Decl("internal string DebuggerDisplay()"))
                    {
                        source.Stmt("global::System.Text.StringBuilder sb = new global::System.Text.StringBuilder();");
                        source.Stmt($"sb.Append(\"{o.Name} {{ \");");

                        foreach (var p in o.Properties)
                        {
                            source.Stmt($"sb.Append(\"{p.Name} = \").Append({p.Name}).Append(\", \");");
                        }

                        if (o.Properties.Length != 0)
                        {
                            source.Stmt("sb.Length -= 2;");
                        }

                        source.Stmt("sb.Append(\" }\");");

                        source.Stmt("return sb.ToString();");
                    }
                }

                if (ns is { } block)
                {
                    block.Dispose();
                }
            }

            context.AddSource($"{o.Name}.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
        }
    }

    private static EditableAdapterObject? CollectAdapterObjectInformation(SynModel context, CancellationToken ct)
    {
        if (!context.Is<ClassDeclarationSyntax>(out var declaration))
        {
            return null;
        }

        if (declaration.GetDeclaredSymbol(ct) is not INamedTypeSymbol declaredType)
        {
            return null;
        }

        var baseType = declaredType.BaseType;
        while (baseType is not null)
        {
            if (baseType.MetadataName.Equals(EditableObjectAdapterMetadataName, StringComparison.Ordinal))
            {
                break;
            }

            baseType = baseType.BaseType;
        }

        if (baseType is null)
        {
            return null;
        }

        if (declaredType.IsAbstract)
        {
            return null;
        }

        var modifiers = string.Join(" ", declaration.Node.Modifiers.Select(m => m.Text));
        var contractTypeInfo = baseType.TypeArguments[0];
        var contractTypeProperties = contractTypeInfo.GetMembers()
            .OfType<IPropertySymbol>()
            .Select(CollectAdapterPropertyInformation)
            .Where(x => x is not null)
            .ToImmutableArray();
        return new()
        {
            Name = declaredType.Name,
            Modifiers = modifiers,
            Namespace = FullNamespace(contractTypeInfo.ContainingNamespace),
            ContractTypeName = GlobalQualifiedTypeName(contractTypeInfo),
            Properties = contractTypeProperties!
        };
    }

    private static string GlobalQualifiedTypeName(ITypeSymbol type)
    {
        var ns = FullNamespace(type.ContainingNamespace);
        return string.IsNullOrEmpty(ns) ? type.Name : $"global::{ns}.{type.Name}";
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

    private static EditableAdapterProperty? CollectAdapterPropertyInformation(
        IPropertySymbol context
    )
    {
        if (context.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        return new()
        {
            Name = context.Name,
            TypeName = GlobalQualifiedTypeName(context.Type),
            Nullable = context.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "",
            Modifiers = context.DeclaredAccessibility.ToString().Replace("|", "").ToLower()
        };
    }
}

public sealed record EditableAdapterObject
{
    public required string Modifiers { get; init; }
    public required string Name { get; init; }
    public required string? Namespace { get; init; }
    public required ImmutableArray<EditableAdapterProperty> Properties { get; init; }
    public required string ContractTypeName { get; init; }
}

public sealed record EditableAdapterProperty
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public required string Modifiers { get; init; }
    public required string Nullable { get; init; }
}