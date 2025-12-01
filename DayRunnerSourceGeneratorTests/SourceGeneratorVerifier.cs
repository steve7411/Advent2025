using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
namespace DayRunnerSourceGeneratorTests;

public class CSharpIncrementalGeneratorTest<TSourceGenerator, TVerifier> : CSharpSourceGeneratorTest<EmptySourceGeneratorProvider, TVerifier>
    where TSourceGenerator : IIncrementalGenerator, new()
    where TVerifier : IVerifier, new() {

    protected override string DefaultFileExt => "cs";
    public override string Language => LanguageNames.CSharp;
    private static ImmutableDictionary<string, ReportDiagnostic> NullableWarnings { get; } = GetNullableWarningsFromCompiler();

    private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler() {
        string[] args = ["/warnaserror:nullable"];
        CSharpCommandLineArguments commandLineArguments = CSharpCommandLineParser.Default.Parse(args, Environment.CurrentDirectory, Environment.CurrentDirectory);
        ImmutableDictionary<string, ReportDiagnostic> nullableWarnings = commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;
        return nullableWarnings;
    }

    //protected override IEnumerable<ISourceGenerator> GetSourceGenerators()
    //    => [new TSourceGenerator().AsSourceGenerator()];

    //protected override GeneratorDriver CreateGeneratorDriver(Project project, ImmutableArray<ISourceGenerator> sourceGenerators) {
    //    return CSharpGeneratorDriver.Create(
    //        sourceGenerators,
    //        project.AnalyzerOptions.AdditionalFiles,
    //        (CSharpParseOptions)project.ParseOptions!,
    //        project.AnalyzerOptions.AnalyzerConfigOptionsProvider);
    //}

    protected override CompilationOptions CreateCompilationOptions() {
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
        return compilationOptions.WithSpecificDiagnosticOptions(
             compilationOptions.SpecificDiagnosticOptions.SetItems(NullableWarnings));
    }

    protected override ParseOptions CreateParseOptions()
        => new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose);

    public static async Task VerifyGeneratorAsync(string source)
        => await VerifyGeneratorAsync(source, []);

    public static async Task VerifyGeneratorAsync(string source, params DiagnosticResult[] diagnostics)
        => await VerifyGeneratorAsync(source, diagnostics);

    public static async Task VerifyGeneratorAsync(string source, DiagnosticResult[] diagnostics, CompilerDiagnostics compilerDiagnostics = CompilerDiagnostics.Errors, params (string filename, string content)[] generatedSources) {
        CSharpIncrementalGeneratorTest<TSourceGenerator, XUnitVerifier> test = new() {
            TestState =
            {
                Sources = { source },
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            CompilerDiagnostics = compilerDiagnostics,
        };

        foreach ((string filename, string content) generatedSource in generatedSources)
            test.TestState.GeneratedSources.Add((typeof(TSourceGenerator), generatedSource.filename, SourceText.From(generatedSource.content, Encoding.UTF8)));
        
        test.ExpectedDiagnostics.AddRange(diagnostics);

        await test.RunAsync(CancellationToken.None);
    }
}