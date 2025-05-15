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