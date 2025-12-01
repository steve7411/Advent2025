using System.Numerics;
using System.Runtime.CompilerServices;

namespace Advent2025;

internal class BitReverser<T> where T : unmanaged, IBinaryInteger<T> {
    private static T[]? groupMasks = default;
    private static readonly int bitSize = Unsafe.SizeOf<T>() * 8;

    static T[] GroupMasks => groupMasks ??= BuildMasks();

    private static T[] BuildMasks() {
        var evenMaskCount = int.TrailingZeroCount(bitSize);
        var masks = new T[evenMaskCount << 1];

        for (var i = 0; i < masks.Length; i += 2) {
            var bitWidth = 1 << (i >> 1);
            masks[i + 1] = ~(masks[i] = BuildAlternatingMask(bitWidth));
        }
        return masks;
    }

    private static T BuildAlternatingMask(int bitWidth) {
        var mask = (T.One << bitWidth) - T.One;
        for (var i = bitWidth << 1; i < bitSize; i <<= 1)
            mask |= mask << i;
        return mask;
    }

    public static T ReverseBits(in T bits) {
        var copy = bits;
        var masks = GroupMasks;
        for (var i = 0; i < masks.Length - 1; i += 2) {
            var shift = 1 << (i >> 1);
            copy = (copy & masks[i]) << shift | (copy & masks[i + 1]) >> shift;
        }
        return copy;
    }
}
