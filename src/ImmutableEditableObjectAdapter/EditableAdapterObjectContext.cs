namespace ImmutableEditableObjectAdapter;

public sealed record TypeDeclaration(
    string Modifiers,
    string QualifiedName,
    string Name,
    string? Namespace
);

public sealed record EditableAdapterProperty(string Name, string TypeName, string Modifiers);

public sealed record EditableAdapterObjectContext(
    TypeDeclaration Declaration,
    ImmutableArray<EditableAdapterProperty> Properties,
    string ContractTypeName
)
{
    public void GenerateEditableObjectAdapter(SrcBuilder source)
    {
        using (source.NullableEnable())
        {
            var ns = string.IsNullOrEmpty(Declaration.Namespace)
                ? default(SrcBuilder.SrcBlock?)
                : source.Decl($"namespace {Declaration.Namespace}");

            source.Stmt("using global::System;").Stmt("using global::System.ComponentModel;").NL();

            source.Stmt("[global::System.Diagnostics.DebuggerDisplayAttribute(\"{DebuggerDisplay(),nq}\")]");
            using (source.Decl($"{Declaration.Modifiers} class {Declaration.Name}"))
            {
                source.Stmt(
                    "[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]"
                );
                source.Stmt(
                    "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]"
                );
                source.Stmt($"private {ContractTypeName} _unedited;");
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

                using (source.Decl($"public {Declaration.Name}({ContractTypeName} originalValue)"))
                {
                    source.Stmt("_unedited = originalValue;");
                }

                using (source.Decl($"public {ContractTypeName} Unedited"))
                {
                    source.Stmt("get => _unedited;");
                    using (source.Decl("set"))
                    {
                        source.Stmt("ThrowIfIsEditing();");
                        source.Stmt($"{ContractTypeName} oldValue = _unedited;");
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

                    source.DocLine("inheritdoc", $"cref=\"{ContractTypeName}.{p.Name}\"");
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
                    source.Stmt($"{ContractTypeName} unedited = _unedited;");
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

                    source.AppendIndent($"{ContractTypeName} edited = unedited with {{").Indent().NL();
                    foreach (var p in Properties)
                    {
                        source.Stmt($"{p.Name} = {p.Name}PropertyChanged ? _changed{p.Name} : Unedited.{p.Name},");
                    }

                    source.Outdent().AppendLine("};");
                    source
                        .Stmt("OnEdited(unedited, edited, false);")
                        .Stmt("DiscardChanges();")
                        .Stmt("SetField(ref _unedited, edited, nameof(Unedited));");
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
                            $"throw new global::System.InvalidOperationException(\"{Declaration.Name} is being edited. Cannot begin edit again, or modify 'Unmodified' before EndEdit(), or CancelEdit() is called.\");"
                        );
                    }
                }

                using (source.Decl("private void ThrowIfNotEditing()"))
                {
                    using (source.If("!IsEditing()"))
                    {
                        source.Stmt(
                            $"throw new global::System.InvalidOperationException(\"{Declaration.Name} is not being edited. Cannot edit properties, besides 'Unmodified', before BeginEdit() is called.\");"
                        );
                    }
                }

                using (source.Decl("internal string DebuggerDisplay()"))
                {
                    source.Stmt("global::System.Text.StringBuilder sb = new global::System.Text.StringBuilder();");
                    source.Stmt($"sb.Append(\"{Declaration.Name} {{ \");");

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
    }
}
