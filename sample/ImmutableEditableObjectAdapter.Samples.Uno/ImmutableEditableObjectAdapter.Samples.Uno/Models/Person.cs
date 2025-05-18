namespace ImmutableEditableObjectAdapter.Samples.Uno.Models;

public sealed record Person(string Name, string FavouriteColor, DateTimeOffset BirthDay, DateTimeOffset? DeceasedAt);

public sealed partial class EditablePerson : System.ComponentModel.ImmutableEditableObjectAdapter<Person>;

