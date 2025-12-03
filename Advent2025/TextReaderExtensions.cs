using System.Buffers;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Advent2025;

public static class TextReaderExtensions {
    private static readonly StringBuilderPool builderPool = StringBuilderPool.Create();

    public static int ReadNextInt(this TextReader r) => r.ReadNextNumber<int>().parsed;

    public static long ReadNextLong(this TextReader r) => r.ReadNextNumber<long>().parsed;

    public static byte ReadNextByte(this TextReader r) => r.ReadNextNumber<byte>().parsed;

    public static (T parsed, int lastRead) ReadNextNumber<T>(this TextReader r) where T : ISpanParsable<T> {
        // Should be enough space for up to long.MinValue
        Span<char> buffer = stackalloc char[20];
        var bufferPos = -1;
        int nextVal = r.ReadToNonWhiteSpace();
        while (nextVal != -1 && !char.IsWhiteSpace((char)nextVal) &&
            (char.IsDigit((char)nextVal) || (bufferPos == -1 && (nextVal is '-' or '+')))) {
            buffer[++bufferPos] = (char)nextVal;
            nextVal = r.Read();
        }
        ConsumeFullNewLine(r, nextVal);
        return (ParseNumber<T>(buffer[..(bufferPos + 1)]), nextVal);
    }

    public static (T parsed, int length, int lastRead) ReadNextNumberWithLen<T>(this TextReader r) where T : ISpanParsable<T> {
        // Should be enough space for up to long.MinValue
        Span<char> buffer = stackalloc char[20];
        var bufferPos = -1;
        int nextVal = r.ReadToNonWhiteSpace();
        while (nextVal != -1 && !char.IsWhiteSpace((char)nextVal) &&
            (char.IsDigit((char)nextVal) || (bufferPos == -1 && (nextVal is '-' or '+')))) {
            buffer[++bufferPos] = (char)nextVal;
            nextVal = r.Read();
        }
        ConsumeFullNewLine(r, nextVal);
        var len = bufferPos + 1;
        return (ParseNumber<T>(buffer[..len]), len, nextVal);
    }

    private static T ParseNumber<T>(ReadOnlySpan<char> chars) where T : ISpanParsable<T> =>
        T.Parse(chars, CultureInfo.InvariantCulture.NumberFormat);

    public static Span<T> Read<T>(this TextReader r, Span<T> buffer) where T : INumber<T> {
        var idx = 0;
        int nextVal;
        while (idx < buffer.Length && (nextVal = r.Read()) != -1)
            buffer[idx++] = T.CreateTruncating(nextVal);
        return buffer[..idx];
    }

    public static int ReadToNonWhiteSpace(this TextReader r) {
        int nextVal;
        while ((nextVal = r.Read()) != -1 && char.IsWhiteSpace((char)nextVal)) ;
        return nextVal;
    }

    public static void SkipToNonWhiteSpaceNoConsume(this TextReader r) {
        int nextVal;
        while ((nextVal = r.Peek()) != -1 && char.IsWhiteSpace((char)nextVal))
            r.Read();
    }

    public static string ReadUntilNoConsume(this TextReader r, params char[] stopBefore) {
        int nextVal;
        var sb = builderPool.Rent();
        while ((nextVal = r.Peek()) != -1 && Array.IndexOf(stopBefore, (char)nextVal) == -1)
            sb.Append((char)r.Read());
        return builderPool.ReturnAndGetString(sb);
    }

    public static string ReadUntilNoConsume(this TextReader r, SearchValues<char> stopBefore) {
        int nextVal;
        var sb = builderPool.Rent();
        while ((nextVal = r.Peek()) != -1 && !stopBefore.Contains((char)nextVal))
            sb.Append((char)r.Read());
        return builderPool.ReturnAndGetString(sb);
    }

    public static int SkipUntil(this TextReader r, int count, char stopAfter) {
        int nextVal;
        while ((nextVal = r.Read()) != -1 && (count -= Convert.ToInt32(nextVal == stopAfter)) > 0) ;
        return nextVal;
    }

    public static int SkipUntil(this TextReader r, char stopAfter) => r.SkipUntil(1, stopAfter);

    public static int SkipUntil(this TextReader r, char stopA, char stopB) => r.SkipUntil(1, stopA, stopB);

    public static int SkipUntil(this TextReader r, params char[] stopAfter) => r.SkipUntil(1, stopAfter);

    public static int SkipUntil(this TextReader r, int count, char stopA, char stopB) {
        int nextVal;
        while ((nextVal = r.Read()) != -1 && (count -= Convert.ToInt32(stopA == nextVal || stopB == nextVal)) > 0) ;
        return nextVal;
    }

    public static int SkipUntil(this TextReader r, int count, params char[] stopAfter) {
        int nextVal;
        while ((nextVal = r.Read()) != -1 && (count -= Convert.ToInt32(Array.IndexOf(stopAfter, (char)nextVal) != -1)) > 0) ;
        return nextVal;
    }

    public static void SkipUntilWhiteSpace(this TextReader r) {
        int nextVal;
        while ((nextVal = r.Read()) != -1 && !char.IsWhiteSpace((char)nextVal)) ;
    }

    public static Span<char> ReadUntil(this TextReader r, char stopAfter, Span<char> buffer) {
        int nextVal;
        var idx = -1;
        while ((nextVal = r.Read()) != -1 && nextVal != stopAfter)
            buffer[++idx] = (char)nextVal;
        return buffer[..(idx + 1)];
    }

    public static string ReadUntil(this TextReader r, char stopAfter) {
        int nextVal;
        var sb = builderPool.Rent();
        while ((nextVal = r.Read()) != -1 && nextVal != stopAfter)
            sb.Append((char)nextVal);
        return builderPool.ReturnAndGetString(sb);
    }

    public static Span<char> ReadUntil(this TextReader r, Span<char> buffer, out int lastRead, char stopAfter) {
        (var position, lastRead) = (0, 0);
        while (position < buffer.Length && (lastRead = r.Read()) != -1 && lastRead != stopAfter)
            buffer[position++] = (char)lastRead;
        return buffer[..position];
    }

    public static Span<T> ReadUntil<T>(this TextReader r, Span<T> buffer, out int lastRead, SearchValues<T> stopAfter) where T : IEquatable<T>, INumber<T> {
        (var position, lastRead) = (0, 0);
        while (position < buffer.Length && (lastRead = r.Read()) != -1 && !stopAfter.Contains(T.CreateTruncating(lastRead)))
            buffer[position++] = T.CreateTruncating(lastRead);
        return buffer[..position];
    }

    public static int ReadUntil(this TextReader r, Span<char> buffer, out int lastRead, params ReadOnlySpan<char> stopAfter) {
        (var position, lastRead) = (0, 0);
        while (position < buffer.Length && (lastRead = r.Read()) != -1 && !stopAfter.Contains((char)lastRead))
            buffer[position++] = (char)lastRead;
        return position;
    }

    public static string ReadUntil(this TextReader r, params ReadOnlySpan<char> stopAfter) => r.ReadUntil(1, stopAfter);
    public static string ReadUntil(this TextReader r, char stopA, char stopB) => r.ReadUntil(1, stopA, stopB);
    public static string ReadUntil(this TextReader r, char stopA, char stopB, char stopC) => r.ReadUntilWithLastRead(1, stopA, stopB, stopC).str;

    public static string ReadUntil(this TextReader r, int count, char stopA, char stopB, char stopC) => r.ReadUntilWithLastRead(count, stopA, stopB, stopC).str;

    public static (string str, int lastRead) ReadUntilWithLastRead(this TextReader r, int count, char stopA, char stopB, char stopC) {
        int nextVal;
        var sb = builderPool.Rent();
        while ((nextVal = r.Read()) != -1 && (count -= Convert.ToInt32(nextVal == stopA || nextVal == stopB || nextVal == stopC)) > 0)
            sb.Append((char)nextVal);
        return (builderPool.ReturnAndGetString(sb), nextVal);
    }

    public static string ReadUntil(this TextReader r, int count, char stopA, char stopB) {
        int nextVal;
        var sb = builderPool.Rent();
        while ((nextVal = r.Read()) != -1 && (count -= Convert.ToInt32(nextVal == stopA || nextVal == stopB)) > 0)
            sb.Append((char)nextVal);
        return builderPool.ReturnAndGetString(sb);
    }

    public static string ReadUntil(this TextReader r, int count, params ReadOnlySpan<char> stopAfter) {
        int nextVal;
        var sb = builderPool.Rent();
        while ((nextVal = r.Read()) != -1 && (count -= Convert.ToInt32(stopAfter.Contains((char)nextVal))) > 0)
            sb.Append((char)nextVal);
        return builderPool.ReturnAndGetString(sb);
    }

    public static string ReadUntilWhiteSpace(this TextReader r) {
        var sb = builderPool.Rent();
        for (var nextVal = r.Read(); nextVal != -1 && !char.IsWhiteSpace((char)nextVal); nextVal = r.Read())
            sb.Append((char)nextVal);
        return builderPool.ReturnAndGetString(sb);
    }

    public static IEnumerable<string> ReadSplitBy(this TextReader t, char separator) {
        while (t.Peek() != -1)
            yield return t.ReadUntil(separator);
    }

    public static Span<T> ReadAllNumbersInLine<T>(this TextReader r, Span<T> buffer) where T : ISpanParsable<T> {
        (T parsed, int lastRead) parseResult;
        var idx = -1;
        while ((parseResult = r.ReadNextNumber<T>()).lastRead is not (-1 or '\r' or '\n'))
            buffer[++idx] = parseResult.parsed;
        buffer[++idx] = parseResult.parsed;
        r.ConsumeFullNewLine(parseResult.lastRead);
        return buffer[..(idx + 1)];
    }

    public static IEnumerable<T> ReadAllNumbersInLine<T>(this TextReader r) where T : ISpanParsable<T> {
        (T parsed, int lastRead) parseResult;
        while ((parseResult = r.ReadNextNumber<T>()).lastRead is not (-1 or '\r' or '\n'))
            yield return parseResult.parsed;
        yield return parseResult.parsed;
        r.ConsumeFullNewLine(parseResult.lastRead);
    }

    public static IEnumerable<IEnumerable<T>> ReadAllNumbersByLine<T>(this TextReader r) where T : ISpanParsable<T> {
        while (r.Peek() != -1)
            yield return ReadAllNumbersInLine<T>(r);
    }

    public static IEnumerable<int> ReadAllInts(this TextReader r) {
        (int parsed, int lastRead) parseResult;
        while ((parseResult = r.ReadNextNumber<int>()).lastRead != -1)
            yield return parseResult.parsed;
        yield return parseResult.parsed;
    }

    public static IEnumerable<long> ReadAllLongs(this TextReader r) {
        (long parsed, int lastRead) parseResult;
        while ((parseResult = r.ReadNextNumber<long>()).lastRead != -1)
            yield return parseResult.parsed;
        yield return parseResult.parsed;
    }

    public static IEnumerable<T> ReadAllParsables<T>(this TextReader r) where T : ISpanParsable<T> {
        (T parsed, int lastRead) parseResult;
        while ((parseResult = r.ReadNextNumber<T>()).lastRead != -1)
            yield return parseResult.parsed;
        yield return parseResult.parsed;
    }

    private static IEnumerable<char> EnumerateLine(this TextReader r) {
        int nextVal;
        while ((nextVal = r.Read()) != -1 && (char)nextVal is not '\n' and not '\r')
            yield return (char)nextVal;
        ConsumeFullNewLine(r, nextVal);
    }

    public static Span<T> ReadLine<T>(this TextReader r, Span<T> buffer) where T : INumber<T> {
        int nextVal;
        int idx = -1;
        while ((nextVal = r.Read()) != -1 && (char)nextVal is not '\n' and not '\r')
            buffer[++idx] = T.CreateTruncating(nextVal);
        ConsumeFullNewLine(r, nextVal);
        return buffer[..(idx + 1)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void ConsumeFullNewLine(this TextReader r, int previousCharacterRead) {
        if (Environment.NewLine.Length > 1 && previousCharacterRead == Environment.NewLine[0]) {
            var newLineIndex = 1;
            while (newLineIndex < Environment.NewLine.Length && r.Peek() == Environment.NewLine[newLineIndex++])
                r.Read();
        }
    }

    public static IEnumerable<IEnumerable<char>> EnumerateAllLines(this TextReader r) {
        while (r.Peek() != -1)
            yield return r.EnumerateLine();
    }

    public static IEnumerable<string> ReadAllLines(this TextReader r) {
        string? current;
        while ((current = r.ReadLine()) != null)
            yield return current;
    }

    public static IEnumerable<string> ReadLinesUntilBlank(this TextReader r) {
        foreach (var line in r.ReadAllLines().TakeWhile(x => !string.IsNullOrWhiteSpace(x)))
            yield return line;
    }

    public static IEnumerable<IEnumerable<char>> EnumerateLinesUntilBlank(this TextReader r) {
        foreach (var line in r.EnumerateAllLines().TakeWhile(x => r.Peek() is not -1 and not '\n' and not '\r'))
            yield return r.EnumerateLine();
    }
}
