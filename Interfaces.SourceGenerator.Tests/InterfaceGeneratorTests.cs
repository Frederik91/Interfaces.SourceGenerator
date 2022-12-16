using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using VerifyCS = Interfaces.SourceGenerator.Tests.CSharpSourceGeneratorVerifier<Interfaces.SourceGenerator.InterfaceGenerator>;
using Microsoft.CodeAnalysis.Testing;
using System.ComponentModel.Design;

namespace Interfaces.SourceGenerator.Tests;

public class InterfaceGeneratorTests
{
    private static readonly string _header = """
        // <auto-generated/>
        #pragma warning disable
        #nullable enable
        namespace Interfaces.SourceGenerator.Tests;
        
        """;

    private readonly ImmutableArray<string> references = AppDomain.CurrentDomain
    .GetAssemblies()
    .Where(assembly => !assembly.IsDynamic)
    .Select(assembly => assembly.Location)
    .ToImmutableArray();
    
    private async Task RunTestAsync(string code, string expectedResult)
    {
        var tester = new VerifyCS.Test
        {
            TestState =
                {
                    Sources = { code },
                    GeneratedSources =
                    {
                        (typeof(InterfaceGenerator), "IClass1.g.cs",
                            SourceText.From(expectedResult, Encoding.UTF8))
                    }
                },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        };

        tester.ReferenceAssemblies.AddAssemblies(references);
        tester.TestState.AdditionalReferences.Add(typeof(InterfaceGenerator).Assembly);
        tester.TestState.AdditionalReferences.Add(typeof(Contracts.Attributes.GenerateInterfaceAttribute).Assembly);

        await tester.RunAsync();
    }

    [Fact]
    public async Task CreateInterface()
    {
        var code = """
using Interfaces.SourceGenerator.Tests.Models;
using Interfaces.SourceGenerator.Contracts.Attributes;
namespace Interfaces.SourceGenerator.Tests
{
    [GenerateInterface]
    public class Class1
    {
        public void Method1() { }
        public TestModel Test() { return new TestModel(); }
        public void Test2<T>(T data) { }
        public void Test3<T>(T data) where T : TestModel { }
        public string Property1 { get; set; }
    }
}

namespace Interfaces.SourceGenerator.Tests.Models
{
    public class TestModel
    {
        
    }
}
""";

        var expected = _header + """
public interface IClass1
{
    void Method1();
    Interfaces.SourceGenerator.Tests.Models.TestModel Test();
    void Test2<T>(T data);
    void Test3<T>(T data)
        where T : Interfaces.SourceGenerator.Tests.Models.TestModel;
    string Property1 { get; set; }
}
""";


        await RunTestAsync(code, expected);
        Assert.True(true); // silence warnings, real test happens in the RunAsync() method
    }
}