
#nullable enable
using global::System;
using global::System.ComponentModel;

[global::System.Diagnostics.DebuggerDisplayAttribute("{DebuggerDisplay(),nq}")]
internal sealed partial class EditablePerson
{
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
    [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
    private Person _unedited;
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
    [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
    private ulong _changedFlags1;
    public EditablePerson(Person originalValue)
    {
        _unedited = originalValue;
    }

    public Person Unedited
    {
        get => _unedited;
        set
        {
            ThrowIfIsEditing();
            Person oldValue = _unedited;
            bool isNameChanged = !EqualityComparer<global::System.String>.Default.Equals(oldValue.Name, value.Name);
            bool isFavouriteColorChanged = !EqualityComparer<global::System.String>.Default.Equals(oldValue.FavouriteColor, value.FavouriteColor);
            bool isBirthDayChanged = !EqualityComparer<global::System.DateTimeOffset>.Default.Equals(oldValue.BirthDay, value.BirthDay);
            if (isNameChanged) OnPropertyChanging(nameof(Name));
            if (isFavouriteColorChanged) OnPropertyChanging(nameof(FavouriteColor));
            if (isBirthDayChanged) OnPropertyChanging(nameof(BirthDay));
            if (!SetField(ref _unedited, value)) return;
            if (isNameChanged) OnPropertyChanged(nameof(Name));
            if (isFavouriteColorChanged) OnPropertyChanged(nameof(FavouriteColor));
            if (isBirthDayChanged) OnPropertyChanged(nameof(BirthDay));
        }

    }

    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
    [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
    private global::System.String _changedName = default(global::System.String)!;
    public bool NamePropertyChanged
    {
        get => (_changedFlags1 & 2ul) != 0ul;
        private set
        {
            bool isChanged = value != ((_changedFlags1 & 2ul) != 0ul);
            if (isChanged) OnPropertyChanging();
            if (value)
            {
                _changedFlags1 |= 2ul;
            }

            else
            {
                _changedFlags1 &= ~2ul;
            }

            if (isChanged) OnPropertyChanged();
        }

    }

    /// <inheritdoc cref="Person.Name"/>
    public global::System.String Name
    {
        get => NamePropertyChanged ? _changedName : Unedited.Name;
        set
        {
            ThrowIfNotEditing();
            NamePropertyChanged |= SetField(ref _changedName, value);
        }

    }

    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
    [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
    private global::System.String _changedFavouriteColor = default(global::System.String)!;
    public bool FavouriteColorPropertyChanged
    {
        get => (_changedFlags1 & 4ul) != 0ul;
        private set
        {
            bool isChanged = value != ((_changedFlags1 & 4ul) != 0ul);
            if (isChanged) OnPropertyChanging();
            if (value)
            {
                _changedFlags1 |= 4ul;
            }

            else
            {
                _changedFlags1 &= ~4ul;
            }

            if (isChanged) OnPropertyChanged();
        }

    }

    /// <inheritdoc cref="Person.FavouriteColor"/>
    public global::System.String FavouriteColor
    {
        get => FavouriteColorPropertyChanged ? _changedFavouriteColor : Unedited.FavouriteColor;
        set
        {
            ThrowIfNotEditing();
            FavouriteColorPropertyChanged |= SetField(ref _changedFavouriteColor, value);
        }

    }

    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
    [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
    private global::System.DateTimeOffset _changedBirthDay = default(global::System.DateTimeOffset)!;
    public bool BirthDayPropertyChanged
    {
        get => (_changedFlags1 & 8ul) != 0ul;
        private set
        {
            bool isChanged = value != ((_changedFlags1 & 8ul) != 0ul);
            if (isChanged) OnPropertyChanging();
            if (value)
            {
                _changedFlags1 |= 8ul;
            }

            else
            {
                _changedFlags1 &= ~8ul;
            }

            if (isChanged) OnPropertyChanged();
        }

    }

    /// <inheritdoc cref="Person.BirthDay"/>
    public global::System.DateTimeOffset BirthDay
    {
        get => BirthDayPropertyChanged ? _changedBirthDay : Unedited.BirthDay;
        set
        {
            ThrowIfNotEditing();
            BirthDayPropertyChanged |= SetField(ref _changedBirthDay, value);
        }

    }

    public override void BeginEdit()
    {
        ThrowIfIsEditing();
        SetEditing(true);
    }

    public override void CancelEdit()
    {
        ThrowIfNotEditing();
        SetEditing(false);
        bool isNameChanged = NamePropertyChanged;
        NamePropertyChanged = false;
        if (isNameChanged) OnPropertyChanging(nameof(Name));
        _changedName = default(global::System.String)!;
        if (isNameChanged) OnPropertyChanged(nameof(Name));
        bool isFavouriteColorChanged = FavouriteColorPropertyChanged;
        FavouriteColorPropertyChanged = false;
        if (isFavouriteColorChanged) OnPropertyChanging(nameof(FavouriteColor));
        _changedFavouriteColor = default(global::System.String)!;
        if (isFavouriteColorChanged) OnPropertyChanged(nameof(FavouriteColor));
        bool isBirthDayChanged = BirthDayPropertyChanged;
        BirthDayPropertyChanged = false;
        if (isBirthDayChanged) OnPropertyChanging(nameof(BirthDay));
        _changedBirthDay = default(global::System.DateTimeOffset)!;
        if (isBirthDayChanged) OnPropertyChanged(nameof(BirthDay));
    }

    public override void EndEdit()
    {
        ThrowIfNotEditing();
        Person unedited = _unedited;
        Person edited = unedited with {            Name = NamePropertyChanged ? _changedName : Unedited.Name,
            FavouriteColor = FavouriteColorPropertyChanged ? _changedFavouriteColor : Unedited.FavouriteColor,
            BirthDay = BirthDayPropertyChanged ? _changedBirthDay : Unedited.BirthDay,
        };
        OnEdited(unedited, edited);
        CancelEdit();
        SetField(ref _unedited, edited, nameof(Unedited));
    }

    public override IEnumerable<string> ChangedProperties()
    {
        if (NamePropertyChanged)
        {
            yield return nameof(Name);
        }

        if (FavouriteColorPropertyChanged)
        {
            yield return nameof(FavouriteColor);
        }

        if (BirthDayPropertyChanged)
        {
            yield return nameof(BirthDay);
        }

        yield break;
    }

    public override bool IsPropertyChanged(string propertyName)
    {
        if (nameof(Name).Equals(propertyName, StringComparison.Ordinal))
        {
            return NamePropertyChanged;
        }

        if (nameof(FavouriteColor).Equals(propertyName, StringComparison.Ordinal))
        {
            return FavouriteColorPropertyChanged;
        }

        if (nameof(BirthDay).Equals(propertyName, StringComparison.Ordinal))
        {
            return BirthDayPropertyChanged;
        }

        return false;
    }

    public bool IsEditing()
    {
        return (_changedFlags1 & 1ul) != 0ul;
    }

    private void SetEditing(bool value)
    {
        if (value)
        {
            _changedFlags1 |= 1ul;
        }

        else
        {
            _changedFlags1 &= ~1ul;
        }

    }

    private void ThrowIfIsEditing()
    {
        if (IsEditing())
        {
            throw new global::System.InvalidOperationException("EditablePerson is being edited. Cannot begin edit again, or modify 'Unmodified' before EndEdit(), or CancelEdit() is called.");
        }

    }

    private void ThrowIfNotEditing()
    {
        if (!IsEditing())
        {
            throw new global::System.InvalidOperationException("EditablePerson is not being edited. Cannot edit properties, besides 'Unmodified', before BeginEdit() is called.");
        }

    }

    internal string DebuggerDisplay()
    {
        global::System.Text.StringBuilder sb = new global::System.Text.StringBuilder();
        sb.Append("EditablePerson { ");
        sb.Append("Name = ").Append(Name).Append(", ");
        sb.Append("FavouriteColor = ").Append(FavouriteColor).Append(", ");
        sb.Append("BirthDay = ").Append(BirthDay).Append(", ");
        sb.Length -= 2;
        sb.Append(" }");
        return sb.ToString();
    }

}


#nullable restore
