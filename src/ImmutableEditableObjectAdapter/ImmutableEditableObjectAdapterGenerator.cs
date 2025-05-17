using System.Globalization;
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
            /// Provides the old, and new value of the <see cref="EditedEventHandler{TContract}"/>, and indicates whether the value has changed.
            /// </summary>
            /// <typeparam name="TContract">The type of the contract <c>record</c>.</typeparam>
            public sealed class EditedEventArgs<TContract> : EventArgs
            {
                public EditedEventArgs(TContract oldValue, TContract newValue, bool cancelledOrUnchanged)
                {
                    OldValue = oldValue;
                    NewValue = newValue;
                    CancelledOrUnchanged = cancelledOrUnchanged;
                }

                public TContract OldValue { get; }
                public TContract NewValue { get; }
                public bool CancelledOrUnchanged { get; }
            }

            /// <summary>
            /// Represents the method that will handle the <see cref="ImmutableEditableObjectAdapter{TContract}.Edited"/> event of an <see cref="ImmutableEditableObjectAdapter{TContract}"/> instance.
            /// </summary>
            /// <typeparam name="TContract">The type of the contract <c>record</c>.</typeparam>
            public delegate void EditedEventHandler<TContract>(
                ImmutableEditableObjectAdapter<TContract> sender,
                EditedEventArgs<TContract> args
            )
                where TContract : notnull;

            /// <summary>
            /// Non-generic interface implemented by <see cref="ImmutableEditableObjectAdapter{TContract}"/>.
            /// </summary>
            public interface IImmutableEditableObjectAdapter : IEditableObject, INotifyPropertyChanged, INotifyPropertyChanging
            {
                /// <summary>
                /// Occurs once, before <see cref="IEditableObject.EndEdit"/> replaces the immutable state <c>record</c>, or <see cref="IEditableObject.CancelEdit"/> discards changes.
                /// <br/>
                /// sender is <cref see="ImmutableEditableObjectAdapter{TContract}"/>
                /// <br/>
                /// event args is <cref see="EditedEventArgs{TContract}"/>
                /// </summary>
                void RegisterOnce(EventHandler callback);
            }

            /// <summary>
            /// Derive a <c>sealed partial class</c> to generate a <see cref="IEditableObject"/> from a immutable state <c>record</c> type.
            /// <br/>
            /// Update the immutable state when the <see cref="Edited"/> event indicates the state is replaced.
            /// </summary>
            /// <typeparam name="TContract">The type of the contract <c>record</c>.</typeparam>
            public abstract class ImmutableEditableObjectAdapter<TContract> : IImmutableEditableObjectAdapter
                where TContract : notnull
            {
                private Queue<EventHandler>? _registerOnceCallbacks;

                /// <inheritdoc />
                public event PropertyChangedEventHandler? PropertyChanged;

                /// <inheritdoc />
                public event PropertyChangingEventHandler? PropertyChanging;

                /// <summary>
                /// Occurs before <see cref="EndEdit"/> replaces the immutable state <c>record</c>, or <see cref="CancelEdit"/> discards changes.
                /// </summary>
                public event EditedEventHandler<TContract>? Edited;

                /// <inheritdoc />
                void IImmutableEditableObjectAdapter.RegisterOnce(EventHandler callback)
                {
                    Queue<EventHandler> registerOnceCallbacks = _registerOnceCallbacks ?? new Queue<EventHandler>();
                    _registerOnceCallbacks = registerOnceCallbacks;
                    registerOnceCallbacks.Enqueue(callback);
                }

                /// <inheritdoc />
                public abstract void BeginEdit();

                /// <inheritdoc />
                public abstract void CancelEdit();

                /// <inheritdoc />
                public abstract void EndEdit();

                /// <summary>
                /// Enumerate names of all changed properties during edit.
                /// </summary>
                public abstract IEnumerable<string> ChangedProperties();

                /// <summary>
                /// Indicates whether the property with the name name has changed during edit.
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

                protected virtual void OnEdited(TContract oldValue, TContract newValue, bool cancelledOrUnchanged)
                {
                    EditedEventArgs<TContract> args = new EditedEventArgs<TContract>(oldValue, newValue, cancelledOrUnchanged)
                    Edited?.Invoke(this, args);
                    Queue<EventHandler>? registerOnceCallbacks = _registerOnceCallbacks;
                    if (registerOnceCallbacks is not null)
                    {
                        while (registerOnceCallbacks.Count != 0)
                        {
                            EventHandler callback = registerOnceCallbacks.Dequeue();
                            callback(this, args);
                        }
                    }
                }

                protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
                {
                    if (EqualityComparer<T>.Default.Equals(field, value))
                    {
                        return false;
                    }
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
                var ns = string.IsNullOrEmpty(o.Declaration.Namespace)
                    ? default(SrcBuilder.SrcBlock?)
                    : source.Decl($"namespace {o.Declaration.Namespace}");

                source.Stmt("using global::System;")
                    .Stmt("using global::System.ComponentModel;")
                    .NL();

                source.Stmt("[global::System.Diagnostics.DebuggerDisplayAttribute(\"{DebuggerDisplay(),nq}\")]");
                using (source.Decl($"{o.Declaration.Modifiers} class {o.Declaration.Name}"))
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
                        $"public {o.Declaration.Name}({o.ContractTypeName} originalValue)"))
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

                            source.Stmt("SetField(ref _unedited, value);");
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
                            $"private {p.TypeName} _changed{p.Name} = default({p.TypeName})!;"
                        ).NL();

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
                        using (source.Decl($"{p.Modifiers} {p.TypeName} {p.Name}"))
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
                        source
                            .Stmt("ThrowIfNotEditing();")
                            .Stmt("SetEditing(false);")
                            .Stmt("OnEdited(Unedited, Unedited, true);")
                            .Stmt("DiscardChanges();");
                    }

                    using (source.Decl("public override void EndEdit()"))
                    {
                        source.Stmt("ThrowIfNotEditing();");
                        source.Stmt("SetEditing(false);");
                        source.Stmt($"{o.ContractTypeName} unedited = _unedited;");
                        source.AppendIndent("bool unchanged = ((");
                        foreach (var flagSetIndex in Enumerable.Range(1, (o.Properties.Length / 64) + 1))
                        {
                            source.Append($"_changedFlags{flagSetIndex} | ");
                        }

                        source.Append("0ul) == 0ul);").NL();
                        using (source.If("unchanged"))
                        {
                            source.Stmt("OnEdited(unedited, unedited, true);");
                            source.Stmt("return;");
                        }
                        source.AppendIndent($"{o.ContractTypeName} edited = unedited with {{").Indent().NL();
                        foreach (var p in o.Properties)
                        {
                            source.Stmt(
                                $"{p.Name} = {p.Name}PropertyChanged ? _changed{p.Name} : Unedited.{p.Name},");
                        }

                        source.Outdent().AppendLine("};");
                        source
                            .Stmt("OnEdited(unedited, edited, false);")
                            .Stmt("DiscardChanges();")
                            .Stmt("SetField(ref _unedited, edited, nameof(Unedited));");
                    }

                    using (source.Decl("private void DiscardChanges()"))
                    {
                        foreach (var p in o.Properties)
                        {
                            source
                                .Stmt($"bool is{p.Name}Changed = {p.Name}PropertyChanged;")
                                .Stmt($"{p.Name}PropertyChanged = false;")
                                .Stmt($"if (is{p.Name}Changed) OnPropertyChanging(nameof({p.Name}));")
                                .Stmt($"_changed{p.Name} = default({p.TypeName})!;")
                                .Stmt($"if (is{p.Name}Changed) OnPropertyChanged(nameof({p.Name}));");
                        }
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
                                $"throw new global::System.InvalidOperationException(\"{o.Declaration.Name} is being edited. Cannot begin edit again, or modify 'Unmodified' before EndEdit(), or CancelEdit() is called.\");");
                        }
                    }

                    using (source.Decl("private void ThrowIfNotEditing()"))
                    {
                        using (source.If("!IsEditing()"))
                        {
                            source.Stmt(
                                $"throw new global::System.InvalidOperationException(\"{o.Declaration.Name} is not being edited. Cannot edit properties, besides 'Unmodified', before BeginEdit() is called.\");");
                        }
                    }

                    using (source.Decl("internal string DebuggerDisplay()"))
                    {
                        source.Stmt("global::System.Text.StringBuilder sb = new global::System.Text.StringBuilder();");
                        source.Stmt($"sb.Append(\"{o.Declaration.Name} {{ \");");

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

            context.AddSource($"{o.Declaration.Name}.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
        }
    }

    private static EditableAdapterObject? CollectAdapterObjectInformation(GeneratorSyntaxContext context, CancellationToken ct)
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

        if (declarationSymbol.IsAbstract)
        {
            return null;
        }

        var contractTypeInfo = baseType.TypeArguments[0];
        var contractTypeProperties = contractTypeInfo.GetMembers()
            .OfType<IPropertySymbol>()
            .Select(CollectAdapterPropertyInformation)
            .Where(x => x is not null)
            .ToImmutableArray();
        return new()
        {
            Declaration = GetDeclaration(declarationSymbol, declarationSyntax),
            ContractTypeName = GlobalQualifiedTypeName(contractTypeInfo),
            Properties = contractTypeProperties!
        };
    }

    private static TypeDeclaration GetDeclaration(ITypeSymbol typeSymbol, ClassDeclarationSyntax typeSyntax)
    {
        return new()
        {
            Name = typeSymbol.Name,
            QualifiedName = GlobalQualifiedTypeName(typeSymbol),
            Namespace = typeSymbol.ContainingNamespace.IsGlobalNamespace ? null : FullNamespace(typeSymbol.ContainingNamespace),
            Modifiers = string.Join(" ", typeSyntax.Modifiers.Select(m => m.Text))
        };
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
            Modifiers = context.DeclaredAccessibility.ToString().Replace("|", "").ToLower(CultureInfo.InvariantCulture)
        };
    }
}

public sealed record TypeDeclaration
{
    public required string Modifiers { get; init; }

    public required string QualifiedName { get; init; }

    public required string Name { get; init; }

    public required string? Namespace { get; init; }
}

public sealed record EditableAdapterObject
{
    public required TypeDeclaration Declaration { get; init; }
    public required ImmutableArray<EditableAdapterProperty> Properties { get; init; }
    public required string ContractTypeName { get; init; }
}

public sealed record EditableAdapterProperty
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public required string Modifiers { get; init; }
}
