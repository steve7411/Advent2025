using FluentAssertions;
using System.Collections;

namespace Advent2025Tests;

public class DayOutputValidationTestData : IEnumerable<object?[]> {
    private static readonly (Type type, object? part1, object? part2)[] validationData = {
            (typeof(Advent2025.Day01.Day01), 1097, 7101),
            (typeof(Advent2025.Day02.Day02), 21898734247UL, 28915664389UL),
            (typeof(Advent2025.Day03.Day03), 16887UL, 167302518850275UL),
            (typeof(Advent2025.Day04.Day04), 1419U, 8739U),
            (typeof(Advent2025.Day05.Day05), 739, 344486348901788UL),
            (typeof(Advent2025.Day06.Day06), 4405895212738UL, 7450962489289UL),
            (typeof(Advent2025.Day07.Day07), 1640, 40999072541589L),
            (typeof(Advent2025.Day08.Day08), 57564, 133296744),
            (typeof(Advent2025.Day09.Day09), 4749929916L, 1572047142L),
            (typeof(Advent2025.Day10.Day10), 578, 20709),
            (typeof(Advent2025.Day11.Day11), 555, 502447498690860L),
            (typeof(Advent2025.Day12.Day12), 427, default),
            //(typeof(Advent2025.Day13.Day13), default, default),
            //(typeof(Advent2025.Day14.Day14), default, default),
            //(typeof(Advent2025.Day15.Day15), default, default),
            //(typeof(Advent2025.Day16.Day16), default, default),
            //(typeof(Advent2025.Day17.Day17), default, default),
            //(typeof(Advent2025.Day18.Day18), default, default),
            //(typeof(Advent2025.Day19.Day19), default, default),
            //(typeof(Advent2025.Day20.Day20), default, default),
            //(typeof(Advent2025.Day21.Day21), default, default),
            //(typeof(Advent2025.Day22.Day22), default, default),
            //(typeof(Advent2025.Day23.Day23), default, default),
            //(typeof(Advent2025.Day24.Day24), default, default),
            //(typeof(Advent2025.Day25.Day25), default, default),
        };

    public IEnumerator<object?[]> GetEnumerator() =>
        validationData.Select(x => new object?[] { x.type, x.part1, x.part2 }).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}

public class DayOutputValidationTests {
    [Theory]
    [ClassData(typeof(DayOutputValidationTestData))]
    public void ValidateDay(Type type, object? part1, object? part2) {
        var day = Activator.CreateInstance(type) as Advent2025.IDay ?? throw new Exception($"Unable to instantiate object of type {type}");
        using (new FluentAssertions.Execution.AssertionScope()) {
            day.Part1().Should().Be(part1);
            day.Part2().Should().Be(part2);
        }
    }
}