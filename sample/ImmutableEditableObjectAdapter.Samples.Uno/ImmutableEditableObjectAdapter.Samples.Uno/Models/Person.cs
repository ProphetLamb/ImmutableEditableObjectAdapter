namespace ImmutableEditableObjectAdapter.Samples.Uno.Models;

public sealed record Person(string Name, string FavouriteColor, DateTimeOffset BirthDay, DateTimeOffset? DeceasedAt);

[ImmutableEditableValueConverter(typeof(Converters.EditablePersonValueConverter))]
public sealed partial class EditablePerson : System.ComponentModel.ImmutableEditableObjectAdapter<Person>;

