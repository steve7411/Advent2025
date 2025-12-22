using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Advent2025.Day10;

internal class SWARHelper64 : ISWARHelper<ulong> {
    public static readonly ulong BORROW_MASK;
    public static readonly ulong PARITY_MASK;
    public static readonly ulong DIGIT_MASK;
    public static readonly ulong LEAST_SIGNIFICANT_DIGIT_MASK = (1UL << ISWARHelper<ulong>.SPACING) - 1;

    static ulong ISWARHelper<ulong>.BORROW_MASK => BORROW_MASK;

    static SWARHelper64() {
        const int TOTAL_WIDTH = 64;
        var width = TOTAL_WIDTH - TOTAL_WIDTH % ISWARHelper<ulong>.SPACING;
        PARITY_MASK = 1UL;
        for (var i = ISWARHelper<ulong>.SPACING; i < width; i += ISWARHelper<ulong>.SPACING)
            PARITY_MASK |= (ulong)1 << i;

        BORROW_MASK = PARITY_MASK << ISWARHelper<ulong>.SPACING - 1;
        DIGIT_MASK = BORROW_MASK - PARITY_MASK;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Expand(ReadOnlySpan<int> ints) {
        var res = 0UL;
        for (var i = ints.Length - 1; i >= 0; --i) {
            Debug.Assert(ints[i] <= (1 << ISWARHelper<ulong>.SPACING - 1) - 1);
            res = res << ISWARHelper<ulong>.SPACING | (ulong)(uint)ints[i];
        }
        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Expand(uint n) => Bmi2.X64.ParallelBitDeposit(n, PARITY_MASK);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint CompressParity(in ulong n) => (uint)Bmi2.X64.ParallelBitExtract(n, PARITY_MASK);
}
