// See https://aka.ms/new-console-template for more information

using System.ComponentModel;
using System.Diagnostics;

Person p = new("Max", "Green", DateTimeOffset.Now.AddYears(-43));
EditablePerson editable = new(p);
editable.Edited += (s, e) => p = s.IsPropertyChanged(nameof(Person.Name)) ? e.NewValue : p;
editable.BeginEdit();
editable.Name = "Müller";
editable.EndEdit();
Console.WriteLine("Hello, World!");

record Person(string Name, string FavouriteColor, DateTimeOffset BirthDay);

sealed partial class EditablePerson : ImmutableEditableObjectAdapter<Person>;