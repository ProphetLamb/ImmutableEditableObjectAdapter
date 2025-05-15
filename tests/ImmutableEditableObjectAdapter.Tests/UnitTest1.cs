using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ImmutableEditableObjectAdapter.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {        // Create the 'input' compilation that the generator will act on
        Compilation inputCompilation = CreateCompilation(@"
namespace ImmutableEditableObjectAdapter.Tests.TestAssembly
{
    using System;
    using System.ComponentModel;

    public record Person(string Name, DateTimeOffset BirthDay);

    public sealed partial class EditablePerson : ImmutableEditableObjectAdapter<Person>;

    public static class Program
    {
        public static void Main()
        {

        }
    }
}
");
        const int TEST_SOURCES_LEN = 1;
        const int GEN_SOURCES_LEN = 5; // Attribute + Dummy + NoAttr + NoFlags + PreferThisEnum
        ImmutableEditableObjectAdapterGenerator generator = new();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var generatorDiagnostics);

        generatorDiagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Warning);
        outputCompilation.SyntaxTrees.Count().Should().Be(TEST_SOURCES_LEN + GEN_SOURCES_LEN);

        var analyzerDiagnostics = outputCompilation.GetDiagnostics();
        analyzerDiagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);

        GeneratorDriverRunResult runResult = driver.GetRunResult();

        runResult.GeneratedTrees.Length.Should().Be(GEN_SOURCES_LEN);
        Assert.True(runResult.Diagnostics.IsEmpty);

        GeneratorRunResult generatorResult = runResult.Results[0];
        Assert.True(generatorResult.Generator.GetGeneratorType() == generator.GetType());
        Assert.True(generatorResult.Diagnostics.IsEmpty);
        generatorResult.GeneratedSources.Length.Should().Be(GEN_SOURCES_LEN);
        generatorResult.Exception.Should().BeNull();
    }


    private static CSharpCompilation CreateCompilation(string source)
        => CSharpCompilation.Create("compilation",
            [CSharpSyntaxTree.ParseText(source)],
            [
                MetadataReference.CreateFromFile(typeof(global::System.String).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(global::System.ComponentModel.DescriptionAttribute).GetTypeInfo()
                    .Assembly.Location),
                MetadataReference.CreateFromFile(typeof(global::System.ReadOnlySpan<char>).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(global::System.Collections.Generic.List<char>).GetTypeInfo()
                    .Assembly.Location),
                MetadataReference.CreateFromFile(typeof(global::System.Runtime.CompilerServices.MethodImplAttribute)
                    .GetTypeInfo()
                    .Assembly.Location),
                MetadataReference.CreateFromFile(typeof(global::System.Runtime.Serialization.ISerializable).GetTypeInfo()
                    .Assembly.Location),
                MetadataReference.CreateFromFile(typeof(global::System.Runtime.InteropServices.StructLayoutAttribute)
                    .GetTypeInfo()
                    .Assembly.Location),
                MetadataReference.CreateFromFile(
                    @"C:\Program Files (x86)\dotnet\shared\Microsoft.NETCore.App\9.0.3\System.Runtime.dll"),
                MetadataReference.CreateFromFile(typeof(global::System.HashCode).GetTypeInfo().Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
}
