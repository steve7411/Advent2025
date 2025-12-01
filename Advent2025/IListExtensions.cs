using System.Collections.Generic;

namespace Advent2025;

public static class IListExtensions {
    public static T AggregateRight<T>(this IReadOnlyList<T> list, Func<T, T, T> func) {
        // We'll just crash in natural ways if we call this with garbage
        //ArgumentNullException.ThrowIfNull(list);
        //ArgumentOutOfRangeException.ThrowIfZero(list.Count);
        
        var accumulator = list[^1];
        for (var i = list.Count - 2; i >= 0; --i)
            accumulator = func(accumulator, list[i]);
        return accumulator;
    }

    public static TAccumulate AggregateRight<TSource, TAccumulate>(this IReadOnlyList<TSource> list, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func) {
        // We'll just crash in natural ways if we call this with garbage
        //ArgumentNullException.ThrowIfNull(list);
        //ArgumentOutOfRangeException.ThrowIfZero(list.Count);

        var accumulator = seed;
        for (var i = list.Count - 1; i >= 0; --i)
            accumulator = func(accumulator, list[i]);
        return accumulator;
    }
}
