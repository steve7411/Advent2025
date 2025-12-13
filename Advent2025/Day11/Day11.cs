using System.Diagnostics;

namespace Advent2025.Day11;

internal sealed class Day11 : DayBase {
    private const int YOU = 0x65F5;
    private const int SVR = 0x4ED2;
    private const int OUT = 0x3EB4;
    private const int DAC = 0x1023;
    private const int FFT = 0x18D4;


    private readonly int youOutPathCount;
    private readonly long svrOutPathCount;

    public Day11() {
        Span<char> newLineDump = stackalloc char[Environment.NewLine.Length];
        Dictionary<short, List<short>> adj = [];
        using var reader = GetDataReader();
        while (!reader.EndOfStream) {
            var node = ReadNode(reader);
            var ch = reader.Read();
            Debug.Assert(ch == ':');
            adj[node] = ReadOutputs(reader);
            reader.Read(newLineDump);
        }

        youOutPathCount = GetYOUPathCount(adj);
        svrOutPathCount = GetSVRPathCount(adj, SVR << 2, []);
    }

    private static short ReadNode(StreamReader reader) => (short)((reader.Read() & 0x1F) << 10 | (reader.Read() & 0x1F) << 5 | (reader.Read() & 0x1F));

    private static List<short> ReadOutputs(StreamReader reader) {
        List<short> outputs = [];
        while (reader.Peek() is ' ') {
            reader.Read();
            outputs.Add(ReadNode(reader));
        }
        return outputs;
    }

    private static int GetYOUPathCount(Dictionary<short, List<short>> adj) {
        Queue<short> q = [];
        q.Enqueue(YOU);

        var count = 0;
        while (q.TryDequeue(out var curr)) {
            if (curr == OUT) {
                ++count;
                continue;
            }

            var neighbors = adj[curr];
            foreach (var neighbor in neighbors)
                q.Enqueue(neighbor);
        }
        return count;
    }

    private static long GetSVRPathCount(Dictionary<short, List<short>> adj, int state, Dictionary<int, long> memo) {
        if (memo.TryGetValue(state, out var cached))
            return cached;

        var node = state >>> 2;
        var requiredBits = state & 3;
        if (node == OUT)
            return requiredBits == 3 ? 1 : 0;
            
        requiredBits |= node == DAC ? 1 : 0;
        requiredBits |= node == FFT ? 2 : 0;
        var count = 0L;

        foreach (var neighbor in adj[(short)node])
            count += GetSVRPathCount(adj, neighbor << 2 | requiredBits, memo);
        return memo[state] = count;
    }

    public override object? Part1() {
        Print("There number of paths from YOU to OUT is: {0}", youOutPathCount);
        return Box<int>.Instance(youOutPathCount);
    }

    public override object? Part2() {
        Print("There number of valid paths from SVR to OUT is: {0}", svrOutPathCount);
        return Box<long>.Instance(svrOutPathCount);
    }
}
