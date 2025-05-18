
#nullable enable
namespace ImmutableEditableObjectAdapter.Samples.Uno.Models
{
    [global::System.Diagnostics.DebuggerDisplayAttribute("{DebuggerDisplay(),nq}")]
    public partial class EditablePerson
    {
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
        private global::ImmutableEditableObjectAdapter.Samples.Uno.Models.Person _unedited;
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
        private ulong _changedFlags1;
        public EditablePerson(global::ImmutableEditableObjectAdapter.Samples.Uno.Models.Person originalValue)
        {
            _unedited = originalValue;
        }
        
        [global::System.ComponentModel.DataAnnotations.DisplayAttribute(AutoGenerateField = false)]
        public global::ImmutableEditableObjectAdapter.Samples.Uno.Models.Person Unedited
        {
            get => _unedited;
            set
            {
                ThrowIfIsEditing();
                global::ImmutableEditableObjectAdapter.Samples.Uno.Models.Person oldValue = _unedited;
                bool isNameChanged = !EqualityComparer<string>.Default.Equals(oldValue.Name, value.Name);
                bool isFavouriteColorChanged = !EqualityComparer<string>.Default.Equals(oldValue.FavouriteColor, value.FavouriteColor);
                bool isBirthDayChanged = !EqualityComparer<global::System.DateTimeOffset>.Default.Equals(oldValue.BirthDay, value.BirthDay);
                bool isDeceasedAtChanged = !EqualityComparer<global::System.DateTimeOffset?>.Default.Equals(oldValue.DeceasedAt, value.DeceasedAt);
                if (isNameChanged) OnPropertyChanging(nameof(Name));
                if (isFavouriteColorChanged) OnPropertyChanging(nameof(FavouriteColor));
                if (isBirthDayChanged) OnPropertyChanging(nameof(BirthDay));
                if (isDeceasedAtChanged) OnPropertyChanging(nameof(DeceasedAt));
                SetField(ref _unedited, value);
                if (isNameChanged) OnPropertyChanged(nameof(Name));
                if (isFavouriteColorChanged) OnPropertyChanged(nameof(FavouriteColor));
                if (isBirthDayChanged) OnPropertyChanged(nameof(BirthDay));
                if (isDeceasedAtChanged) OnPropertyChanged(nameof(DeceasedAt));
            }
            
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
        private string _changedName = default(string)!;

        [global::System.ComponentModel.DataAnnotations.DisplayAttribute(AutoGenerateField = false)]
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
        
        /// <inheritdoc cref="global::ImmutableEditableObjectAdapter.Samples.Uno.Models.Person.Name"/>
        public string Name
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
        private string _changedFavouriteColor = default(string)!;

        [global::System.ComponentModel.DataAnnotations.DisplayAttribute(AutoGenerateField = false)]
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
        
        /// <inheritdoc cref="global::ImmutableEditableObjectAdapter.Samples.Uno.Models.Person.FavouriteColor"/>
        [global::System.ComponentModel.DataAnnotations.DisplayAttribute(Name = "Color")]
        public string FavouriteColor
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

        [global::System.ComponentModel.DataAnnotations.DisplayAttribute(AutoGenerateField = false)]
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
        
        /// <inheritdoc cref="global::ImmutableEditableObjectAdapter.Samples.Uno.Models.Person.BirthDay"/>
        public global::System.DateTimeOffset BirthDay
        {
            get => BirthDayPropertyChanged ? _changedBirthDay : Unedited.BirthDay;
            set
            {
                ThrowIfNotEditing();
                BirthDayPropertyChanged |= SetField(ref _changedBirthDay, value);
            }
            
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
        [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
        private global::System.DateTimeOffset? _changedDeceasedAt = default(global::System.DateTimeOffset?)!;

        [global::System.ComponentModel.DataAnnotations.DisplayAttribute(AutoGenerateField = false)]
        public bool DeceasedAtPropertyChanged
        {
            get => (_changedFlags1 & 16ul) != 0ul;
            private set
            {
                bool isChanged = value != ((_changedFlags1 & 16ul) != 0ul);
                if (isChanged) OnPropertyChanging();
                if (value)
                {
                    _changedFlags1 |= 16ul;
                }
                
                else
                {
                    _changedFlags1 &= ~16ul;
                }
                
                if (isChanged) OnPropertyChanged();
            }
            
        }
        
        /// <inheritdoc cref="global::ImmutableEditableObjectAdapter.Samples.Uno.Models.Person.DeceasedAt"/>
        public global::System.DateTimeOffset? DeceasedAt
        {
            get => DeceasedAtPropertyChanged ? _changedDeceasedAt : Unedited.DeceasedAt;
            set
            {
                ThrowIfNotEditing();
                DeceasedAtPropertyChanged |= SetField(ref _changedDeceasedAt, value);
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
            OnEdited(Unedited, Unedited, true);
            DiscardChanges();
        }
        
        public override void EndEdit()
        {
            ThrowIfNotEditing();
            SetEditing(false);
            global::ImmutableEditableObjectAdapter.Samples.Uno.Models.Person unedited = _unedited;
            bool unchanged = ((_changedFlags1 | 0ul) == 0ul);
            if (unchanged)
            {
                OnEdited(unedited, unedited, true);
                return;
            }
            
            global::ImmutableEditableObjectAdapter.Samples.Uno.Models.Person edited = unedited with
            {
                Name = NamePropertyChanged ? _changedName : Unedited.Name,
                FavouriteColor = FavouriteColorPropertyChanged ? _changedFavouriteColor : Unedited.FavouriteColor,
                BirthDay = BirthDayPropertyChanged ? _changedBirthDay : Unedited.BirthDay,
                DeceasedAt = DeceasedAtPropertyChanged ? _changedDeceasedAt : Unedited.DeceasedAt,
            };
            
            OnEdited(unedited, edited, false);
            SetField(ref _unedited, edited, nameof(Unedited));
            DiscardChanges();
        }
        
        private void DiscardChanges()
        {
            bool isNameChanged = NamePropertyChanged;
            NamePropertyChanged = false;
            if (isNameChanged) OnPropertyChanging(nameof(Name));
            _changedName = default(string)!;
            if (isNameChanged) OnPropertyChanged(nameof(Name));
            bool isFavouriteColorChanged = FavouriteColorPropertyChanged;
            FavouriteColorPropertyChanged = false;
            if (isFavouriteColorChanged) OnPropertyChanging(nameof(FavouriteColor));
            _changedFavouriteColor = default(string)!;
            if (isFavouriteColorChanged) OnPropertyChanged(nameof(FavouriteColor));
            bool isBirthDayChanged = BirthDayPropertyChanged;
            BirthDayPropertyChanged = false;
            if (isBirthDayChanged) OnPropertyChanging(nameof(BirthDay));
            _changedBirthDay = default(global::System.DateTimeOffset)!;
            if (isBirthDayChanged) OnPropertyChanged(nameof(BirthDay));
            bool isDeceasedAtChanged = DeceasedAtPropertyChanged;
            DeceasedAtPropertyChanged = false;
            if (isDeceasedAtChanged) OnPropertyChanging(nameof(DeceasedAt));
            _changedDeceasedAt = default(global::System.DateTimeOffset?)!;
            if (isDeceasedAtChanged) OnPropertyChanged(nameof(DeceasedAt));
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
            
            if (DeceasedAtPropertyChanged)
            {
                yield return nameof(DeceasedAt);
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
            
            if (nameof(DeceasedAt).Equals(propertyName, StringComparison.Ordinal))
            {
                return DeceasedAtPropertyChanged;
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
            sb.Append("DeceasedAt = ").Append(DeceasedAt).Append(", ");
            sb.Length -= 2;
            sb.Append(" }");
            return sb.ToString();
        }
        
    }
    
}


#nullable restore
