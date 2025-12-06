using System.Runtime.CompilerServices;

namespace Advent2025.Day06;

internal sealed class Day06 : DayBase {
    private enum Op : byte { Mul, Add }

    private const int HEIGHT = 4;
    private const int WIDTH = 1_000;

    private readonly ulong totalSum = 0;
    private readonly ulong transposedSum = 0;

    [SkipLocalsInit]
    public Day06() {
        using var reader = GetDataReader();
        var (width, height) = reader.BaseStream.GetLineInfoForRegularFile();
        reader.BaseStream.SeekToLine(height - 1, width);
        reader.DiscardBufferedData();

        Span<Op> ops = stackalloc Op[WIDTH];
        Span<byte> logs = stackalloc byte[WIDTH];
        for (int i = 0; !reader.EndOfStream; ++i) {
            ops[i] = (Op)(reader.Read() & 1);

            ref var log = ref logs[i];
            for (log = 0; reader.Peek() == ' '; ++log)
                reader.Read();
        }
        ++logs[^1];

        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        reader.DiscardBufferedData();

        Span<ulong> results = stackalloc ulong[WIDTH];
        for (var i = 0; i < results.Length; ++i)
            results[i] = (ulong)ops[i] ^ 1;

        Span<ulong> transposed = stackalloc ulong[width - WIDTH + 1];
        transposed.Clear();

        for (var y = 0; y < HEIGHT; ++y) {
            ref var t = ref transposed[0];
            for (var x = 0; x < WIDTH; ++x) {
                var num = 0UL;
                for (var log = logs[x] - 1; log >= 0; --log, t = ref Unsafe.Add(ref t, 1)) {
                    var digit = (ulong)reader.Read() & 0xF;
                    if (digit != 0) {
                        t = t * 10 + digit;
                        num = num * 10 + digit;
                    }
                }
                reader.Read();

                ref var res = ref results[x];
                res = ops[x] == Op.Mul ? res * num : res + num;
            }
            reader.Read();
        }

        totalSum = results.Sum();
        transposedSum = transposed.Sum();

        ref var curr = ref transposed[0];
        for (var x = 0; x < WIDTH; ++x) {
            if (ops[x] != Op.Mul) {
                curr = ref Unsafe.Add(ref curr, logs[x]);
                continue;
            }

            transposedSum -= curr;
            var res = curr;
            curr = ref Unsafe.Add(ref curr, 1);
            for (var i = logs[x]; i > 1; --i, curr = ref Unsafe.Add(ref curr, 1)) {
                transposedSum -= curr;
                res *= curr;
            }
            transposedSum += res;
        }
    }

    public override object? Part1() {
        Print("The total sum of all results is: {0}", totalSum);
        return Box<ulong>.Instance(totalSum);
    }

    public override object? Part2() {
        Print("The total transposed sum of all results is: {0}", transposedSum);
        return Box<ulong>.Instance(transposedSum);
    }
}
