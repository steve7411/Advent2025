using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Advent2025;

public readonly struct Vector3D<T> : IEnumerable<T>, IEquatable<Vector3D<T>> where T : INumber<T> {
    public readonly T x;
    public readonly T y;
    public readonly T z;

    public static Vector3D<T> Zero => default;
    public static Vector3D<T> One => new(T.One, T.One, T.One);

    public T this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => Unsafe.Add(ref Unsafe.AsRef(in x), index);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        init => Unsafe.Add(ref Unsafe.AsRef(in x), index) = value;
    }

    public readonly T X { init => x = value; }
    public readonly T Y { init => y = value; }
    public readonly T Z { init => z = value; }

    public Vector3D(T x, T y, T z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public bool Equals(Vector3D<T> other) => x == other.x && y == other.y && z == other.z;
    public override bool Equals([NotNullWhen(true)] object? obj) {
        return obj is Vector3D<T> other &&
                x == other.x &&
                y == other.y &&
                z == other.z;
    }

    public override int GetHashCode() => HashCode.Combine(x, y, z);

    public void Deconstruct(out T x, out T y, out T z) {
        x = this.x;
        y = this.y;
        z = this.z;
    }

    public static implicit operator Vector2D<T>(in Vector3D<T> value) => new(value.x, value.y);
    public static implicit operator (T x, T y, T z)(in Vector3D<T> value) => (value.x, value.y, value.z);
    public static implicit operator Vector3D<T>(in (T x, T y, T z) value) => new(value.x, value.y, value.z);
    public static bool operator ==(in Vector3D<T> a, in Vector3D<T> b) => a.x == b.x && a.y == b.y && a.z == b.z;
    public static bool operator !=(in Vector3D<T> a, in Vector3D<T> b) => !(a == b);
    public static Vector3D<T> operator -(in Vector3D<T> a) => new(-a.x, -a.y, -a.z);
    public static Vector3D<T> operator -(in Vector3D<T> a, in Vector3D<T> b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
    public static Vector3D<T> operator +(in Vector3D<T> a, in Vector3D<T> b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
    public static T operator *(in Vector3D<T> a, in Vector3D<T> b) => Dot(a, b);
    public static Vector3D<T> operator *(in Vector3D<T> a, T scale) => new(a.x * scale, a.y * scale, a.z * scale);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Dot(in Vector3D<T> a, in Vector3D<T> b) =>
        a.x * b.x + a.y * b.y + a.z * b.z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ManhattanDistanceTo(in Vector3D<T> b) =>
        T.Abs(x - b.x) + T.Abs(y - b.y) + +T.Abs(z - b.z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3D<T> Sign() => new(T.CreateTruncating(T.Sign(x)), T.CreateTruncating(T.Sign(y)), T.CreateTruncating(T.Sign(z)));

    public static Vector3D<T> Cross(Vector3D<T> a, Vector3D<T> b) {
        return new(
            a.y * b.z - b.y * a.z,
            -(a.x * b.z - b.x * a.z),
            a.x * b.y - b.x * a.y);
    }

    public TRes SquaredDistanceTo<TRes>(in Vector3D<T> other) where TRes : INumberBase<TRes> {
        var dx = TRes.CreateTruncating(x - other.x);
        var dy = TRes.CreateTruncating(y - other.y);
        var dz = TRes.CreateTruncating(z - other.z);
        return dx * dx + dy * dy + dz * dz;
    }

    public TFloat DistanceTo<TFloat>(in Vector3D<T> b) where TFloat : IRootFunctions<TFloat> {
        var delta = this - b;
        return TFloat.Sqrt(TFloat.CreateTruncating(delta * delta));
    }

    public TFloat Magnitude<TFloat>() where TFloat : IRootFunctions<TFloat> => TFloat.Sqrt(TFloat.CreateTruncating(this * this));

    public Vector3D<TFloat> Normalize<TFloat>() where TFloat : IFloatingPoint<TFloat>, IRootFunctions<TFloat> {
        var mag = Magnitude<TFloat>();
        return new(TFloat.CreateTruncating(x) / mag, TFloat.CreateTruncating(y) / mag, TFloat.CreateTruncating(z) / mag);
    }

    public override string ToString() => $"({x}, {y}, {z})";

    public IEnumerator<T> GetEnumerator() {
        yield return x;
        yield return y;
        yield return z;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
