using Microsoft.CodeAnalysis.Testing;
using DayRunnerSourceGenerator;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace DayRunnerSourceGeneratorTests;
using VerifyCS = CSharpIncrementalGeneratorTest<DayRunnerGenerator, XUnitVerifier>;

public class DayRunnerSourceGeneratorSnapshotTests {
    [Fact(Skip = "Need to figure out how to do these nowadays")]
    public async Task SnapshotTest() {
        var code = """
            using System;
            namespace Advent3000 {
                internal interface IDay { }
                internal abstract class DayBase : IDay { }
            }

            namespace Advent3000.Day01 {
                using Advent3000;
                internal class Day01 : DayBase { }
            }

            namespace Advent3000.Day02 {
                using Advent3000;
                internal abstract class Day02Base : IDay { }
                internal class Day02 : Day02Base { }
            }

            namespace Advent3000.Day03 {
                using Advent3000;
                internal class Day03 : IDay { }
            }
            """;

        var expected = File.ReadAllText("Snapshot.txt");
        await VerifyCS.VerifyGeneratorAsync(code, DiagnosticResult.EmptyDiagnosticResults, CompilerDiagnostics.None, ("DayRunner.g.cs", expected));
    }
}