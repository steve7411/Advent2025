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
        Span<long> tmpBuffer = stackalloc long[width];
        Span<long> shiftBuffer = stackalloc long[width + 2];
        shiftBuffer[0] = shiftBuffer[^1] = 0;

        for (long y = 3, pos = lineLen << 1; y < height; y += 2, pos += twoLines) {
            stream.Seek(pos, SeekOrigin.Begin);
            stream.ReadExactly(readBuffer);
            readBuffer.RightShift(6);

            TensorPrimitives.ConvertTruncating(readBuffer, splitters);

            TensorPrimitives.Negate(splitters, tmpBuffer);
            TensorPrimitives.BitwiseAnd(tmpBuffer, beams, shiftBuffer[1..]);
            TensorPrimitives.OnesComplement(tmpBuffer, tmpBuffer);
            TensorPrimitives.BitwiseAnd(tmpBuffer, beams, tmpBuffer);

            TensorPrimitives.Negate(beams, beams);
            TensorPrimitives.ShiftRightArithmetic(beams, 63, beams);
            TensorPrimitives.BitwiseAnd(beams, splitters, splitters);

            splitCount += (int)splitters.Sum();

            TensorPrimitives.Add(shiftBuffer[2..], shiftBuffer[..^2], splitters);
            TensorPrimitives.Add(splitters, tmpBuffer, splitters);
            
            var tmp = splitters;
            splitters = beams;
            beams = tmp;
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
