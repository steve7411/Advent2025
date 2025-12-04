using System.Runtime.CompilerServices;

namespace Advent2025;

// EXTREMELY not thread safe!
internal static class Box<T> where T : struct {
    private static readonly object instance = default(T);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Instance(in T toBox) {
        Unsafe.Unbox<T>(instance) = toBox;
        return instance;
    }
}
