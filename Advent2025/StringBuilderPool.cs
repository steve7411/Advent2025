using System.Collections.Concurrent;
using System.Text;

namespace Advent2025;

public class StringBuilderPool {
    const int INITIAL_CAPACITY = 1;
    const int BUILDER_INITIAL_CAPACITY = 1000;

    private readonly ConcurrentBag<StringBuilder> stringBuilders =
        new(Enumerable.Range(0, INITIAL_CAPACITY).Select(_ => new StringBuilder(BUILDER_INITIAL_CAPACITY)));

    protected StringBuilderPool() { }

    public static StringBuilderPool Create() => new();

    public StringBuilder Rent() =>
        stringBuilders.TryTake(out var sb) ? sb : new(BUILDER_INITIAL_CAPACITY);

    public void Return(StringBuilder sb) => stringBuilders.Add(sb.Clear());

    public string ReturnAndGetString(StringBuilder sb) {
        var str = sb.ToString();
        stringBuilders.Add(sb.Clear());
        return str;
    }
}

