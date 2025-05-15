// See https://aka.ms/new-console-template for more information

using System.ComponentModel;

Person p = new("Max", "Green", DateTimeOffset.Now.AddYears(-43));
EditablePerson editable = new(p);
editable.Edited += (s, e) => p = s.IsPropertyChanged(nameof(Person.Name)) ? e.NewValue : p;
editable.BeginEdit();
editable.Name = "Müller";
editable.EndEdit();
Console.WriteLine("Hello, World!");

internal sealed record Person(string Name, string FavouriteColor, DateTimeOffset BirthDay);

internal sealed partial class EditablePerson : ImmutableEditableObjectAdapter<Person>;
