using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Advent2025.Day10;

internal readonly unsafe ref struct SixteenSegmentedMemory {
    private const int MAX_JOLTAGE = 10;
    private const int MAX_SIZE = 1 << MAX_JOLTAGE;
    private const int ENDS_PADDING_INT_COUNT = 64 / sizeof(uint);

    [ThreadStatic]
    private static uint* baseAddr;
    private readonly uint* lens;
    private readonly uint* data;

    public SixteenSegmentedMemory(int len) {
        if (baseAddr is null)
            baseAddr = (uint*)NativeMemory.AlignedAlloc(((MAX_SIZE << 4) + MAX_SIZE + ENDS_PADDING_INT_COUNT * 2) * sizeof(uint), sizeof(uint));

        lens = baseAddr + ENDS_PADDING_INT_COUNT;
        data = lens + MAX_SIZE;
        NativeMemory.Clear(lens, (nuint)(len * sizeof(uint)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(uint idx, uint val) => data[idx << 4 | lens[idx]++] = val;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<uint> AsSpan(int idx) => new(data + (idx << 4), (int)lens[idx]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FreeMemory() {
        if (baseAddr is not null) {
            NativeMemory.AlignedFree(baseAddr);
            baseAddr = null;
        }
    }
}

internal unsafe sealed class Day10 : DayBase {
    const int SIZE = 193;

    private static readonly int[] buttonBuffer = new int[SIZE * 10];
    private static readonly int[] joltageBuffer = new int[SIZE * 10];
    private static readonly (ReadOnlyMemory<int> buttons, ReadOnlyMemory<int> joltages, uint desired)[] inputBuffer = new (ReadOnlyMemory<int>, ReadOnlyMemory<int>, uint)[SIZE];

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
        Parallel.ForEach(inputBuffer, new() { MaxDegreeOfParallelism = -1 }, () => (0U, 0U), (input, _, _, acc) => {
            var (buttons, joltages, desired) = input;
            var (joltage, lights) = joltages.Length switch {
                <= 6 => ShortestPathJoltages<ulong>(joltages, buttons, desired),
                _ => ShortestPathJoltages<UInt128>(joltages, buttons, desired),
            };
            return (acc.Item1 + joltage, acc.Item2 + lights);
        }, results => {
            Interlocked.Add(ref localJoltageSum, results.Item1);
            Interlocked.Add(ref localLightsSum, results.Item2);
            SixteenSegmentedMemory.FreeMemory();
        });

        minPressesJoltagesSum = localJoltageSum;
        minPressesLightsSum = localLightsSum;
    }

    private static ReadOnlyMemory<int> ReadJoltages(StreamReader reader, Memory<int> buffer) {
        var ch = reader.Read();
        Debug.Assert(ch == '{');

        var idx = 0;
        var span = buffer.Span;
        for (; reader.Peek() is not (-1 or '\r' or '\n'); ++idx)
            span[idx] = reader.ReadNextInt();
        return buffer.Slice(0, idx);
    }

    private static ReadOnlyMemory<int> ReadButtons(StreamReader reader, Memory<int> buffer) {
        var idx = 0;
        var span = buffer.Span;
        for (; reader.Peek() == '('; ++idx) {
            reader.Read();
            ref var n = ref span[idx];
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
        SixteenSegmentedMemory parityMap = new(1 << joltages.Length);
        parityMap.Add(0, 0);
        var minLights = uint.MaxValue >>> 2;

        for (uint msb = 1, log = 0; msb < maskEnd; msb <<= 1, ++log) {
            for (var lowBits = 0U; lowBits < msb; ++lowBits) {
                var bits = msb | lowBits;
                var val = combs[bits] = combs[lowBits] + wideButtons[log];
                Debug.Assert((val & SWARHelper<T>.BORROW_MASK) == T.Zero);
                var valParity = SWARHelper<T>.CompressParity(val);
                Debug.Assert(uint.CreateChecked(T.PopCount(SWARHelper<T>.PARITY_MASK & val)) == Popcnt.PopCount(valParity));
                parityMap.Add(valParity, bits);
                if (valParity == lights) {
                    var minSteps = MathUtils.Min(minLights, Popcnt.PopCount(bits));
                    Debug.Assert(minSteps <= minLights & minSteps <= Popcnt.PopCount(bits));
                    minLights = minSteps;
                }
            }
        }
        var requirements = SWARHelper<T>.Expand(joltages.Span);
        return (DFS(combs, requirements, parityMap, []), minLights);
    }

    private static uint DFS<T>(T* combs, T requirements, in SixteenSegmentedMemory parityMap, Dictionary<T, uint> memo) where T : unmanaged, IBinaryInteger<T> {
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
            res = MathUtils.Min(res, Popcnt.PopCount(p) + (DFS(combs, left >>> 1, parityMap, memo) << 1));
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
