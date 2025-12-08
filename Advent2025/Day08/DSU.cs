using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Advent2025.Day08;

public readonly unsafe ref struct DSU(ushort* parentsBuffer, ushort* countsBuffer, int size) {
    private readonly ushort* parents = FillIdxs(parentsBuffer, size);
    private readonly ushort* counts = Fill(countsBuffer, size, 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort* FillIdxs(ushort* mem, int count) {
        Debug.Assert(((nuint)mem & 31) == 0, "mem should be 32-byte aligned");
        Span<ushort> test = new(mem, count);
        var idxs = mem;
        var end = idxs + count;
        var idxReg = Vector256<ushort>.Indices;
        var step = Vector256.Create((ushort)Vector256<ushort>.Count);
        for (; idxs < end; idxs += Vector256<ushort>.Count, idxReg += step)
            idxReg.StoreAligned(idxs);
        return mem;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort* Fill(ushort* mem, int count, ushort val) {
        Debug.Assert(((nuint)mem & 31) == 0, "mem should be 32-byte aligned");

        var vals = mem;
        var end = vals + count;
        var idxReg = Vector256.Create(val);
        for (; vals < end; vals += Vector256<ushort>.Count)
            idxReg.StoreAligned(vals);
        return mem;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Find(ushort val) {
        while (parents[val] != val)
            (val, parents[val]) = (parents[val], parents[parents[val]]);
        return val;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Union(ushort a, ushort b) {
        (a, b) = (Find(a), Find(b));
        if (a == b)
            return counts[a];
        parents[b] = a;
        return counts[a] += counts[b];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetCount(ushort val) => counts[Find(val)];
}