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
            EditedEventArgs<TContract> args = new EditedEventArgs<TContract>(oldValue, newValue, cancelledOrUnchanged);
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