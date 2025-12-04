using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Advent2025;

internal static class SpanExtensions {

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Mask<T>(this Span<T> span, T mask) where T : struct, IBitwiseOperators<T, T, T> {
        Unsafe.SkipInit(out Vector<T> vec);
        Unsafe.SkipInit(out Vector<T> vec2);
        Vector<T> maskVec = new(mask);
        
        var bytes = MemoryMarshal.AsBytes(span);
        var remaining = bytes.Length;
        ref var first = ref bytes[0];
        for (var i = 0; remaining >= Vector<T>.Count * 2; i += Vector<T>.Count * 2, remaining -= Vector<T>.Count * 2) {
            ref var curr = ref Unsafe.Add(ref first, i);
            ref var second = ref Unsafe.Add(ref curr, Vector<T>.Count);
            vec = Unsafe.ReadUnaligned<Vector<T>>(ref curr);
            vec2 = Unsafe.ReadUnaligned<Vector<T>>(ref second);

            Unsafe.WriteUnaligned(ref curr, vec & maskVec);
            Unsafe.WriteUnaligned(ref second, vec2 & maskVec);
        }

        for (var i = span.Length - remaining; i < span.Length; ++i)
            span[i] &= mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RightShift<T>(this Span<T> span, int shiftBy) where T : struct, IShiftOperators<T, int, T>, IBitwiseOperators<T, T, T> {
        Unsafe.SkipInit(out Vector<T> vec);
        Unsafe.SkipInit(out Vector<T> vec2);

        var bytes = MemoryMarshal.AsBytes(span);
        var remaining = bytes.Length;
        ref var first = ref bytes[0];
        var doubleCount = Vector<T>.Count * 2;
        for (var i = 0; remaining >= doubleCount; i += doubleCount, remaining -= doubleCount) {
            ref var curr = ref Unsafe.Add(ref first, i);
            ref var second = ref Unsafe.Add(ref curr, Vector<T>.Count);
            vec = Unsafe.ReadUnaligned<Vector<T>>(ref curr);
            vec2 = Unsafe.ReadUnaligned<Vector<T>>(ref second);

            Unsafe.WriteUnaligned(ref curr, vec >> shiftBy);
            Unsafe.WriteUnaligned(ref second, vec2 >> shiftBy);
        }

        for (var i = span.Length - remaining; i < span.Length; ++i)
            span[i] >>= shiftBy;
    }
}
