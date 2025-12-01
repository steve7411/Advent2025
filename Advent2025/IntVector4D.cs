using System;
using System.Collections;
using System.Collections.Generic;

namespace Advent2025;

public struct IntVector4D : IEnumerable<int> {
    public int x;
    public int y;
    public int z;
    public int w;

    public static IntVector4D Zero => new();

    public int this[int index] {
        get {
            return index switch
            {
                0 => x,
                1 => y,
                2 => z,
                3 => w,
                _ => throw new IndexOutOfRangeException()
            };
        }

        set {
            switch (index) {
                case 0:
                    x = value;
                    break;
                case 1:
                    y = value;
                    break;
                case 2:
                    z = value;
                    break;
                case 3:
                    w = value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }

    public IntVector4D(int x, int y, int z, int w) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    public override bool Equals(object? obj) {
        return obj is IntVector4D other &&
                x == other.x &&
                y == other.y &&
                z == other.z &&
                w == other.w;
    }

    public override int GetHashCode() => HashCode.Combine(x, y, z, w);

    public void Deconstruct(out int x, out int y, out int z, out int w) {
        x = this.x;
        y = this.y;
        z = this.z;
        w = this.w;
    }

    public static implicit operator (int x, int y, int z, int w)(in IntVector4D value) => (value.x, value.y, value.z, value.w);
    public static implicit operator IntVector4D(in (int x, int y, int z, int w) value) => new(value.x, value.y, value.z, value.w);
    public static bool operator ==(in IntVector4D a, in IntVector4D b) => a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
    public static bool operator !=(in IntVector4D a, in IntVector4D b) => !(a == b);
    public static IntVector4D operator -(in IntVector4D a) => new(-a.x, -a.y, -a.z, -a.w);
    public static IntVector4D operator -(in IntVector4D a, in IntVector4D b) => new(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
    public static IntVector4D operator +(in IntVector4D a, in IntVector4D b) => new(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
    public static float operator *(in IntVector4D a, in IntVector4D b) => a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;

    public int ManhattanDistanceTo(in IntVector4D b) {
        return Math.Abs(x - b.x) + Math.Abs(y - b.y) + Math.Abs(z - b.z) + Math.Abs(w - b.w);
    }

    public float DistanceTo(in IntVector4D b) {
        return (this - b).Magnitude();
    }

    public float Magnitude() {
        return (float)Math.Sqrt(this * this);
    }

    public (float x, float y, float z, float w) Normalize() {
        float mag = Magnitude();
        return (x / mag, y / mag, z / mag, w / mag);
    }

    public override string ToString() {
        return $"({x}, {y}, {z}, {w})";
    }

    public IEnumerator<int> GetEnumerator() {
        yield return x;
        yield return y;
        yield return z;
        yield return w;
    }

    IEnumerator IEnumerable.GetEnumerator() {
        yield return x;
        yield return y;
        yield return z;
        yield return w;
    }
}
