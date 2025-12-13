using System.Diagnostics;

namespace Advent2025.Day09;

using Vec = Vector2D<int>;
using AxisAlignedLine = (int common, int min, int max);

internal sealed class Day09 : DayBase {
    const int SIZE = 496;

    private static readonly Vec[] points = new Vec[SIZE];
    private static readonly AxisAlignedLine[] lines = new AxisAlignedLine[SIZE];

    private readonly int horizontalToVerticalPartition;
    private readonly long maxArea;
    private readonly long maxInternalArea;

    public Day09() {
        using var reader = GetDataReader();
        var vertWrite = lines.Length - 1;
        for (var write = 0; !reader.EndOfStream; ++write)
            points[write] = new(reader.ReadNextInt(), reader.ReadNextInt());
        ValidateBorderAssumption(points);

        Vec prev = points[^1];
        for (var i = 0; i < points.Length; ++i) {
            var p = points[i];
            Debug.Assert(vertWrite >= horizontalToVerticalPartition);
            if (prev.x == p.x)
                lines[vertWrite--] = CreateAxisAligned(p.x, prev.y, p.y);
            else
                lines[horizontalToVerticalPartition++] = CreateAxisAligned(p.y, prev.x, p.x);
            prev = p;
        }
        Debug.Assert(lines.All(static l => l.min != l.max));

        var horiz = lines.AsMemory(0, horizontalToVerticalPartition);
        var vert = lines.AsMemory(horizontalToVerticalPartition);
        horiz.Span.Sort();
        vert.Span.Sort();

        var localMaxArea = 0L;
        var localMaxInternalArea = 0L;
        Lock lck = new();

        Parallel.For(0, points.Length, i => {
            var a = points[i];
            var horizSpan = horiz.Span;
            var vertSpan = vert.Span;
            for (var j = i + 1; j < points.Length; ++j) {
                var b = points[j];
                var area = GetArea(a, b);
                if (area > localMaxArea) {
                    lock (lck) {
                        if (area > localMaxArea)
                            localMaxArea = area;
                    }
                }

                if (area > localMaxInternalArea && !HasIntersection(a, b, horizSpan, vertSpan)) {
                    lock (lck) {
                        if (area > localMaxInternalArea)
                            localMaxInternalArea = area;
                    }
                }
            }
        });
        maxArea = localMaxArea;
        maxInternalArea = localMaxInternalArea;
    }

    static long GetArea(in Vec a, in Vec b) => (long)(Math.Abs(a.x - b.x) + 1) * (Math.Abs(a.y - b.y) + 1);

    static AxisAlignedLine CreateAxisAligned(int val, int a, int b) => a < b ? (val, a, b) : (val, b, a);

    private static bool HasIntersection(in Vec cornerA, in Vec cornerB, ReadOnlySpan<AxisAlignedLine> horiz, ReadOnlySpan<AxisAlignedLine> vert) {
        Vec xDelta = cornerA.x < cornerB.x ? new(cornerA.x, cornerB.x) : new(cornerB.x, cornerA.x);
        Vec yDelta = cornerA.y < cornerB.y ? new(cornerA.y, cornerB.y) : new(cornerB.y, cornerA.y);
        return HasIntersection(yDelta, xDelta, horiz) || HasIntersection(xDelta, yDelta, vert);
    }

    private static bool HasIntersection(Vec delta, Vec perpendicularDelta, ReadOnlySpan<AxisAlignedLine> perpendiculars) {
        var idx = perpendiculars.BinarySearch((delta.x + 1, 0, 0));
        idx ^= idx >> 31;
        for (; idx < perpendiculars.Length; ++idx) {
            var (common, min, max) = perpendiculars[idx];
            if (common >= delta.y)
                break;
            if (min < perpendicularDelta.y & max > perpendicularDelta.x)
                return true;
        }
        return false;
    }

    private static bool Overlaps(ref (Vec a, Vec b) a, ref (Vec a, Vec b) b) {
        var bLeft = Math.Min(b.a.x, b.b.x);
        var bRight = Math.Max(b.a.x, b.b.x);
        var bTop = Math.Min(b.a.y, b.b.y);
        var bBottom = Math.Max(b.a.y, b.b.y);

        var aLeft = Math.Min(a.a.x, a.b.x);
        var aRight = Math.Max(a.a.x, a.b.x);
        var aTop = Math.Min(a.a.y, a.b.y);
        var aBottom = Math.Max(a.a.y, a.b.y);

        return aRight > bLeft & aLeft < bRight & aBottom > bTop & aTop < bBottom;
    }

    [Conditional("DEBUG")]
    static void ValidateBorderAssumption(Span<Vec> points) {
        var prev = points[^1];
        HashSet<Vec> pointSet = new(points.Length);
        HashSet<int> horizSet = new(points.Length);
        HashSet<int> vertSet = new(points.Length);

        for (var i = 0; i < points.Length; ++i) {
            var curr = points[i];
            Debug.Assert(pointSet.Add(curr));
            Debug.Assert(curr.x == prev.x | curr.y == prev.y);

            var line = (prev, curr);
            var s = prev.x == curr.x ? vertSet : horizSet;
            Debug.Assert(s.Add(prev.x == curr.x ? curr.y : curr.x));
            for (var j = i + 1; j < points.Length - 1; ++j) {
                var nextLine = (points[j], points[j + 1]);
                Debug.Assert(!Overlaps(ref line, ref nextLine));
            }
            prev = curr;
        }
    }

    public override object? Part1() {
        Print("The max area possible is: {0}", maxArea);
        return Box<long>.Instance(maxArea);
    }

    public override object? Part2() {
        Print("The max internal area possible is: {0}", maxInternalArea);
        return Box<long>.Instance(maxInternalArea);
    }
}
