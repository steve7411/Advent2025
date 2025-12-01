using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Advent2025;

public readonly struct Rect2D<T> where T: INumber<T> {
    [Flags]
    public enum SpatialRelation {
        Inside       = 0,
        GreaterThanY = 0b1,
        GreaterThanX = 0b10,
        LessThanY    = 0b100,
        LessThanX    = 0b1000,
    };

    public readonly Vector2D<T> size;
    public readonly Vector2D<T> minPoint;
    public readonly Vector2D<T> maxPoint;

    private Rect2D(Vector2D<T> minPoint, Vector2D<T> maxPoint, Vector2D<T> size) {
        this.minPoint = minPoint;
        this.maxPoint = maxPoint;
        this.size = size;
    }

    public Rect2D(T x1, T x2, T y1, T y2) {
        var start = new Vector2D<T>(x1, y1);
        var end = new Vector2D<T>(x2, y2);
        var size = end - start;
        (minPoint, maxPoint) = CalculateMinAndMax(GetStandardizedPoint(start, size), size);
        this.size = size.Abs();
    }

    private static (Vector2D<T> point, Vector2D<T> size) CalculateMinAndMax(Vector2D<T> point, Vector2D<T> size) {
        var end = point + size;
        return (new(T.Min(point.x, end.x), T.Min(point.y, end.y)),
            new(T.Max(point.x, end.x), T.Max(point.y, end.y)));
    }

    private static Vector2D<T> GetStandardizedPoint(Vector2D<T> point, Vector2D<T> size) =>
        new(size.x < T.Zero ? point.x + size.x : point.x, size.y < T.Zero ? point.y + size.y : point.y);


    public static Rect2D<T> From(Vector2D<T> point, Vector2D<T> size) {
        var (minPoint, maxPoint) = CalculateMinAndMax(point, size);
        return new(minPoint, maxPoint, size);
    }

    public void Deconstruct(out Vector2D<T> point, out Vector2D<T> size) {
        point = this.minPoint;
        size = this.size;
    }

    public static implicit operator Rect2D<T>((Vector2D<T> point, Vector2D<T> size) tuple) => From(tuple.point, tuple.size);
    public static implicit operator (Vector2D<T> point, Vector2D<T> size)(Rect2D<T> rect) => (rect.minPoint, rect.size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Vector2D<T> point) =>
        point.x >= minPoint.x && point.x <= maxPoint.x && point.y >= minPoint.y && point.y <= maxPoint.y;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public SpatialRelation GetRelationTo(Vector2D<T> point) {
        var horizontalReltaion = (point.x < minPoint.x ? SpatialRelation.LessThanX : 0) | (point.x > maxPoint.x ? SpatialRelation.GreaterThanX : 0);
        var verticalRelation = (point.y < minPoint.y ? SpatialRelation.LessThanY : 0) | (point.y > maxPoint.y ? SpatialRelation.GreaterThanY : 0);
        return horizontalReltaion | verticalRelation;
    }

    public override int GetHashCode() => HashCode.Combine(minPoint, maxPoint);
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Rect2D<T> other && this == other;

    public static bool operator !=(in Rect2D<T> a, in Rect2D<T> b) =>
        a.minPoint != b.minPoint || a.maxPoint != b.maxPoint;

    public static bool operator ==(in Rect2D<T> a, in Rect2D<T> b) =>
        a.minPoint == b.minPoint && a.maxPoint == b.maxPoint;

    public override string ToString() => $"{{min: {minPoint}, max: {maxPoint}}}";
}
