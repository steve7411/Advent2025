using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace DayRunnerSourceGenerator;

using DayInfo = (string shortName, string qualifiedName, string topNamespace);

[Generator(LanguageNames.CSharp)]
public class DayRunnerGenerator : IIncrementalGenerator {
    private static readonly Regex baseTypeRegex = new("""IDay|DayBase|Day\d+Base""", RegexOptions.Compiled);
    private static readonly Regex trailingNumberExtractor = new("""[1-9]\d*$""", RegexOptions.Compiled);
    
    private readonly StringBuilder outputBuilder = new();

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var iDayDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
            static (n, _) => n is ClassDeclarationSyntax cds && cds.BaseList != null && !cds.Modifiers.Any(m => m.RawKind == (int)SyntaxKind.AbstractKeyword) && cds.BaseList.Types.Any(t => baseTypeRegex.IsMatch(t.Type.ToString())),
            static (n, _) => {
                if (n.SemanticModel.GetDeclaredSymbol(n.Node) is not INamedTypeSymbol nts || !nts.AllInterfaces.Any(i => i.Name == "IDay"))
                    return default;
                return (shortName: nts.Name, qualifiedName: GetQualifiedName(nts)!, topNamespace: GetNamespaceString(nts));
            })
            .Where(static t => t.shortName != null)
            .Collect();

        context.RegisterSourceOutput(iDayDeclarations, (spc, dayInfos) =>
            spc.AddSource("DayRunner.g.cs", GetDayRunnerClass(dayInfos, outputBuilder)));
    }

    private static string GetDayRunnerClass(in ImmutableArray<DayInfo> dayInfos, StringBuilder outputBuilder) {
        outputBuilder.Clear();
        using StringWriter sw = new(outputBuilder);
        using IndentedTextWriter writer = new(sw);

        writer.WriteLine("using System.Diagnostics;");
        writer.WriteLine();
        writer.WriteLine($"namespace {dayInfos.FirstOrDefault().topNamespace ?? $"Advent{DateTime.Today.Year}"};");
        writer.WriteLine();
        writer.WriteLine("static partial class DayRunner {");
        ++writer.Indent;

        WriteRunDayByType(writer, dayInfos);
        writer.WriteLine();
        WriteRunLatestDay(writer, dayInfos);
        writer.WriteLine();
        WriteRunAllDays(writer, dayInfos);
        writer.WriteLine();
        WriteRunDayByNumber(writer, dayInfos, true);
        writer.WriteLine();
        WriteRunDayByNumber(writer, dayInfos, false);
        writer.WriteLine();
        WriteGetDurationString(writer);

        --writer.Indent;
        writer.WriteLine("}");

        return sw.ToString();
    }

    private static void WriteRunDayByNumber(IndentedTextWriter writer, in ImmutableArray<DayInfo> dayInfos, bool print) {
        writer.WriteLine($"public static void RunDay{(print ? "" : "NoPrint")}(int dayNumber) {{");
        ++writer.Indent;

        writer.WriteLine("switch (dayNumber) {");
        ++writer.Indent;
        foreach (var di in dayInfos.OrderBy(d => d.shortName)) {
            writer.Write($"case {ExtractNumber(di.shortName)}: ");
            WriteRunBlock(writer, di, true, print);
        }
        writer.WriteLine("""default: throw new ArgumentException("Invalid value passed in for dayNumber");""");
        --writer.Indent;
        writer.WriteLine("}");

        --writer.Indent;
        writer.WriteLine("}");
    }

    private static string ExtractNumber(string shortName) => trailingNumberExtractor.Match(shortName).Value;

    private static void WriteRunDayByType(IndentedTextWriter writer, in ImmutableArray<DayInfo> dayInfos) {
        writer.WriteLine("public static void RunDay<TDay>() where TDay : IDay, new() {");
        ++writer.Indent;
        writer.WriteLine("var dayType = typeof(TDay);");
        WriteTypeBranches(writer, dayInfos);
        writer.WriteLine("throw new UnreachableException();");
        --writer.Indent;
        writer.WriteLine("}");
    }

    private static void WriteTypeBranches(IndentedTextWriter writer, in ImmutableArray<DayInfo> dayInfos) {
        foreach (var di in dayInfos) {
            writer.Write($"if (dayType == typeof({di.qualifiedName})) ");
            WriteRunBlock(writer, di);
        }
    }

    private static void WriteRunAllDays(IndentedTextWriter writer, in ImmutableArray<DayInfo> dayInfos) {
        writer.WriteLine("public static void RunAllDays() {");
        ++writer.Indent;
        writer.WriteLine("var allDaysStartTime = Stopwatch.GetTimestamp();");
        foreach (var di in dayInfos.OrderBy(d => d.shortName))
            WriteRunBlock(writer, di, false);
        writer.WriteLine("""Console.WriteLine($"Finished running all days in {GetDurationString(Stopwatch.GetElapsedTime(allDaysStartTime))}");""");
        --writer.Indent;
        writer.WriteLine("}");
    }

    private static void WriteRunLatestDay(IndentedTextWriter writer, in ImmutableArray<DayInfo> dayInfos) {
        writer.Write("public static void RunLatestDay() ");
        WriteRunBlock(writer, GetMax(dayInfos), false);
    }

    private static void WriteRunBlock(IndentedTextWriter writer, in DayInfo dayInfo, bool returnAtEnd = true, bool print = true) {
        writer.WriteLine("{");
        ++writer.Indent;
        if (print) {
            writer.WriteLine($$"""Console.WriteLine("Running {{dayInfo.shortName}}:");""");
            writer.WriteLine("var startTime = Stopwatch.GetTimestamp();");
        }
        writer.WriteLine($"var day = new {dayInfo.qualifiedName}();");
        writer.WriteLine($"day.Part1({(print ? "true" : "false")});");
        writer.WriteLine($"day.Part2({(print ? "true" : "false")});");
        if (print)
            writer.WriteLine("""Console.WriteLine($"Finished in {GetDurationString(Stopwatch.GetElapsedTime(startTime))}");""");
        if (returnAtEnd)
            writer.WriteLine("return;");
        --writer.Indent;
        writer.WriteLine("}");
    }

    private static void WriteGetDurationString(IndentedTextWriter writer) {
        writer.WriteLine("private static string GetDurationString(TimeSpan elapsed) {");
        ++writer.Indent;
        writer.WriteLine("return elapsed.TotalMilliseconds >= 1.0");
        ++writer.Indent;
        writer.WriteLine("""? $"{elapsed.TotalMilliseconds} milliseconds" """);
        writer.WriteLine(""": $"{elapsed.Ticks / (TimeSpan.TicksPerMillisecond / 1000.0):#,#} microseconds";""");
        --writer.Indent;
        --writer.Indent;
        writer.WriteLine("}");
    }

    private static INamespaceSymbol? GetTopLevelNamespace(ISymbol? symbol) {
        var namespaceSymbol = symbol as INamespaceSymbol ?? symbol?.ContainingNamespace;
        while (namespaceSymbol?.ContainingNamespace != null && !namespaceSymbol.ContainingNamespace.IsGlobalNamespace)
            namespaceSymbol = namespaceSymbol.ContainingNamespace;
        return namespaceSymbol;
    }

    private static string? GetQualifiedName(ISymbol? symbol) =>
        symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));

    private static string GetNamespaceString(INamedTypeSymbol symbol) =>
        GetQualifiedName(GetTopLevelNamespace(symbol))!;

    private static DayInfo GetMax(in ImmutableArray<DayInfo> symbols) {
        var max = symbols[0];
        for (var i = 1; i < symbols.Length; i++) {
            var symbol = symbols[i];
            if (string.Compare(max.shortName, symbol.shortName, StringComparison.OrdinalIgnoreCase) < 0)
                max = symbol;
        }
        return max;
    }
}
