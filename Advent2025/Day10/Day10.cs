using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Advent2025.Day10;

internal unsafe sealed class Day10 : DayBase {
    private readonly ref struct SixteenSegmented {
        private const int MAX_SIZE = 1 << 10;

        [ThreadStatic]
        private static uint* buffer;
        private readonly uint* lensStart;

        public SixteenSegmented(int len) {
            if (buffer is null)
                buffer = (uint*)NativeMemory.AlignedAlloc(((MAX_SIZE << 4) + MAX_SIZE) * sizeof(uint), sizeof(uint));
            lensStart = buffer + (len << 4);
            NativeMemory.Clear(lensStart, (nuint)(len * sizeof(uint)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(uint idx, uint val) => buffer[idx << 4 | lensStart[idx]++] = val;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<uint> AsSpan(int idx) => new(buffer + (idx << 4), (int)lensStart[idx]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FreeMemory() {
            if (buffer is null)
                return;
            NativeMemory.AlignedFree(buffer);
            buffer = null;
        }
    }


    const int SIZE = 193;

    private static readonly int[] buttonBuffer = new int[SIZE * 10];
    private static readonly int[] joltageBuffer = new int[SIZE * 10];
    private static readonly (Memory<int> buttons, Memory<int> joltages, uint desired)[] inputBuffer = new (Memory<int>, Memory<int>, uint)[SIZE];

    private readonly uint minPressesLightsSum;
    private readonly uint minPressesJoltagesSum;

    [SkipLocalsInit]
    public Day10() {
        Span<char> newLineDump = stackalloc char[Environment.NewLine.Length];

        using var reader = GetDataReader();
        for (int buttonsIdx = 0, joltageIdx = 0, line = 0; !reader.EndOfStream; ++line) {
            var desired = 0U;

            var ch = reader.Read();
            Debug.Assert(ch == '[');
            for (var i = 0; reader.Peek() != ']'; ++i)
                desired |= ((uint)reader.Read() & 1) << i;

            reader.Read();
            ch = reader.Read();
            Debug.Assert(ch == ' ');
            var buttons = ReadButtons(reader, buttonBuffer.AsMemory(buttonsIdx));
            var joltages = ReadJoltages(reader, joltageBuffer.AsMemory(joltageIdx));
            reader.Read(newLineDump);

            inputBuffer[line] = (buttons, joltages, desired);
            buttonsIdx += buttons.Length;
            joltageIdx += joltages.Length;
        }

        var (localJoltageSum, localLightsSum) = (0U, 0U);
        Parallel.ForEach(inputBuffer, () => (0U, 0U), (input, _, _, acc) => {
            var (buttons, joltages, desired) = input;
            var (joltage, lights) = joltages.Length switch {
                <= 6 => ShortestPathJoltages<ulong>(joltages, buttons, desired),
                _ => ShortestPathJoltages<UInt128>(joltages, buttons, desired),
            };
            return (acc.Item1 + joltage, acc.Item2 + lights);
        }, results => {
            Interlocked.Add(ref localJoltageSum, results.Item1);
            Interlocked.Add(ref localLightsSum, results.Item2);
            SixteenSegmented.FreeMemory();
        });

        minPressesJoltagesSum = localJoltageSum;
        minPressesLightsSum = localLightsSum;
    }

    private static Memory<int> ReadJoltages(StreamReader reader, Memory<int> buffer) {
        var ch = reader.Read();
        Debug.Assert(ch == '{');

        var idx = 0;
        var span = buffer.Span;
        for (; reader.Peek() is not (-1 or '\r' or '\n'); ++idx)
            span[idx] = reader.ReadNextInt();
        return buffer.Slice(0, idx);
    }

    private static Memory<int> ReadButtons(StreamReader reader, Memory<int> buffer) {
        var idx = 0;
        for (; reader.Peek() == '('; ++idx) {
            reader.Read();
            ref var n = ref buffer.Span[idx];
            for (n = 0; reader.Peek() != ' ';) {
                n |= 1 << (reader.Read() & 0xF);
                var ch = reader.Read();
                Debug.Assert(ch is ',' or ')');
            }
            reader.Read();
        }
        return buffer.Slice(0, idx);
    }

    [SkipLocalsInit]
    private static (uint joltage, uint lights) ShortestPathJoltages<T>(ReadOnlyMemory<int> joltages, ReadOnlyMemory<int> buttons, uint lights) where T : unmanaged, IBinaryInteger<T> {
        var wideButtons = stackalloc T[buttons.Length];
        var buttonSpan = buttons.Span;
        for (var i = 0; i < buttons.Length; ++i) {
            wideButtons[i] = SWARHelper<T>.Expand((uint)buttonSpan[i]);
            Debug.Assert((uint)buttonSpan[i] == SWARHelper<T>.CompressParity(wideButtons[i]));
        }

        var maskEnd = 1 << buttons.Length;
        var combs = stackalloc T[maskEnd];
        combs[0] = T.Zero;
        SixteenSegmented parityMap = new(1 << joltages.Length);
        parityMap.Add(0, 0);
        var minLights = uint.MaxValue;

        for (var mask = 1U; mask < maskEnd; ++mask) {
            var log = BitOperations.Log2(mask);
            var msb = 1U << log;
            var val = combs[mask] = combs[mask ^ msb] + wideButtons[log];
            Debug.Assert((val & SWARHelper<T>.BORROW_MASK) == T.Zero);
            var valParity = SWARHelper<T>.CompressParity(val);
            Debug.Assert(uint.CreateChecked(T.PopCount(SWARHelper<T>.PARITY_MASK & val)) == Popcnt.PopCount(valParity));
            parityMap.Add(valParity, mask);
            if (valParity == lights)
                minLights = Math.Min(minLights, Popcnt.PopCount(mask));
        }
        var requirements = SWARHelper<T>.Expand(joltages.Span);
        return (DFS(combs, requirements, parityMap, []), minLights);
    }

    private static uint DFS<T>(T* combs, T requirements, in SixteenSegmented parityMap, Dictionary<T, uint> memo) where T : unmanaged, IBinaryInteger<T> {
        if (memo.TryGetValue(requirements, out var cached))
            return cached;

        Debug.Assert((requirements & SWARHelper<T>.BORROW_MASK) == T.Zero);
        if (requirements == T.Zero)
            return 0;

        var res = uint.MaxValue >>> 2;
        var span = parityMap.AsSpan((int)SWARHelper<T>.CompressParity(requirements));
        for (var i = 0; i < span.Length & res > 0; ++i) {
            var p = span[i];
            var left = requirements - combs[(int)p];
            if ((left & SWARHelper<T>.BORROW_MASK) != T.Zero)
                continue;

            Debug.Assert((left & SWARHelper<T>.PARITY_MASK) == T.Zero);
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
