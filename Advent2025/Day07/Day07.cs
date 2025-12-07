using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics.Tensors;

namespace Advent2025.Day07;

internal sealed class Day07 : DayBase {
    private readonly int splitCount;
    private readonly long timelineCount;

    [SkipLocalsInit]
    public Day07() {
        using var stream = GetDataStream();
        var (width, height) = stream.GetLineInfoForRegularFile();

        Span<byte> readBuffer = stackalloc byte[width];
        Span<long> beams = stackalloc long[width];
        beams.Clear();

        stream.ReadExactly(readBuffer);
        var startIdx = readBuffer.IndexOf((byte)'S');
        Debug.Assert(startIdx >= 0);
        beams[startIdx] = 1;

        var lineLen = width + Environment.NewLine.Length;
        var twoLines = lineLen << 1;

        Span<long> splitters = stackalloc long[width];
        Span<long> shiftBuffer = stackalloc long[width + 2];
        var middleShiftBuffer = shiftBuffer[1..^1];
        var leftShiftBuffer = shiftBuffer[..^2];
        var rightShiftBuffer = shiftBuffer[2..];
        shiftBuffer[0] = shiftBuffer[^1] = 0;
        Span<bool> bools = MemoryMarshal.CreateSpan(ref Unsafe.As<byte, bool>(ref readBuffer[0]), width);

        for (long y = 3, pos = lineLen << 1; y < height; y += 2, pos += twoLines) {
            stream.Seek(pos, SeekOrigin.Begin);
            stream.ReadExactly(readBuffer);
            readBuffer.RightShift(6);

            TensorPrimitives.ConvertTruncating(readBuffer, splitters);

            TensorPrimitives.Negate(splitters, splitters);                              // Mask where a splitter was found
            TensorPrimitives.BitwiseAnd(splitters, beams, shiftBuffer[1..]);            // Beams that are hitting splitters this row
            TensorPrimitives.OnesComplement(splitters, splitters);                      // Mask where there is no splitter
            TensorPrimitives.BitwiseAnd(splitters, beams, splitters);                   // Beams that DON'T hit a splitter this row

            TensorPrimitives.IsZero(middleShiftBuffer, bools);                          // False for any beam currently hitting a splitter
            splitCount += width - TensorPrimitives.Sum(readBuffer);

            TensorPrimitives.Add(leftShiftBuffer, rightShiftBuffer, beams);             // Add split beams shifted left and shifted right
            TensorPrimitives.Add(splitters, beams, beams);                              // Add beams that missed splitters this row
        }

        timelineCount = beams.Sum();
    }

    public override object? Part1() {
        Print("The total split count is: {0}", splitCount);
        return Box<int>.Instance(splitCount);
    }

    public override object? Part2() {
        Print("The total timeline count is: {0}", timelineCount);
        return Box<long>.Instance(timelineCount);
    }
}
