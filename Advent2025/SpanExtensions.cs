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
        
        var remaining = span.Length;
        ref var first = ref span[0];
        var doubleCount = Vector<T>.Count * 2;
        for (var i = 0; remaining >= doubleCount; i += doubleCount, remaining -= doubleCount) {
            ref var curr = ref Unsafe.Add(ref first, i);
            ref var second = ref Unsafe.Add(ref curr, Vector<T>.Count);
            vec = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref curr));
            vec2 = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref second));

            (vec & maskVec).StoreUnsafe(ref curr);
            (vec2 & maskVec).StoreUnsafe(ref second);
        }

        for (var i = span.Length - remaining; i < span.Length; ++i)
            span[i] &= mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RightShift<T>(this Span<T> span, int shiftBy) where T : struct, IShiftOperators<T, int, T> {
        Unsafe.SkipInit(out Vector<T> vec);
        Unsafe.SkipInit(out Vector<T> vec2);

        var remaining = span.Length;
        var doubleCount = Vector<T>.Count * 2;
        ref var first = ref span[0];
        for (var i = 0; remaining >= doubleCount; i += doubleCount, remaining -= doubleCount) {
            ref var curr = ref Unsafe.Add(ref first, i);
            ref var second = ref Unsafe.Add(ref curr, Vector<T>.Count);
            vec = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref curr));
            vec2 = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref second));

            (vec >> shiftBy).StoreUnsafe(ref curr);
            (vec2 >> shiftBy).StoreUnsafe(ref second);
        }

        for (var i = span.Length - remaining; i < span.Length; ++i)
            span[i] >>= shiftBy;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Sum<T>(this Span<T> span) where T : unmanaged, IAdditionOperators<T, T, T> {
        Unsafe.SkipInit(out Vector<T> lhs);
        Unsafe.SkipInit(out Vector<T> rhs);

        var remaining = span.Length;
        ref var first = ref span[0];
        var doubleCount = Vector<T>.Count * 2;
        var sumVec = Vector<T>.Zero;
        for (var i = 0; remaining >= doubleCount; i += doubleCount, remaining -= doubleCount) {
            ref var curr = ref Unsafe.Add<T>(ref first, i);
            ref var second = ref Unsafe.Add(ref curr, Vector<T>.Count);
            lhs = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref curr));
            rhs = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref second));
            sumVec += lhs + rhs;
        }
        
        var sum = default(T);
        for (var i = span.Length - remaining; i < span.Length; ++i)
            sum += span[i];

        Span<T> buffer = stackalloc T[Vector<T>.Count];
        sumVec.StoreUnsafe(ref buffer[0]);
        foreach (var n in buffer)
            sum += n;
        return sum;
    }
}
