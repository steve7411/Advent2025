using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Advent2025;

[DebuggerDisplay("(x: {x}, y: {y})")]
[StructLayout(LayoutKind.Sequential)]
public readonly struct Vector2D<T> : IEnumerable<T>, IEquatable<Vector2D<T>> where T : INumber<T> {
    public readonly T x;
    public readonly T y;

    public static Vector2D<T> Zero => new(T.Zero, T.Zero);
    public static Vector2D<T> One => new(T.One, T.One);

    public T this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => Unsafe.Add(ref Unsafe.AsRef(in x), index);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        init => Unsafe.Add(ref Unsafe.AsRef(in x), index) = value;
    }

    public readonly T X { init => x = value; }
    public readonly T Y { init => y = value; }

    public Vector2D(T x, T y) {
        this.x = x;
        this.y = y;
    }

    public bool Equals(Vector2D<T> other) => x == other.x && y == other.y;

    public readonly override bool Equals(object? obj) {
        return obj is Vector2D<T> other &&
                x == other.x &&
                y == other.y;
    }

    public readonly override int GetHashCode() => HashCode.Combine(x, y);

    public readonly void Deconstruct(out T x, out T y) {
        x = this.x;
        y = this.y;
    }

    public static implicit operator Vector3D<T>(in Vector2D<T> value) => new(value.x, value.y, T.Zero);
    public static implicit operator (T x, T y)(in Vector2D<T> value) => (value.x, value.y);
    public static implicit operator Vector2D<T>(in (T x, T y) value) => new(value.x, value.y);
    public static bool operator ==(in Vector2D<T> a, in Vector2D<T> b) => a.x == b.x && a.y == b.y;
    public static bool operator !=(in Vector2D<T> a, in Vector2D<T> b) => !(a == b);
    public static Vector2D<T> operator -(in Vector2D<T> a) => new(-a.x, -a.y);
    public static Vector2D<T> operator -(in Vector2D<T> a, in Vector2D<T> b) => new(a.x - b.x, a.y - b.y);
    public static Vector2D<float> operator -(in Vector2D<T> a, in Vector2D<float> b) => Subtract(a, b);
    public static Vector2D<double> operator -(in Vector2D<T> a, in Vector2D<double> b) => Subtract(a, b);
    public static Vector2D<T> operator +(in Vector2D<T> a, in Vector2D<T> b) => new(a.x + b.x, a.y + b.y);
    public static Vector2D<T> operator /(in Vector2D<T> a, in Vector2D<T> b) => new(a.x / b.x, a.y / b.y);
    public static Vector2D<T> operator /(in Vector2D<T> a, T scalar) => new(a.x / scalar, a.y / scalar);
    public static double operator *(in Vector2D<T> a, in Vector2D<T> b) => Dot<double>(a, b);
    public static Vector2D<T> operator *(in Vector2D<T> a, T scalar) => new(a.x * scalar, a.y * scalar);
    public static (float x, float y) operator *(in Vector2D<T> a, float scalar) => Scale(a, scalar);
    public static Vector2D<T> operator %(in Vector2D<T> a, in Vector2D<T> b) => new(a.x % b.x, a.y % b.y);
    public static Vector2D<T> operator %(in Vector2D<T> a, T scalar) => new(a.x % scalar, a.y % scalar);

    private static Vector2D<TFloat> Subtract<TFloat>(in Vector2D<T> a, in Vector2D<TFloat> b) where TFloat : IFloatingPoint<TFloat> => new(TFloat.CreateChecked(a.x) - b.x, TFloat.CreateChecked(a.y) - b.y);
    private static Vector2D<TFloat> Scale<TFloat>(in Vector2D<T> a, TFloat scalar) where TFloat : IFloatingPoint<TFloat> => new(TFloat.CreateChecked(a.x) * scalar, TFloat.CreateChecked(a.y) * scalar);
    private static TFloat Dot<TFloat>(in Vector2D<T> a, in Vector2D<T> b) where TFloat : IFloatingPoint<TFloat> => TFloat.CreateChecked(a.x * b.x + a.y * b.y);

    public static T Cross(in Vector2D<T> a, in Vector2D<T> b) => a.x * b.y - a.y * b.x;

    public Vector2D<T> Abs() => new(T.Abs(x), T.Abs(y));
    
    public T Min() => T.Min(x, y);
    public T Max() => T.Max(x, y);

    public T ManhattanDistanceTo(in Vector2D<T> b) => T.Abs(x - b.x) + T.Abs(y - b.y);

    public bool IsColinearWith(in Vector2D<T> b, in Vector2D<T> c) =>
        double.Abs(0.5 * double.CreateChecked(x * (b.y - c.y) + b.x * (c.y - y) + c.x * (y - b.y))) < 0.00001;

    public TFloat DistanceTo<TFloat>(in Vector2D<T> b) where TFloat : IFloatingPoint<TFloat>, IRootFunctions<TFloat> => (this - b).Magnitude<TFloat>();

    public Vector2D<T> Clamp(T min, T max) => new(T.Clamp(x, min, max), T.Clamp(y, min, max));

    public TFloat Magnitude<TFloat>() where TFloat : IFloatingPoint<TFloat>, IRootFunctions<TFloat> => TFloat.Sqrt(Dot<TFloat>(this, this));

    public Vector2D<TFloat> Normalize<TFloat>() where TFloat : IFloatingPoint<TFloat>, IRootFunctions<TFloat> {
        var mag = Magnitude<TFloat>();
        return (TFloat.CreateChecked(x) / mag, TFloat.CreateChecked(y) / mag);
    }

    public override string ToString() => $"({x}, {y})";

    public Vector2D<T> RotateAroundOrigin(TurnDirection turn) {
        return turn switch {
            TurnDirection.Left => new(-y, x),
            TurnDirection.Right => new(y, -x),
            TurnDirection.Inverse => -this,
            _ => throw new NotImplementedException(),
        };
    }

    public Vector2D<TFloat> RotateAround<TFloat>(Vector2D<TFloat> rotateAround, TFloat degrees) where TFloat : IFloatingPoint<TFloat>, ITrigonometricFunctions<TFloat> {
        var radians = TFloat.DegreesToRadians(degrees);
        var cosine = TFloat.Cos(radians);
        var sine = TFloat.Sin(radians);
        var translated = Subtract(this, rotateAround);
        var rotatedX = translated.x * cosine - translated.y * sine;
        var rotatedY = translated.y * cosine + translated.x * sine;
        return new(rotatedX + rotateAround.x, rotatedY + rotateAround.y);
    }

    public IEnumerator<T> GetEnumerator() {
        yield return x;
        yield return y;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public Vector2D<TFloat> ScaleAroundFloat<TFloat>(in Vector2D<TFloat> center, in Vector2D<TFloat> scale) where TFloat : IFloatingPoint<TFloat> =>
        new((TFloat.CreateChecked(x) - center.x) * scale.x + center.x, (TFloat.CreateChecked(x) - center.y) * scale.y + center.y);
}