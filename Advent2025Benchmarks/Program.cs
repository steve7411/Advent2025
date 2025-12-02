using Advent2025Benchmarks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<DayBenchmarks>(DefaultConfig.Instance.AddJob(Job.Default.WithCustomBuildConfiguration("Bench")));