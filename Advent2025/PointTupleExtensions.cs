using System.Numerics;

namespace Advent2025;
public static class PointTupleExtensions {
    public static (T x, T y) Add<T>(this (T x, T y) lhs, (T x, T y) rhs) where T : INumber<T> => (lhs.x + rhs.x, lhs.y + rhs.y);

    public static (T x, T y) Subtract<T>(this (T x, T y) lhs, (T x, T y) rhs) where T : INumber<T> => (lhs.x - rhs.x, lhs.y - rhs.y);
    public static (T x, T y) Subtract<T>(this (T x, T y) a, Vector2D<T> b) where T : INumber<T> => (a.x - b.x, a.y - b.y);

    public static (T x, T y) Multiply<T>(this (T x, T y) a, T scalar) where T : INumber<T> => (a.x * scalar, a.y * scalar);
    public static (T x, T y) Multiply<T>(this (T x, T y) lhs, (T x, T y) rhs) where T : INumber<T> => (lhs.x * rhs.x, lhs.y * rhs.y);

    public static Vector2D<int> RoundToInt<T>(this (T x, T y) a) where T : IFloatingPoint<T> => new(int.CreateTruncating(T.Round(a.x)), int.CreateTruncating(T.Round(a.y)));

    public static (int x, int y) TruncateToInt<T>(this (T x, T y) a) where T : IFloatingPoint<T> => (int.CreateTruncating(T.Truncate(a.x)), int.CreateTruncating(T.Truncate(a.y)));
    public static Vector2D<int> TruncateToIntVec<T>(this (T x, T y) a) where T : IFloatingPoint<T> => new(int.CreateTruncating(T.Truncate(a.x)), int.CreateTruncating(T.Truncate(a.y)));

    public static (T x, T y) Abs<T>(this (T x, T y) a) where T : INumber<T> => (T.Abs(a.x), T.Abs(a.y));

    public static T Dot<T>(this (T x, T y) lhs, (T x, T y) rhs) where T : INumber<T> => lhs.x * rhs.x + lhs.y * rhs.y;

    public static (T x, T y, T z) Add<T>(this (T x, T y, T z) lhs, (T x, T y, T z) rhs) where T : INumber<T> => (lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
    
    public static (T x, T y, T z) Subtract<T>(this (T x, T y, T z) lhs, (T x, T y, T z) rhs) where T : INumber<T> => (lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);

    public static T Dot<T>(this (T x, T y, T z) lhs, (T x, T y, T z) rhs) where T : INumber<T> => lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;

    public static (T x, T y) Swap<T>(this (T x, T y) a) => (a.y, a.x);

    public static T ManhattanDistanceTo<T>(this (T x, T y) a, (T x, T y) b) where T : INumber<T> => T.Abs(a.x - b.x) + T.Abs(a.y - b.y);
}