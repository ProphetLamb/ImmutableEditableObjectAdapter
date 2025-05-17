#nullable enable
namespace System.ComponentModel
{
    /// <summary>
    /// Provides the old, and new value of the <see cref="EditedEventHandler{TContract}"/>, and indicates whether the value has changed.
    /// </summary>
    /// <typeparam name="TContract">The type of the contract <c>record</c>.</typeparam>
    public sealed class EditedEventArgs<TContract> : global::System.EventArgs
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
    public interface IImmutableEditableObjectAdapter : global::System.ComponentModel.IEditableObject, global::System.ComponentModel.INotifyPropertyChanged, global::System.ComponentModel.INotifyPropertyChanging
    {
        /// <summary>
        /// Occurs once, before <see cref="IEditableObject.EndEdit"/> replaces the immutable state <c>record</c>, or <see cref="IEditableObject.CancelEdit"/> discards changes.
        /// <br/>
        /// sender is <cref see="ImmutableEditableObjectAdapter{TContract}"/>
        /// <br/>
        /// event args is <cref see="EditedEventArgs{TContract}"/>
        /// </summary>
        void RegisterOnce(global::System.EventHandler callback);
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
        private global::System.Collections.Generic.Queue<global::System.EventHandler>? _registerOnceCallbacks;

        /// <inheritdoc />
        public event global::System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        /// <inheritdoc />
        public event global::System.ComponentModel.PropertyChangingEventHandler? PropertyChanging;

        /// <summary>
        /// Occurs before <see cref="EndEdit"/> replaces the immutable state <c>record</c>, or <see cref="CancelEdit"/> discards changes.
        /// </summary>
        public event EditedEventHandler<TContract>? Edited;

        /// <inheritdoc />
        void IImmutableEditableObjectAdapter.RegisterOnce(global::System.EventHandler callback)
        {
            global::System.Collections.Generic.Queue<global::System.EventHandler> registerOnceCallbacks = _registerOnceCallbacks ?? new global::System.Collections.Generic.Queue<global::System.EventHandler>();
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
        public abstract global::System.Collections.Generic.IEnumerable<string> ChangedProperties();

        /// <summary>
        /// Indicates whether the property with the name name has changed during edit.
        /// </summary>
        public abstract bool IsPropertyChanged(string propertyName);

        protected virtual void OnPropertyChanging([global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnEdited(TContract oldValue, TContract newValue, bool cancelledOrUnchanged)
        {
            EditedEventArgs<TContract> args = new EditedEventArgs<TContract>(oldValue, newValue, cancelledOrUnchanged);
            Edited?.Invoke(this, args);
            Queue<EventHandler>? registerOnceCallbacks = _registerOnceCallbacks;
            if (registerOnceCallbacks != null)
            {
                while (registerOnceCallbacks.Count != 0)
                {
                    EventHandler callback = registerOnceCallbacks.Dequeue();
                    callback(this, args);
                }
            }
        }

        protected bool SetField<T>(ref T field, T value, [global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (global::System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
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

/// <summary>
/// Generates an <see cref="Microsoft.UI.Xaml.Data.IValueConverter"/> for your <see cref="ImmutableEditableObjectAdapter{TContract}"/> type, by annotating it with the converter type you wish to generate the members of.
/// </summary>
public sealed class ImmutableEditableValueConverterAttribute : global::System.Attribute
{
    public global::System.Type ValueConverterToGenerateType { get; }

    /// <summary>
    /// Generates an <see cref="Microsoft.UI.Xaml.Data.IValueConverter"/> for your <see cref="ImmutableEditableObjectAdapter{TContract}"/> type, by annotating it with the converter type you wish to generate the members of.
    /// </summary>
    /// <param name="valueConverterToGenerateType">The <c>sealed partial class</c> type of the <see cref="Microsoft.UI.Xaml.Data.IValueConverter"/> to generate.</param>
    public ImmutableEditableValueConverterAttribute(global::System.Type valueConverterToGenerateType)
    {
        ValueConverterToGenerateType = valueConverterToGenerateType;
    }
}
#nullable restore