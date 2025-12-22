using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Advent2025.Day10;

//internal readonly unsafe ref struct SixteenSegmentedDictionary<T> : IDisposable where T : unmanaged {
//    //private const int MAX_JOLTAGE = 10;
//    //private const int MAX_SIZE = 1 << MAX_JOLTAGE;
//    private const int ENDS_PADDING_BYTES = 64;

//    private readonly byte* lensBase;
//    private readonly byte* lens;
//    private readonly T* dataBase;
//    private readonly T* data;
//    private readonly int len;

//    public SixteenSegmentedDictionary(int len) {
//        this.len = len;

//        lensBase = (byte*)NativeMemory.AllocZeroed((nuint)(len + ENDS_PADDING_BYTES * 2));
//        lens = lensBase + ENDS_PADDING_BYTES;

//        var dataEndPadding = (ENDS_PADDING_BYTES + sizeof(T) - 1) / sizeof(T);
//        var dataBase = (T*)NativeMemory.AlignedAlloc((nuint)(((len << 4) + dataEndPadding * 2) * sizeof(T)), (nuint)sizeof(T));
//        data = dataBase + dataEndPadding;

//    }

//    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
//    //public readonly void Add(uint idx, ushort val) {
//    //    Debug.Assert(lens[idx] < 16);
//    //    data[idx << 4 | lens[idx]++] = val;
//    //}
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public bool Find(in T val) {

//    }

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public void Clear() => NativeMemory.Clear(lens, (nuint)len);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public void Dispose() {
//        NativeMemory.Free(lensBase);
//        NativeMemory.AlignedFree(dataBase);
//    }
//}

internal readonly unsafe ref struct SixteenSegmentedMemory {
    private const int MAX_JOLTAGE = 10;
    private const int MAX_SIZE = 1 << MAX_JOLTAGE;
    private const int ENDS_PADDING_COUNT = 64 / sizeof(ushort);

    [ThreadStatic]
    private static ushort* baseAddr;
    private readonly ushort* lens;
    private readonly ushort* data;

    public SixteenSegmentedMemory(int len) {
        if (baseAddr is null)
            baseAddr = (ushort*)NativeMemory.AlignedAlloc(((MAX_SIZE << 4) + MAX_SIZE + ENDS_PADDING_COUNT * 2) * sizeof(ushort), sizeof(ushort));

        lens = baseAddr + ENDS_PADDING_COUNT;
        data = lens + len;
        NativeMemory.Clear(lens, (nuint)(len * sizeof(ushort)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Add(uint idx, ushort val) {
        Debug.Assert(lens[idx] < 16);
        data[idx << 4 | lens[idx]++] = val;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<ushort> AsSpan(int idx) => new(data + (idx << 4), lens[idx]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ushort* GetSegment(int idx, out ushort* end) {
        var start = data + (idx << 4);
        end = start + lens[idx];
        return start;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FreeMemory() {
        if (baseAddr is not null) {
            NativeMemory.AlignedFree(baseAddr);
            baseAddr = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsAny(int idx) => lens[idx] > 0;
}
