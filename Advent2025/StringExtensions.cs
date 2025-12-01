using System;
using System.Globalization;

namespace Advent2025;

public static class StringExtensions {
    public static (T number, int endIndex) ReadNumber<T>(this string input, int startIndex) where T : ISpanParsable<T> =>
        (input.ReadNumber<T>(startIndex, out var endIndex), endIndex);

    public static T ReadNumber<T>(this string input, int startIndex, out int endIndex) where T : ISpanParsable<T> {
        endIndex = startIndex;
        while (++endIndex < input.Length && (input[endIndex] is >= '0' and <= '9' || startIndex == endIndex && input[endIndex] is '-' or '+'));
        return T.Parse(input.AsSpan()[startIndex..endIndex--], CultureInfo.InvariantCulture.NumberFormat);
    }

    public static T ReadNumber<T>(this string input, ref int startIndex) where T : ISpanParsable<T> {
        var endIndex = startIndex;
        while (++endIndex < input.Length && (input[endIndex] is >= '0' and <= '9' || startIndex == endIndex && input[endIndex] is '-' or '+')) ;
        var result = T.Parse(input.AsSpan()[startIndex..endIndex--], CultureInfo.InvariantCulture.NumberFormat);
        startIndex = endIndex - 1;
        return result;
    }
}
