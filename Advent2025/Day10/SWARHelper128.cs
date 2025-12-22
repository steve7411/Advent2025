using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Advent2025.Day10;

internal class SWARHelper128 : ISWARHelpwer<UInt128> {
    private static readonly ulong LOW_PARITY;
    private static readonly ulong HIGH_PARITY;

    public static readonly UInt128 BORROW_MASK;
    public static readonly UInt128 PARITY_MASK;
    public static readonly UInt128 DIGIT_MASK;
    public static readonly UInt128 LEAST_SIGNIFICANT_DIGIT_MASK = ((UInt128)1 << ISWARHelpwer<UInt128>.SPACING) - UInt128.One;

    static UInt128 ISWARHelpwer<UInt128>.BORROW_MASK => BORROW_MASK;

    static unsafe SWARHelper128() {
        const int TOTAL_WIDTH = 128;
        var width = TOTAL_WIDTH - TOTAL_WIDTH % ISWARHelpwer<UInt128>.SPACING;
        PARITY_MASK = UInt128.One;
        for (var i = ISWARHelpwer<UInt128>.SPACING; i < width; i += ISWARHelpwer<UInt128>.SPACING)
            PARITY_MASK |= UInt128.One << i;

        BORROW_MASK = PARITY_MASK << ISWARHelpwer<UInt128>.SPACING - 1;
        DIGIT_MASK = BORROW_MASK - PARITY_MASK;

        LOW_PARITY = ulong.CreateTruncating(PARITY_MASK);

        Debug.Assert(BitConverter.IsLittleEndian);
        var parity = PARITY_MASK;
        HIGH_PARITY = ((ulong*)&parity)[1];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt128 Expand(ReadOnlySpan<int> ints) {
        UInt128 res = 0;
        for (var i = ints.Length - 1; i >= 0; --i) {
            Debug.Assert(ints[i] <= (1 << ISWARHelpwer<UInt128>.SPACING - 1) - 1);
            res = res << ISWARHelpwer<UInt128>.SPACING | (UInt128)(uint)ints[i];
        }
        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt128 Expand(uint n) {
        var low = Bmi2.X64.ParallelBitDeposit(n, LOW_PARITY);
        return new(Bmi2.X64.ParallelBitDeposit(n >>> 7, HIGH_PARITY), low);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe uint CompressParity(in UInt128 n) {
        var lower = (ulong*)Unsafe.AsPointer(in n);
        var low = Bmi2.X64.ParallelBitExtract(*lower, LOW_PARITY);
        var high = Bmi2.X64.ParallelBitExtract(lower[1], HIGH_PARITY) << 7;
        return (uint)(high | low);
    }
}
