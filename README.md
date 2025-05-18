[![NuGet Version](https://img.shields.io/nuget/v/ImmutableEditableObjectAdapter)](https://www.nuget.org/packages/ImmutableEditableObjectAdapter) [![NuGet Downloads](https://img.shields.io/nuget/dt/ImmutableEditableObjectAdapter)](https://www.nuget.org/packages/ImmutableEditableObjectAdapter)

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

Feel free to review the [generated code](https://github.com/ProphetLamb/ImmutableEditableObjectAdapter/blob/main/sample/ImmutableEditableObjectAdapter.Samples/GeneratedFiles/ImmutableEditableObjectAdapter/ImmutableEditableObjectAdapter.ImmutableEditableObjectAdapterGenerator/EditablePerson.g.cs#L7) for this example.

Generated `ImmutableEditableObjectAdapter` types mirrors the `public` Properties of the `record` passed as a generic type parameter. However, all properties have setter.
Each property, set to a different value than the property in the `Unedited` reference, is used to reconstruct `Unedited` into a new `record`:

```csharp
Person edited = Unedited with {
  Name = NamePropertyChanged ? Name : Unedited.Name,
}
```

The constructed record is passed as `NewValue` to the `Edited` event, then set as the new `Unedited`.

## UNO Platform Integration

https://github.com/user-attachments/assets/26737d4e-9eeb-48d0-814c-687048ce235d

Binding commands to edits in UNO requires
- a `IValueConverter`
- a `ICommand` attached property

in addition to the above example. `ImmutableEditableObjectAdapter` generates these implementations.

**Declare the models**

```csharp
namespace ImmutableEditableObjectAdapter.Samples.Uno.Models;

public sealed record Person(
  string Name,
  [property: Display(Name = "Color")] string FavouriteColor,
  DateTimeOffset BirthDay,
  DateTimeOffset? DeceasedAt
);

public sealed partial class EditablePerson : System.ComponentModel.ImmutableEditableObjectAdapter<Person>;
```

**Declare the converter**

Annotate the `EditablePersonValueConverter` implementing `IValueConverter` with the `ImmutableEditableValueConverter` attribute for the type `EditablePerson`.

```csharp
namespace ImmutableEditableObjectAdapter.Samples.Uno.Converters;

[ImmutableEditableValueConverter(typeof(EditablePerson))]
public sealed partial class EditablePersonValueConverter : IValueConverter;
```

**Create the model**

- `Persons` provides data for the `DataGrid`.
- `LastEdited` informs the user about the latest changes.
- `PersonChanged` is invoked when a person changed.

```csharp
namespace ImmutableEditableObjectAdapter.Samples.Uno.Presentation;

public partial record MainModel
{
    public IState<Person> LastEdited => State<Person>.Empty(this); 
    
    public IListState<Person> Persons => ListState.Value(this, IImmutableList<Person> () => [
        new("Max", "Green", DateTimeOffset.Now.AddYears(-43), null),
        new("Günter", "Orange", DateTimeOffset.Now.AddYears(-32), null),
    ]);
    
    public async Task PersonChanged(EditedEventArgs<Person> edited)
    {
        if (edited.CancelledOrUnchanged)
        {
            return;
        }
    
        await LastEdited.UpdateAsync(_ => edited.NewValue);
    }
}
```

**Create the UI**

```xml
<Page.Resources>
  <converters:EditablePersonValueConverter x:Key="EditablePersonValueConverter" />
</Page.Resources>
```

```xml
<TextBox IsReadOnly="True" Header="Changed Name" Text="{Binding LastEdited.Name}" />
<TextBox IsReadOnly="True" Header="Changed Favourite Colour" Text="{Binding LastEdited.FavouriteColor}" />

<ui:FeedView Source="{Binding Persons, Converter={StaticResource EditablePersonValueConverter}}">
  <ui:FeedView.ValueTemplate>
    <DataTemplate>
      <wuc:DataGrid
        ItemsSource="{Binding Data, Mode=TwoWay}"
        utu:EditableExtensions.Command="{utu:AncestorBinding Path=DataContext.PersonChanged, AncestorType=ui:FeedView}">
      </wuc:DataGrid>
    </DataTemplate>
  </ui:FeedView.ValueTemplate>
</ui:FeedView>
```

Feel free to review the [generated code](https://github.com/ProphetLamb/ImmutableEditableObjectAdapter/blob/main/sample/ImmutableEditableObjectAdapter.Samples.Uno/ImmutableEditableObjectAdapter.Samples.Uno/GeneratedFiles/ImmutableEditableObjectAdapter/ImmutableEditableObjectAdapter.ImmutableEditableObjectAdapterGenerator/EditablePersonValueConverter.g.cs#L5) for this example.

## Customization and API

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
    public bool CancelledOrUnchanged { get; }
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
public abstract class ImmutableEditableObjectAdapter<TContract>
    : IImmutableEditableObjectAdapter
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

/// <summary>
/// Generates an <see cref="Microsoft.UI.Xaml.Data.IValueConverter"/> for your <see cref="ImmutableEditableObjectAdapter{TContract}"/> type, by annotating it with the converter type you wish to generate the members of.
/// </summary>
/// <param name="valueConverterToGenerateType">The <c>sealed partial class</c> type of the <see cref="Microsoft.UI.Xaml.Data.IValueConverter"/> to generate.</param>
public sealed class ImmutableEditableValueConverterAttribute(Type valueConverterToGenerateType) : Attribute
{
   public Type ValueConverterToGenerateType { get; } = valueConverterToGenerateType;
}
```
