namespace ImmutableEditableObjectAdapter;

public sealed record TypeDeclaration(
    string Accessibility,
    string QualifiedName,
    string Name,
    string? Namespace
);

public sealed record EditableAdapterProperty(string Name, string TypeName, string Modifiers);

public sealed record EditableAdapterValueConverterContext(
    TypeDeclaration Type,
    EditableAdapterObjectContext AdapterObject
);

public sealed record EditableAdapterObjectContext(
    TypeDeclaration Type,
    ImmutableArray<EditableAdapterProperty> Properties,
    TypeDeclaration ContractType,
    TypeDeclaration? ImmutableEditableValueConverterType
)
{
    public void GenerateEditableObjectAdapter(SrcBuilder source)
    {
        using var _ = source.NullableEnable();

        var ns = string.IsNullOrEmpty(Type.Namespace)
            ? default(SrcBuilder.SrcBlock?)
            : source.Decl($"namespace {Type.Namespace}");

        source.Stmt("using global::System;").Stmt("using global::System.ComponentModel;").NL();

        source.Stmt("[global::System.Diagnostics.DebuggerDisplayAttribute(\"{DebuggerDisplay(),nq}\")]");
        using (source.Decl($"{Type.Accessibility} partial class {Type.Name}"))
        {
            source.Stmt(
                "[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]"
            );
            source.Stmt(
                "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]"
            );
            source.Stmt($"private {ContractType.QualifiedName} _unedited;");
            foreach (var flagStoreIndex in Enumerable.Range(1, (Properties.Length / 64) + 1))
            {
                source.Stmt(
                    "[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]"
                );
                source.Stmt(
                    "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]"
                );
                source.Stmt($"private ulong _changedFlags{flagStoreIndex};");
            }

            using (source.Decl($"public {Type.Name}({ContractType.QualifiedName} originalValue)"))
            {
                source.Stmt("_unedited = originalValue;");
            }

            source.Stmt("[global::System.ComponentModel.DataAnnotations.DisplayAttribute(AutoGenerateField = false)]");
            using (source.Decl($"public {ContractType.QualifiedName} Unedited"))
            {
                source.Stmt("get => _unedited;");
                using (source.Decl("set"))
                {
                    source.Stmt("ThrowIfIsEditing();");
                    source.Stmt($"{ContractType.QualifiedName} oldValue = _unedited;");
                    foreach (var p in Properties)
                    {
                        source.Stmt(
                            $"bool is{p.Name}Changed = !EqualityComparer<{p.TypeName}>.Default.Equals(oldValue.{p.Name}, value.{p.Name});"
                        );
                    }

                    foreach (var p in Properties)
                    {
                        source.Stmt($"if (is{p.Name}Changed) OnPropertyChanging(nameof({p.Name}));");
                    }

                    source.Stmt("SetField(ref _unedited, value);");
                    foreach (var p in Properties)
                    {
                        source.Stmt($"if (is{p.Name}Changed) OnPropertyChanged(nameof({p.Name}));");
                    }
                }
            }

            foreach (var (p, index) in Properties.Select((x, i) => (x, i)))
            {
                source.Stmt(
                    "[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]"
                );
                source.Stmt(
                    "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]"
                );
                source.Stmt($"private {p.TypeName} _changed{p.Name} = default({p.TypeName})!;").NL();

                source.Stmt("[global::System.ComponentModel.DataAnnotations.DisplayAttribute(AutoGenerateField = false)]");
                using (source.Decl($"public bool {p.Name}PropertyChanged"))
                {
                    var flagStoreIndex = (index / 64) + 1;
                    var flagIndex = (index + 1) % 64;
                    source.Stmt($"get => (_changedFlags{flagStoreIndex} & {1ul << flagIndex}ul) != 0ul;");
                    using (source.Decl("private set"))
                    {
                        source.Stmt(
                            $"bool isChanged = value != ((_changedFlags{flagStoreIndex} & {1ul << flagIndex}ul) != 0ul);"
                        );
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

                source.DocLine("inheritdoc", $"cref=\"{ContractType.QualifiedName}.{p.Name}\"");
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
                source.Stmt($"{ContractType.QualifiedName} unedited = _unedited;");
                source.AppendIndent("bool unchanged = ((");
                foreach (var flagSetIndex in Enumerable.Range(1, (Properties.Length / 64) + 1))
                {
                    source.Append($"_changedFlags{flagSetIndex} | ");
                }

                source.Append("0ul) == 0ul);").NL();
                using (source.If("unchanged"))
                {
                    source.Stmt("OnEdited(unedited, unedited, true);");
                    source.Stmt("return;");
                }

                using (source.Decl($"{ContractType.QualifiedName} edited = unedited with", ";"))
                {
                    foreach (var p in Properties)
                    {
                        source.Stmt($"{p.Name} = {p.Name}PropertyChanged ? _changed{p.Name} : Unedited.{p.Name},");
                    }
                }

                source
                    .Stmt("OnEdited(unedited, edited, false);")
                    .Stmt("SetField(ref _unedited, edited, nameof(Unedited));")
                    .Stmt("DiscardChanges();");
            }

            using (source.Decl("private void DiscardChanges()"))
            {
                foreach (var p in Properties)
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
                foreach (var p in Properties)
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
                foreach (var p in Properties)
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
                        $"throw new global::System.InvalidOperationException(\"{Type.Name} is being edited. Cannot begin edit again, or modify 'Unmodified' before EndEdit(), or CancelEdit() is called.\");"
                    );
                }
            }

            using (source.Decl("private void ThrowIfNotEditing()"))
            {
                using (source.If("!IsEditing()"))
                {
                    source.Stmt(
                        $"throw new global::System.InvalidOperationException(\"{Type.Name} is not being edited. Cannot edit properties, besides 'Unmodified', before BeginEdit() is called.\");"
                    );
                }
            }

            using (source.Decl("internal string DebuggerDisplay()"))
            {
                source.Stmt("global::System.Text.StringBuilder sb = new global::System.Text.StringBuilder();");
                source.Stmt($"sb.Append(\"{Type.Name} {{ \");");

                foreach (var p in Properties)
                {
                    source.Stmt($"sb.Append(\"{p.Name} = \").Append({p.Name}).Append(\", \");");
                }

                if (Properties.Length != 0)
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

    public string EditableObjectExtensionsName => $"{Type.Name}Extensions";

    public bool GenerateEditableObjectExtensions(SrcBuilder source)
    {
        using var _ = source.NullableEnable();
        var ns = string.IsNullOrEmpty(Type.Namespace)
            ? default(SrcBuilder.SrcBlock?)
            : source.Decl($"namespace {Type.Namespace}");
        using (source.Decl($"{Type.Accessibility} static partial class {EditableObjectExtensionsName}"))
        {
            if (ImmutableEditableValueConverterType is null)
            {
                source.Pre("#if HAS_UNO");
            }

            using (source.Decl(
                    $"public static {Type.Name} ToEditable(this {ContractType.QualifiedName} contract, global::System.Windows.Input.ICommand? command = null)"
                ))
            {
                source.Stmt($"{Type.Name} editable = new {Type.Name}(contract);");
                using (source.If("command != null"))
                using (source.Decl("editable.Edited += (_, args) =>", ";"))
                using (source.If("command.CanExecute(args)"))
                {
                    source.Stmt("command.Execute(args);");
                }

                source.Stmt("return editable;");
            }

            if (ImmutableEditableValueConverterType is null)
            {
                source.Pre("#else");
                using (source.Decl($"public static {Type.Name} ToEditable(this {ContractType.QualifiedName} contract)"))
                {
                    source.Stmt($"return new {Type.Name}(contract);");
                }

                source.Pre("#endif");
            }

            using (source.PreIfEnd("HAS_UNO"))
            using (source.Decl(
                    $"public static global::System.Collections.Immutable.IImmutableList<{Type.Name}> ToEditableList(this global::System.Collections.Immutable.IImmutableList<{ContractType.QualifiedName}> contractList, global::System.Windows.Input.ICommand? command = null)"
                ))
            {
                source.Stmt("return contractList.Select(x => ToEditable(x, command)).ToImmutableArray();");
            }

            using (source.PreIfEnd("HAS_UNO"))
            using (source.Decl(
                    $"public static global::Uno.Extensions.Reactive.IFeed<{Type.Name}> ToEditableFeed(this global::Uno.Extensions.Reactive.IFeed<{ContractType.QualifiedName}> feed, global::System.Windows.Input.ICommand? command = null)"
                ))
            {
                source.Stmt("return feed.Select(x => ToEditable(x, command));");
            }

            using (source.PreIfEnd("HAS_UNO"))
            using (source.Decl(
                    $"public static global::Uno.Extensions.Reactive.IListFeed<{Type.Name}> ToEditableListFeed(this global::Uno.Extensions.Reactive.IListFeed<{ContractType.QualifiedName}> feed, global::System.Windows.Input.ICommand? command = null)"
                ))
            {
                source.Stmt("return feed.AsFeed().Select(x => ToEditableList(x, command)).AsListFeed();");
            }
        }

        if (ns is { } block)
        {
            block.Dispose();
        }

        return true;
    }

    public string EditableObjectValueConverterName =>
        ImmutableEditableValueConverterType?.Name ?? $"{Type.Name}ValueConverter";

    public bool GenerateEditableObjectValueConverter(SrcBuilder source)
    {
        if (ImmutableEditableValueConverterType is null)
        {
            return false;
        }

        using var _ = source.NullableEnable();
        var ns = string.IsNullOrEmpty(ImmutableEditableValueConverterType.Namespace)
            ? default(SrcBuilder.SrcBlock?)
            : source.Decl($"namespace {ImmutableEditableValueConverterType.Namespace}");

        using (source.Decl(
                $"{ImmutableEditableValueConverterType.Accessibility} sealed partial class {EditableObjectValueConverterName}"
            ))
        {
            using (source.Decl(
                    "public object? Convert(object? value, global::System.Type targetType, object? parameter, string language)"
                ))
            using (source.Decl("return value switch", ";"))
            {
                source.Stmt("null => null,");
                source.Stmt($"{ContractType.QualifiedName} x => {EditableObjectExtensionsName}.ToEditable(x),");
                using (source.PreIfEnd("HAS_UNO"))
                {
                    source.Stmt(
                        $"global::System.Collections.Immutable.IImmutableList<{ContractType.QualifiedName}> x => {EditableObjectExtensionsName}.ToEditableList(x),"
                    );
                    source.Stmt(
                        $"global::Uno.Extensions.Reactive.IFeed<{ContractType.QualifiedName}> x => {EditableObjectExtensionsName}.ToEditableFeed(x),"
                    );
                    source.Stmt(
                        $"global::Uno.Extensions.Reactive.IFeed<global::System.Collections.Immutable.IImmutableList<{ContractType.QualifiedName}>> x => {EditableObjectExtensionsName}.ToEditableListFeed(x.AsListFeed()).AsFeed(),"
                    );
                    source.Stmt(
                        $"global::Uno.Extensions.Reactive.IListFeed<{ContractType.QualifiedName}> x => {EditableObjectExtensionsName}.ToEditableListFeed(x),"
                    );
                }

                source.Stmt(
                    $"_ => throw new NotSupportedException($\"{{nameof({EditableObjectValueConverterName})}} can only convert {{nameof({ContractType.QualifiedName})}} to {{nameof({Type.QualifiedName})}} types, as well as immutable lists and feeds thereof.\"),"
                );
            }

            using (source.Decl(
                    "public object? ConvertBack(object? value, global::System.Type targetType, object? parameter, string language)"
                ))
            using (source.Decl("return value switch", ";"))
            {
                source.Stmt("null => null,");
                source.Stmt($"{Type.QualifiedName} x => x.Unedited,");
                using (source.PreIfEnd("HAS_UNO"))
                {
                    source.Stmt(
                        $"global::System.Collections.Immutable.IImmutableList<{Type.QualifiedName}> l => (global::System.Collections.Immutable.IImmutableList<{ContractType.QualifiedName}>)l.Select(x => x.Unedited).ToImmutableArray(),"
                    );
                    source.Stmt(
                        $"global::Uno.Extensions.Reactive.IFeed<{Type.QualifiedName}> f => f.Select(x => x.Unedited),"
                    );
                    source.Stmt(
                        $"global::Uno.Extensions.Reactive.IFeed<global::System.Collections.Immutable.IImmutableList<{Type.QualifiedName}>> f => f.Select(global::System.Collections.Immutable.IImmutableList<{ContractType.QualifiedName}> (l) => l.Select(x => x.Unedited).ToImmutableArray()),"
                    );
                    source.Stmt(
                        $"global::Uno.Extensions.Reactive.IListFeed<{Type.QualifiedName}> f => f.AsFeed().Select(global::System.Collections.Immutable.IImmutableList<{ContractType.QualifiedName}> (l) => l.Select(x => x.Unedited).ToImmutableArray()).AsListFeed(),"
                    );
                }

                source.Stmt(
                    $"_ => throw new NotSupportedException($\"{{nameof({EditableObjectValueConverterName})}} can only convert {{nameof({Type.QualifiedName})}} back to {{nameof({ContractType.QualifiedName})}} types, as well as immutable lists and feeds thereof.\"),"
                );
            }
        }

        if (ns is { } block)
        {
            block.Dispose();
        }

        return true;
    }
}
