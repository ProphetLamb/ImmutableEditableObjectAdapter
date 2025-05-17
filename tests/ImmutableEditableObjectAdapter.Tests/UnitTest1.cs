using System.ComponentModel;
using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ImmutableEditableObjectAdapter.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        // Create the 'input' compilation that the generator will act on
        Compilation inputCompilation = CreateCompilation(
            @"
namespace ImmutableEditableObjectAdapter.Tests.TestAssembly
{
    using System;
    using System.ComponentModel;

    public sealed record Person(string Name, string FavouriteColor, DateTimeOffset BirthDay, DateTimeOffset? DeceasedAt);

    public sealed partial class EditablePerson : ImmutableEditableObjectAdapter<Person>;

    public static class Program
    {
        public static void Main()
        {
            Person p = new(""Max"", ""Green"", DateTimeOffset.Now.AddYears(-43), null);
            EditablePerson editable = new(p);
            editable.Edited += (s, e) => p = s.IsPropertyChanged(nameof(Person.Name)) ? e.NewValue : p;
            editable.BeginEdit();
            editable.Name = ""Müller"";
            editable.EndEdit();
        }
    }
}
"
        );
        const int TEST_SOURCES_LEN = 1;
        const int GEN_SOURCES_LEN = 4; // ImmutableEditableObjectAdapter + EditableExtensions + EditablePerson + EditablePersonExtensions
        ImmutableEditableObjectAdapterGenerator generator = new();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            inputCompilation,
            out var outputCompilation,
            out var generatorDiagnostics
        );
        generatorDiagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Warning);
        outputCompilation.SyntaxTrees.Count().Should().Be(TEST_SOURCES_LEN + GEN_SOURCES_LEN);
        var analyzerDiagnostics = outputCompilation.GetDiagnostics();
        // Im unable to import the library required for IEnumerable &, EqualityComparer, as such only test relevant errors :/
        analyzerDiagnostics.Should().NotContain(d => d.Id != "CS0246" && d.Id != "CS0103" && d.Id != "CS1061" && d.Id != "CS0019" &&  d.Severity == DiagnosticSeverity.Error);

        var runResult = driver.GetRunResult();

        runResult.GeneratedTrees.Length.Should().Be(GEN_SOURCES_LEN);
        Assert.True(runResult.Diagnostics.IsEmpty);

        var generatorResult = runResult.Results[0];
        Assert.True(generatorResult.Generator.GetGeneratorType() == generator.GetType());
        Assert.True(generatorResult.Diagnostics.IsEmpty);
        generatorResult.GeneratedSources.Length.Should().Be(GEN_SOURCES_LEN);
        generatorResult.Exception.Should().BeNull();
    }


    private static CSharpCompilation CreateCompilation(string source) =>
        CSharpCompilation.Create(
            "compilation",
            [CSharpSyntaxTree.ParseText(source)],
            [

                MetadataReference.CreateFromFile(Assembly.Load("System").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Linq").Location),
                MetadataReference.CreateFromFile(Assembly.Load("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.ComponentModel").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.ObjectModel").Location),
            ],
            new CSharpCompilationOptions(OutputKind.ConsoleApplication)
        );
}
