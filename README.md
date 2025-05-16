[![NuGet version](https://badge.fury.io/nu/ImmutableEditableObjectAdapter.svg)](https://www.nuget.org/packages/ImmutableEditableObjectAdapter)

```bash
dotnet add package ImmutableEditableObjectAdapter
```

# ImmutableEditableObjectAdapter

Adapts immutable state `record`s into an `IEditableObject` replacing the `record` on edit, intended for `Binding` in a `DataGrid`. 

```csharp
using System.ComponentModel;

Person p = new("Max", "Green", DateTimeOffset.Now.AddYears(-43), null);
EditablePerson editable = new(p);
editable.Edited += (s, e) => p = s.IsPropertyChanged(nameof(Person.Name)) ? e.NewValue : p;
editable.BeginEdit();
editable.Name = "Müller";
editable.EndEdit();
Console.WriteLine("Hello, World!");

internal sealed record Person(string Name, string FavouriteColor, DateTimeOffset BirthDay, DateTimeOffset? DeceasedAt);

internal sealed partial class EditablePerson : ImmutableEditableObjectAdapter<Person>;
```

Generated `ImmutableEditableObjectAdapter` types mirrors the `public` Properties of the `record` passed as a generic type parameter. However, all properties have setter.
Each property, set to a different value than the property in the `Unedited` reference, is used to reconstruct `Unedited` into a new `record`:

```csharp
Person edited = Unedited with {
  Name = NamePropertyChanged ? Name : Unedited.Name,
}
```

The constructed record is passed as `NewValue` to the `Edited` event, then set as the new `Unedited`.

## Customization

`ImmutableEditableObjectAdapter` API allows customizing the creation of events.

- OnPropertyChanging
- OnPropertyChanged
- OnEdited

```csharp
/// <summary>
/// Provides the old, and new value of the <see cref="EditedEventHandler{TContract}"/>.
/// </summary>
/// <typeparam name="TContract">The type of the contract <c>record</c>.</typeparam>
public sealed class EditedEventArgs<TContract> : EventArgs
{
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

    protected virtual void OnPropertyChanging([CallerMemberName] string? propertyName = null);
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null);
    protected virtual void OnEdited(TContract oldValue, TContract newValue);
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null);
}
```
