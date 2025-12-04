using BenchmarkDotNet.Attributes;
using static Advent2025.DayRunner;

namespace Advent2025Benchmarks;

[MemoryDiagnoser]
public class DayBenchmarks {
    [Params(1, 2, 3, 4)]
    public int day;

    [Benchmark]
    public void Day() => RunDayNoPrint(day);
}
