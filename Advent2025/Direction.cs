using System.Numerics;

namespace Advent2025;

public enum Direction : byte {
    North,
    East,
    South,
    West,
}

public static class DirectionExtensions {
    private const int MOD_MASK = 0x3;

    public static Direction LeftOf(this Direction direction) => (Direction)(((int)direction + MOD_MASK) & MOD_MASK);

    public static Direction RightOf(this Direction direction) => (Direction)(((int)direction + 1) & MOD_MASK);

    public static Direction Inverse(this Direction direction) => (Direction)(((int)direction + 2) & MOD_MASK);

    public static Vector2D<T> ToDirVector<T>(this Direction direction, bool northPositive = false) where T : INumber<T> {
        return direction switch {
            Direction.North when northPositive => new(T.Zero, T.One),
            Direction.North => new(T.Zero, -T.One),
            Direction.East => new(T.One, T.Zero),
            Direction.South when northPositive => new(T.Zero, -T.One),
            Direction.South => new(T.Zero, T.One),
            Direction.West => new(-T.One, T.Zero),
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };
    }

    public static Direction TurnByDegrees(this Direction direction, int degrees) {
        var turnCount = Math.DivRem(degrees, 90, out var remainder) & 3;
        if (remainder != 0)
            throw new Exception("Unable to convert degrees to turns");
        return Math.Abs(turnCount) switch {
            0 => direction,
            1 => turnCount > 0 ? direction.LeftOf() : direction.RightOf(),
            2 => direction.Inverse(),
            3 => turnCount < 0 ? direction.LeftOf() : direction.RightOf(),
            _ => throw new Exception("How did this happen?")
        };
    }
}