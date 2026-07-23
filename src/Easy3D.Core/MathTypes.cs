using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Easy3D.Core
{
    // ==================== Vector3 ====================
    
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector3 : IEquatable<Vector3>
    {
        public float X, Y, Z;

        public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }
        public Vector3(float value) : this(value, value, value) { }

        public static readonly Vector3 Zero = new(0, 0, 0);
        public static readonly Vector3 One = new(1, 1, 1);
        public static readonly Vector3 UnitX = new(1, 0, 0);
        public static readonly Vector3 UnitY = new(0, 1, 0);
        public static readonly Vector3 UnitZ = new(0, 0, 1);
        public static readonly Vector3 Up = new(0, 1, 0);
        public static readonly Vector3 Down = new(0, -1, 0);
        public static readonly Vector3 Left = new(-1, 0, 0);
        public static readonly Vector3 Right = new(1, 0, 0);
        public static readonly Vector3 Forward = new(0, 0, -1);
        public static readonly Vector3 Backward = new(0, 0, 1);

        public readonly float Length => MathF.Sqrt(X * X + Y * Y + Z * Z);
        public readonly float LengthSquared => X * X + Y * Y + Z * Z;

        public readonly Vector3 Normalized()
        {
            float len = Length;
            return len > 1e-6f ? this / len : Zero;
        }

        public readonly float Dot(Vector3 other) => X * other.X + Y * other.Y + Z * other.Z;
        
        public readonly Vector3 Cross(Vector3 other) => new(
            Y * other.Z - Z * other.Y,
            Z * other.X - X * other.Z,
            X * other.Y - Y * other.X
        );

        public readonly float DistanceTo(Vector3 other) => (this - other).Length;
        public readonly float DistanceSquaredTo(Vector3 other) => (this - other).LengthSquared;

        public readonly Vector3 Lerp(Vector3 target, float t) => this + (target - this) * Math.Clamp(t, 0, 1);
        
        public readonly Vector3 Abs() => new(MathF.Abs(X), MathF.Abs(Y), MathF.Abs(Z));
        public readonly Vector3 Min(Vector3 other) => new(MathF.Min(X, other.X), MathF.Min(Y, other.Y), MathF.Min(Z, other.Z));
        public readonly Vector3 Max(Vector3 other) => new(MathF.Max(X, other.X), MathF.Max(Y, other.Y), MathF.Max(Z, other.Z));
        public readonly Vector3 Clamp(Vector3 min, Vector3 max) => Max(min).Min(max);

        public readonly Vector3 RotateX(float angle)
        {
            float c = MathF.Cos(angle), s = MathF.Sin(angle);
            return new(X, Y * c - Z * s, Y * s + Z * c);
        }

        public readonly Vector3 RotateY(float angle)
        {
            float c = MathF.Cos(angle), s = MathF.Sin(angle);
            return new(X * c + Z * s, Y, -X * s + Z * c);
        }

        public readonly Vector3 RotateZ(float angle)
        {
            float c = MathF.Cos(angle), s = MathF.Sin(angle);
            return new(X * c - Y * s, X * s + Y * c, Z);
        }

        public readonly Vector3 Rotate(Vector3 euler) => RotateZ(euler.Z).RotateY(euler.Y).RotateX(euler.X);

        public readonly Vector3 Reflect(Vector3 normal) => this - 2 * Dot(normal) * normal;

        public readonly Vector3 Project(Vector3 onto)
        {
            float sqrMag = onto.LengthSquared;
            return sqrMag < 1e-6f ? Zero : onto * (Dot(onto) / sqrMag);
        }

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a.Lerp(b, t);
        public static float Dot(Vector3 a, Vector3 b) => a.Dot(b);
        public static Vector3 Cross(Vector3 a, Vector3 b) => a.Cross(b);
        public static float Distance(Vector3 a, Vector3 b) => a.DistanceTo(b);
        public static Vector3 Normalize(Vector3 v) => v.Normalized();
        public static Vector3 Min(Vector3 a, Vector3 b) => a.Min(b);
        public static Vector3 Max(Vector3 a, Vector3 b) => a.Max(b);

        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator -(Vector3 v) => new(-v.X, -v.Y, -v.Z);
        public static Vector3 operator *(Vector3 v, float s) => new(v.X * s, v.Y * s, v.Z * s);
        public static Vector3 operator *(float s, Vector3 v) => new(v.X * s, v.Y * s, v.Z * s);
        public static Vector3 operator *(Vector3 a, Vector3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        public static Vector3 operator /(Vector3 v, float s) => new(v.X / s, v.Y / s, v.Z / s);
        public static Vector3 operator /(Vector3 a, Vector3 b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);

        public static bool operator ==(Vector3 a, Vector3 b) => MathF.Abs(a.X - b.X) < 1e-6f && MathF.Abs(a.Y - b.Y) < 1e-6f && MathF.Abs(a.Z - b.Z) < 1e-6f;
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);

        public static implicit operator Vector3(System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);
        public static implicit operator System.Numerics.Vector3(Vector3 v) => new(v.X, v.Y, v.Z);

        public readonly bool Equals(Vector3 other) => this == other;
        public readonly override bool Equals(object? obj) => obj is Vector3 v && this == v;
        public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public readonly override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3})";

        public readonly (float X, float Y, float Z) ToTuple() => (X, Y, Z);
        public readonly float[] ToArray() => [X, Y, Z];
    }

    // ==================== Vector2 ====================
    
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2 : IEquatable<Vector2>
    {
        public float X, Y;

        public Vector2(float x, float y) { X = x; Y = y; }
        public Vector2(float value) : this(value, value) { }

        public static readonly Vector2 Zero = new(0, 0);
        public static readonly Vector2 One = new(1, 1);
        public static readonly Vector2 UnitX = new(1, 0);
        public static readonly Vector2 UnitY = new(0, 1);

        public readonly float Length => MathF.Sqrt(X * X + Y * Y);
        public readonly float LengthSquared => X * X + Y * Y;

        public readonly Vector2 Normalized()
        {
            float len = Length;
            return len > 1e-6f ? this / len : Zero;
        }

        public readonly float Dot(Vector2 other) => X * other.X + Y * other.Y;
        public readonly float DistanceTo(Vector2 other) => (this - other).Length;

        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator -(Vector2 v) => new(-v.X, -v.Y);
        public static Vector2 operator *(Vector2 v, float s) => new(v.X * s, v.Y * s);
        public static Vector2 operator *(float s, Vector2 v) => new(v.X * s, v.Y * s);
        public static Vector2 operator /(Vector2 v, float s) => new(v.X / s, v.Y / s);

        public static bool operator ==(Vector2 a, Vector2 b) => MathF.Abs(a.X - b.X) < 1e-6f && MathF.Abs(a.Y - b.Y) < 1e-6f;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);

        public readonly bool Equals(Vector2 other) => this == other;
        public readonly override bool Equals(object? obj) => obj is Vector2 v && this == v;
        public readonly override int GetHashCode() => HashCode.Combine(X, Y);
        public readonly override string ToString() => $"({X:F3}, {Y:F3})";
    }

    // ==================== Vector4 ====================
    
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector4
    {
        public float X, Y, Z, W;

        public Vector4(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }
        public Vector4(Vector3 v, float w) : this(v.X, v.Y, v.Z, w) { }

        public static readonly Vector4 Zero = new(0, 0, 0, 0);
        public static readonly Vector4 One = new(1, 1, 1, 1);

        public readonly Vector3 ToVector3() => W != 0 ? new(X / W, Y / W, Z / W) : new(X, Y, Z);

        public static Vector4 operator +(Vector4 a, Vector4 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
        public static Vector4 operator *(Vector4 v, float s) => new(v.X * s, v.Y * s, v.Z * s, v.W * s);
        public static Vector4 operator /(Vector4 v, float s) => new(v.X / s, v.Y / s, v.Z / s, v.W / s);

        public readonly float Length => MathF.Sqrt(X * X + Y * Y + Z * Z + W * W);
    }

    // ==================== Matrix4x4 ====================
    
    [StructLayout(LayoutKind.Sequential)]
    public struct Matrix4x4
    {
        public float M11, M12, M13, M14;
        public float M21, M22, M23, M24;
        public float M31, M32, M33, M34;
        public float M41, M42, M43, M44;

        public Matrix4x4(
            float m11, float m12, float m13, float m14,
            float m21, float m22, float m23, float m24,
            float m31, float m32, float m33, float m34,
            float m41, float m42, float m43, float m44)
        {
            M11 = m11; M12 = m12; M13 = m13; M14 = m14;
            M21 = m21; M22 = m22; M23 = m23; M24 = m24;
            M31 = m31; M32 = m32; M33 = m33; M34 = m34;
            M41 = m41; M42 = m42; M43 = m43; M44 = m44;
        }

        public static readonly Matrix4x4 Identity = new(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        );

        public static Matrix4x4 Translation(float x, float y, float z) => new(
            1, 0, 0, x,
            0, 1, 0, y,
            0, 0, 1, z,
            0, 0, 0, 1
        );

        public static Matrix4x4 Translation(Vector3 v) => Translation(v.X, v.Y, v.Z);

        public static Matrix4x4 Scale(float x, float y, float z) => new(
            x, 0, 0, 0,
            0, y, 0, 0,
            0, 0, z, 0,
            0, 0, 0, 1
        );

        public static Matrix4x4 Scale(Vector3 v) => Scale(v.X, v.Y, v.Z);
        public static Matrix4x4 Scale(float s) => Scale(s, s, s);

        public static Matrix4x4 RotationX(float angle)
        {
            float c = MathF.Cos(angle), s = MathF.Sin(angle);
            return new(1, 0, 0, 0, 0, c, -s, 0, 0, s, c, 0, 0, 0, 0, 1);
        }

        public static Matrix4x4 RotationY(float angle)
        {
            float c = MathF.Cos(angle), s = MathF.Sin(angle);
            return new(c, 0, s, 0, 0, 1, 0, 0, -s, 0, c, 0, 0, 0, 0, 1);
        }

        public static Matrix4x4 RotationZ(float angle)
        {
            float c = MathF.Cos(angle), s = MathF.Sin(angle);
            return new(c, -s, 0, 0, s, c, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        }

        public static Matrix4x4 Rotation(Vector3 euler) => RotationZ(euler.Z) * RotationY(euler.Y) * RotationX(euler.X);
        public static Matrix4x4 Rotation(float pitch, float yaw, float roll) => Rotation(new Vector3(pitch, yaw, roll));

        public static Matrix4x4 LookAt(Vector3 eye, Vector3 target, Vector3 up)
        {
            Vector3 z = (eye - target).Normalized();
            Vector3 x = up.Cross(z).Normalized();
            Vector3 y = z.Cross(x);
            return new(
                x.X, x.Y, x.Z, -x.Dot(eye),
                y.X, y.Y, y.Z, -y.Dot(eye),
                z.X, z.Y, z.Z, -z.Dot(eye),
                0, 0, 0, 1
            );
        }

        public static Matrix4x4 Perspective(float fovY, float aspect, float near, float far)
        {
            float t = MathF.Tan(fovY / 2);
            float range = far - near;
            return new(
                1 / (aspect * t), 0, 0, 0,
                0, 1 / t, 0, 0,
                0, 0, -(far + near) / range, -(2 * far * near) / range,
                0, 0, -1, 0
            );
        }

        public static Matrix4x4 Orthographic(float left, float right, float bottom, float top, float near, float far)
        {
            float w = right - left, h = top - bottom, d = far - near;
            return new(
                2 / w, 0, 0, -(right + left) / w,
                0, 2 / h, 0, -(top + bottom) / h,
                0, 0, -2 / d, -(far + near) / d,
                0, 0, 0, 1
            );
        }

        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b) => new(
            a.M11*b.M11 + a.M12*b.M21 + a.M13*b.M31 + a.M14*b.M41,
            a.M11*b.M12 + a.M12*b.M22 + a.M13*b.M32 + a.M14*b.M42,
            a.M11*b.M13 + a.M12*b.M23 + a.M13*b.M33 + a.M14*b.M43,
            a.M11*b.M14 + a.M12*b.M24 + a.M13*b.M34 + a.M14*b.M44,

            a.M21*b.M11 + a.M22*b.M21 + a.M23*b.M31 + a.M24*b.M41,
            a.M21*b.M12 + a.M22*b.M22 + a.M23*b.M32 + a.M24*b.M42,
            a.M21*b.M13 + a.M22*b.M23 + a.M23*b.M33 + a.M24*b.M43,
            a.M21*b.M14 + a.M22*b.M24 + a.M23*b.M34 + a.M24*b.M44,

            a.M31*b.M11 + a.M32*b.M21 + a.M33*b.M31 + a.M34*b.M41,
            a.M31*b.M12 + a.M32*b.M22 + a.M33*b.M32 + a.M34*b.M42,
            a.M31*b.M13 + a.M32*b.M23 + a.M33*b.M33 + a.M34*b.M43,
            a.M31*b.M14 + a.M32*b.M24 + a.M33*b.M34 + a.M34*b.M44,

            a.M41*b.M11 + a.M42*b.M21 + a.M43*b.M31 + a.M44*b.M41,
            a.M41*b.M12 + a.M42*b.M22 + a.M43*b.M32 + a.M44*b.M42,
            a.M41*b.M13 + a.M42*b.M23 + a.M43*b.M33 + a.M44*b.M43,
            a.M41*b.M14 + a.M42*b.M24 + a.M43*b.M34 + a.M44*b.M44
        );

        public readonly Vector4 Transform(Vector4 v) => new(
            M11*v.X + M12*v.Y + M13*v.Z + M14*v.W,
            M21*v.X + M22*v.Y + M23*v.Z + M24*v.W,
            M31*v.X + M32*v.Y + M33*v.Z + M34*v.W,
            M41*v.X + M42*v.Y + M43*v.Z + M44*v.W
        );

        public readonly Vector3 TransformPoint(Vector3 p)
        {
            var r = Transform(new Vector4(p, 1));
            return r.W != 0 ? new(r.X / r.W, r.Y / r.W, r.Z / r.W) : new(r.X, r.Y, r.Z);
        }

        public readonly Vector3 TransformDirection(Vector3 d) => new(
            M11*d.X + M12*d.Y + M13*d.Z,
            M21*d.X + M22*d.Y + M23*d.Z,
            M31*d.X + M32*d.Y + M33*d.Z
        );

        public readonly Matrix4x4 Transposed() => new(
            M11, M21, M31, M41,
            M12, M22, M32, M42,
            M13, M23, M33, M43,
            M14, M24, M34, M44
        );

        public readonly Matrix4x4 Inverse()
        {
            float det = Determinant();
            if (MathF.Abs(det) < 1e-10f) return Identity;
            float inv = 1f / det;

            return new Matrix4x4(
                (M22*(M33*M44-M34*M43) - M23*(M32*M44-M34*M42) + M24*(M32*M43-M33*M42)) * inv,
               -(M12*(M33*M44-M34*M43) - M13*(M32*M44-M34*M42) + M14*(M32*M43-M33*M42)) * inv,
                (M12*(M23*M44-M24*M43) - M13*(M22*M44-M24*M42) + M14*(M22*M43-M23*M42)) * inv,
               -(M12*(M23*M34-M24*M33) - M13*(M22*M34-M24*M32) + M14*(M22*M33-M23*M32)) * inv,

               -(M21*(M33*M44-M34*M43) - M23*(M31*M44-M34*M41) + M24*(M31*M43-M33*M41)) * inv,
                (M11*(M33*M44-M34*M43) - M13*(M31*M44-M34*M41) + M14*(M31*M43-M33*M41)) * inv,
               -(M11*(M23*M44-M24*M43) - M13*(M21*M44-M24*M41) + M14*(M21*M43-M23*M41)) * inv,
                (M11*(M23*M34-M24*M33) - M13*(M21*M34-M24*M31) + M14*(M21*M33-M23*M31)) * inv,

                (M21*(M32*M44-M34*M42) - M22*(M31*M44-M34*M41) + M24*(M31*M42-M32*M41)) * inv,
               -(M11*(M32*M44-M34*M42) - M12*(M31*M44-M34*M41) + M14*(M31*M42-M32*M41)) * inv,
                (M11*(M22*M44-M24*M42) - M12*(M21*M44-M24*M41) + M14*(M21*M42-M22*M41)) * inv,
               -(M11*(M22*M34-M24*M32) - M12*(M21*M34-M24*M31) + M14*(M21*M32-M22*M31)) * inv,

               -(M21*(M32*M43-M33*M42) - M22*(M31*M43-M33*M41) + M23*(M31*M42-M32*M41)) * inv,
                (M11*(M32*M43-M33*M42) - M12*(M31*M43-M33*M41) + M13*(M31*M42-M32*M41)) * inv,
               -(M11*(M22*M43-M23*M42) - M12*(M21*M43-M23*M41) + M13*(M21*M42-M22*M41)) * inv,
                (M11*(M22*M33-M23*M32) - M12*(M21*M33-M23*M31) + M13*(M21*M32-M22*M31)) * inv
            );
        }

        public readonly float Determinant() =>
            M11*(M22*(M33*M44-M34*M43) - M23*(M32*M44-M34*M42) + M24*(M32*M43-M33*M42)) -
            M12*(M21*(M33*M44-M34*M43) - M23*(M31*M44-M34*M41) + M24*(M31*M43-M33*M41)) +
            M13*(M21*(M32*M44-M34*M42) - M22*(M31*M44-M34*M41) + M24*(M31*M42-M32*M41)) -
            M14*(M21*(M32*M43-M33*M42) - M22*(M31*M43-M33*M41) + M23*(M31*M42-M32*M41));

        public readonly float[] ToArray() => [
            M11, M12, M13, M14,
            M21, M22, M23, M24,
            M31, M32, M33, M34,
            M41, M42, M43, M44
        ];
    }

    // ==================== Quaternion ====================
    
    [StructLayout(LayoutKind.Sequential)]
    public struct Quaternion
    {
        public float X, Y, Z, W;

        public Quaternion(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }

        public static readonly Quaternion Identity = new(0, 0, 0, 1);

        public readonly float Length => MathF.Sqrt(X*X + Y*Y + Z*Z + W*W);

        public readonly Quaternion Normalized()
        {
            float len = Length;
            return len > 1e-6f ? new(X/len, Y/len, Z/len, W/len) : Identity;
        }

        public static Quaternion FromEuler(Vector3 euler)
        {
            float cx = MathF.Cos(euler.X/2), sx = MathF.Sin(euler.X/2);
            float cy = MathF.Cos(euler.Y/2), sy = MathF.Sin(euler.Y/2);
            float cz = MathF.Cos(euler.Z/2), sz = MathF.Sin(euler.Z/2);
            return new(
                sx*cy*cz - cx*sy*sz,
                cx*sy*cz + sx*cy*sz,
                cx*cy*sz - sx*sy*cz,
                cx*cy*cz + sx*sy*sz
            ).Normalized();
        }

        public readonly Vector3 ToEuler()
        {
            float sinr = 2*(W*X + Y*Z);
            float cosr = 1 - 2*(X*X + Y*Y);
            float x = MathF.Atan2(sinr, cosr);

            float sinp = 2*(W*Y - Z*X);
            float y = MathF.Abs(sinp) >= 1 ? MathF.CopySign(MathF.PI/2, sinp) : MathF.Asin(sinp);

            float siny = 2*(W*Z + X*Y);
            float cosy = 1 - 2*(Y*Y + Z*Z);
            float z = MathF.Atan2(siny, cosy);

            return new(x, y, z);
        }

        public static Quaternion operator *(Quaternion a, Quaternion b) => new(
            a.W*b.X + a.X*b.W + a.Y*b.Z - a.Z*b.Y,
            a.W*b.Y - a.X*b.Z + a.Y*b.W + a.Z*b.X,
            a.W*b.Z + a.X*b.Y - a.Y*b.X + a.Z*b.W,
            a.W*b.W - a.X*b.X - a.Y*b.Y - a.Z*b.Z
        );

        public readonly Vector3 RotateVector(Vector3 v)
        {
            var qv = new Vector3(X, Y, Z);
            var uv = qv.Cross(v);
            var uuv = qv.Cross(uv);
            return v + 2*(W*uv + uuv);
        }

        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
        {
            float dot = a.X*b.X + a.Y*b.Y + a.Z*b.Z + a.W*b.W;
            if (dot < 0) { b = new(-b.X, -b.Y, -b.Z, -b.W); dot = -dot; }
            if (dot > 0.9995f) return new(
                a.X + t*(b.X - a.X), a.Y + t*(b.Y - a.Y),
                a.Z + t*(b.Z - a.Z), a.W + t*(b.W - a.W)
            ).Normalized();
            float theta = MathF.Acos(dot);
            float sinT = MathF.Sin(theta);
            float wa = MathF.Sin((1-t)*theta) / sinT;
            float wb = MathF.Sin(t*theta) / sinT;
            return new(
                a.X*wa + b.X*wb, a.Y*wa + b.Y*wb,
                a.Z*wa + b.Z*wb, a.W*wa + b.W*wb
            );
        }
    }
}
