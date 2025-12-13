using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Advent2025.Day12;

internal sealed class Day12 : DayBase {
    const int SHAPE_COUNT = 6;

    private readonly int canFitCount;

    [SkipLocalsInit]
    public Day12() {
        Span<char> newLineDump = stackalloc char[Environment.NewLine.Length];
        Span<uint> shapes = stackalloc uint[SHAPE_COUNT];
        using var reader = GetDataReader();
        for (var i = 0; i < SHAPE_COUNT; ++i)
            ReadShape(reader, ref shapes[i], newLineDump);

        while (!reader.EndOfStream) {
            var (width, height) = (reader.ReadNextInt(), reader.ReadNextInt());
            var ch = reader.Read();
            Debug.Assert(ch == ' ');
            var sum = 0;
            var requiredArea = 0;
            for (var i = 0; i < SHAPE_COUNT; ++i) {
                var r = reader.ReadNextInt();
                sum += r;
#if DEBUG
                requiredArea += r * BitOperations.PopCount(shapes[i]);
#endif
            }

            var gridArea = width * height;
            canFitCount += requiredArea <= gridArea & gridArea >= sum * 9 ? 1 : 0; // lol
        }
    }

#if DEBUG
    private static void ReadShape(StreamReader reader, ref uint bits, Span<char> newLineDump) {
        var ch = reader.Read();
        Debug.Assert(ch is >= '0' and <= '9');
        ch = reader.Read();
        Debug.Assert(ch == ':');
        reader.Read(newLineDump);

        bits = ReadLine(reader, 6);
        reader.Read(newLineDump);
        bits |= ReadLine(reader, 3);
        reader.Read(newLineDump);
        bits |= ReadLine(reader, 0);

        reader.Read(newLineDump);
        reader.Read(newLineDump);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint ReadLine(StreamReader reader, int offset) =>
            (uint)((reader.Read() & 1) << offset + 2 | (reader.Read() & 1) << offset + 1 | (reader.Read() & 1) << offset);
    }
#else
    private static void ReadShape(StreamReader reader, ref uint bits, Span<char> newLineDump) {
        var size = 11 + (newLineDump.Length << 2) + newLineDump.Length;
        bits = 0;
        for (var i = 0; i < size; ++i)
            reader.Read();
    }
#endif

    [Conditional("DEBUG")]
    private static void PrintShape(uint shape) {
        Span<char> buffer = stackalloc char[13];
        buffer[3] = buffer[7] = buffer[11] = buffer[12] = '\n';

        var write = 0;
        for (var i = 8; i >= 0;) {
            for (var j = 0; j < 3; ++j, --i)
                buffer[write++] = (shape >>> i & 1) == 1 ? '#' : '.';
            ++write;
        }
        Console.Out.Write(buffer);
    }

    public override object? Part1() {
        Print("The count of regions that can fit required presents is {0}", canFitCount);
        return Box<int>.Instance(canFitCount);
    }
}
