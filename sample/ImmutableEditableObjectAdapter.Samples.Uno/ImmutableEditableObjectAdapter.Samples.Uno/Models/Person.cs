using System.ComponentModel.DataAnnotations;

namespace ImmutableEditableObjectAdapter.Samples.Uno.Models;

public sealed record Person(
    string Name,
    [property: Display(Name = "Color")] string FavouriteColor,
    DateTimeOffset BirthDay,
    DateTimeOffset? DeceasedAt
);

public sealed partial class EditablePerson : System.ComponentModel.ImmutableEditableObjectAdapter<Person>;
