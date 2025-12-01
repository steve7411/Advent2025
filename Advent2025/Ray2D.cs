using System.Numerics;

namespace Advent2025;

internal readonly struct Ray2D<T>(Vector2D<T> origin, Vector2D<T> direction) where T : INumber<T> {
    public readonly Vector2D<T> origin = origin;
    public readonly Vector2D<T> direction = direction;
}
