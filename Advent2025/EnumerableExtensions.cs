using System;
using System.Collections.Generic;

namespace Advent2025;

public static class EnumerableExtensions {
    public static IEnumerable<T> PerformOnFirst<T>(this IEnumerable<T> enumerable, Action<T> action) {
        using var enumerator = enumerable.GetEnumerator();
        if (!enumerator.MoveNext())
            yield break;
        action(enumerator.Current);
        yield return enumerator.Current;
        while (enumerator.MoveNext())
            yield return enumerator.Current;
    }
}
