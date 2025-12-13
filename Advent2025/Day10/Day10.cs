using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Advent2025.Day10;

internal unsafe sealed class Day10 : DayBase {
    private static class WideBits<T> where T : unmanaged, IBinaryInteger<T> {
        public const int SPACING = 10;

        private static readonly ulong LOW_PARITY;
        private static readonly ulong HIGH_PARITY;

        public static readonly T BORROW_MASK;
        public static readonly T PARITY_MASK;
        public static readonly T DIGIT_MASK;
        public static readonly T LEAST_SIGNIFICANT_DIGIT_MASK = (T.One << SPACING) - T.One;

        static WideBits() {
            var totalWidth = int.CreateTruncating(T.LeadingZeroCount(T.Zero));
            var width = totalWidth - totalWidth % SPACING;
            PARITY_MASK = T.One;
            for (var i = SPACING; i < width; i += SPACING)
                PARITY_MASK |= T.One << i;

            BORROW_MASK = PARITY_MASK << SPACING - 1;
            DIGIT_MASK = BORROW_MASK - PARITY_MASK;

            if (typeof(T) == typeof(UInt128)) {
                LOW_PARITY = ulong.CreateTruncating(PARITY_MASK);
                HIGH_PARITY = ulong.CreateTruncating(PARITY_MASK >>> 64);
            }
        }

        public static T Expand(ReadOnlySpan<int> ints) {
            var res = T.Zero;
            var max = (1 << SPACING - 1) - 1;
            for (var i = ints.Length - 1; i >= 0; --i) {
                Debug.Assert(ints[i] <= max);
                res = res << SPACING | T.CreateTruncating(ints[i]);
            }
            return res;
        }

        public static T Expand(uint n) {
            if (typeof(T) == typeof(ulong))
                return T.CreateTruncating(Bmi2.X64.ParallelBitDeposit(n, ulong.CreateTruncating(PARITY_MASK)));
            else if (typeof(T) == typeof(UInt128)) {
                UInt128 value = new(Bmi2.X64.ParallelBitDeposit(n >>> 7, HIGH_PARITY), Bmi2.X64.ParallelBitDeposit(n, LOW_PARITY));
                return T.CreateTruncating(value);
            }

            var res = T.Zero;
            for (; n != 0; n = Bmi1.ResetLowestSetBit(n)) {
                var tzc = Bmi1.TrailingZeroCount(n);
                res |= T.One << (int)tzc * SPACING;
            }
            return res;
        }

        public static uint CompressParity(T n) {
            n &= PARITY_MASK;
            if (typeof(T) == typeof(ulong))
                return (uint)Bmi2.X64.ParallelBitExtract(ulong.CreateTruncating(n), ulong.CreateTruncating(PARITY_MASK));
            else if (typeof(T) == typeof(UInt128)) {
                var high = Bmi2.X64.ParallelBitExtract(ulong.CreateTruncating(n >>> 64), HIGH_PARITY) << 7;
                var low = Bmi2.X64.ParallelBitExtract(ulong.CreateTruncating(n), LOW_PARITY);
                return (uint)(high | low);
            }

            var res = 0U;
            for (; n != T.Zero; n &= n - T.One) {
                var tzc = T.TrailingZeroCount(n);
                res |= 1U << int.CreateTruncating(tzc) / SPACING;
            }
            return res;
        }

        [SkipLocalsInit]
        public static string GetString(T n, int digitCount = 0) {
            Span<char> buffer = stackalloc char[40];
            if (digitCount == 0)
                digitCount = Math.Max(1, (int.CreateTruncating(T.Log2(n)) + (SPACING - 1)) / SPACING);

            var ten = T.CreateTruncating(10);
            var write = buffer.Length;
            for (var i = digitCount - 1; i >= 0; --i) {
                var digitBits = n >>> i * SPACING & LEAST_SIGNIFICANT_DIGIT_MASK;
                if (digitBits == T.Zero) {
                    buffer[--write] = '0';
                    buffer[--write] = ',';
                    continue;
                }

                while (digitBits != T.Zero) {
                    (digitBits, var digit) = T.DivRem(digitBits, ten);
                    buffer[--write] = (char)(uint.CreateTruncating(digit) | '0');
                }
                buffer[--write] = ',';
            }

            return new(buffer[(write + 1)..]);
        }
    }

    private readonly uint minPressesLightsSum;
    private readonly uint minPressesJoltagesSum;

    [SkipLocalsInit]
    public Day10() {
        Span<int> buttonBuffer = stackalloc int[15];
        buttonBuffer.Clear();
        Span<int> joltageBuffer = stackalloc int[15];
        joltageBuffer.Clear();
        Span<char> newLineDump = stackalloc char[Environment.NewLine.Length];

        using var reader = GetDataReader();
        while (!reader.EndOfStream) {
            var desired = 0U;

            var ch = reader.Read();
            Debug.Assert(ch == '[');
            for (var i = 0; reader.Peek() != ']'; ++i)
                desired |= ((uint)reader.Read() & 1) << i;

            reader.Read();
            ch = reader.Read();
            Debug.Assert(ch == ' ');
            var buttons = ReadButtons(reader, buttonBuffer);
            var joltages = ReadJoltages(reader, joltageBuffer);
            reader.Read(newLineDump);

            var (joltageMin, lightsMin) = joltages.Length switch {
                <= 6 => ShortestPathJoltages<ulong>(joltages, buttons, desired),
                _ => ShortestPathJoltages<UInt128>(joltages, buttons, desired),
            };
            minPressesJoltagesSum += joltageMin;
            minPressesLightsSum += lightsMin;

            buttons.Clear();
            joltages.Clear();
        }
    }

    private static Span<int> ReadJoltages(StreamReader reader, Span<int> buffer) {
        var ch = reader.Read();
        Debug.Assert(ch == '{');

        var idx = 0;
        for (; reader.Peek() is not (-1 or '\r' or '\n'); ++idx)
            buffer[idx] = reader.ReadNextInt();
        return buffer.Slice(0, idx);
    }

    private static Span<int> ReadButtons(StreamReader reader, Span<int> buffer) {
        var idx = 0;
        for (; reader.Peek() == '('; ++idx) {
            reader.Read();
            ref var n = ref buffer[idx];
            while (reader.Peek() != ' ') {
                n |= 1 << (reader.Read() & 0xF);
                var ch = reader.Read();
                Debug.Assert(ch is ',' or ')');
            }
            reader.Read();
        }
        return buffer.Slice(0, idx);
    }

    [SkipLocalsInit]
    private static (uint joltage, uint lights) ShortestPathJoltages<T>(ReadOnlySpan<int> joltages, ReadOnlySpan<int> buttons, uint lights) where T : unmanaged, IBinaryInteger<T> {
        Span<T> wideButtons = stackalloc T[buttons.Length];
        for (var i = 0; i < buttons.Length; ++i) {
            wideButtons[i] = WideBits<T>.Expand((uint)buttons[i]);
            Debug.Assert((uint)buttons[i] == WideBits<T>.CompressParity(wideButtons[i]));
        }

        var maskEnd = 1 << wideButtons.Length;
        Span<T> combs = stackalloc T[maskEnd];
        combs[0] = T.Zero;
        var parityMap = new List<uint>[1 << joltages.Length];
        (parityMap[0] = []).Add(0);
        var minLights = uint.MaxValue;

        for (var mask = 1U; mask < maskEnd; ++mask) {
            var log = BitOperations.Log2(mask);
            var msb = 1U << log;
            var val = combs[(int)mask] = combs[(int)(mask ^ msb)] + wideButtons[log];
            Debug.Assert((val & WideBits<T>.BORROW_MASK) == T.Zero);
            var valParity = WideBits<T>.CompressParity(val);
            Debug.Assert(uint.CreateChecked(T.PopCount(WideBits<T>.PARITY_MASK & val)) == Popcnt.PopCount(valParity));
            (parityMap[valParity] ??= []).Add(mask);
            if (valParity == lights)
                minLights = Math.Min(minLights, Popcnt.PopCount(mask));
        }

        var requirements = WideBits<T>.Expand(joltages);
        return (DFS(combs, requirements, parityMap, []), minLights);
    }

    private static uint DFS<T>(ReadOnlySpan<T> combs, T requirements, List<uint>[] parityMap, Dictionary<T, uint> memo) where T : unmanaged, IBinaryInteger<T> {
        if (memo.TryGetValue(requirements, out var cached))
            return cached;

        Debug.Assert((requirements & WideBits<T>.BORROW_MASK) == T.Zero);
        if (requirements == T.Zero)
            return 0;

        var res = uint.MaxValue >>> 2;
        var parity = WideBits<T>.CompressParity(requirements);
        var lst = parityMap[parity];
        if (lst is null)
            return res;

        for (var i = 0; i < lst.Count & res > 0; ++i) {
            var p = lst[i];
            var left = requirements - combs[(int)p];
            if ((left & WideBits<T>.BORROW_MASK) != T.Zero)
                continue;

            Debug.Assert((left & WideBits<T>.PARITY_MASK) == T.Zero);
            res = Math.Min(res, Popcnt.PopCount(p) + (DFS(combs, left >>> 1, parityMap, memo) << 1));
        }
        return memo[requirements] = res;
    }

    [SkipLocalsInit, Obsolete("Obsolete for part 1, but leaving for posterity since it's very efficient and I think it's neat")]
    private static int ShortestPathLights(int desired, ReadOnlySpan<int> buttons) {
        var end = 1 << buttons.Length;
        var cache = stackalloc uint[end];
        *cache = (uint)desired;
        for (var i = 0; true; ++i) {
            for (var mask = Bmi1.GetMaskUpToLowestSetBit(1U << i); mask < end; mask = mask.GetNextWithSamePopulation()) {
                var buttonIdx = (int)Bmi1.TrailingZeroCount(mask);
                var val = cache[mask] = cache[Bmi1.ResetLowestSetBit(mask)] ^ (uint)buttons[buttonIdx];
                if (val == 0)
                    return (int)Popcnt.PopCount(mask);
            }
        }
    }

    public override object? Part1() {
        Print("The sum of required steps for lights is: {0}", minPressesLightsSum);
        return Box<uint>.Instance(minPressesLightsSum);
    }

    public override object? Part2() {
        Print("The sum of required steps for joltages is: {0}", minPressesJoltagesSum);
        return Box<uint>.Instance(minPressesJoltagesSum);
    }
}
