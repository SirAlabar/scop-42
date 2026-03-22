using System;

namespace Scop.Math
{
    public struct Vec3
    {
        /* ── Fields ──────────────────────────────────────────────────────── */

        public float X;
        public float Y;
        public float Z;

        /* ── Constructor ─────────────────────────────────────────────────── */

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /* ── Static constants ────────────────────────────────────────────── */

        public static Vec3 Zero  => new Vec3(0f, 0f, 0f);
        public static Vec3 One   => new Vec3(1f, 1f, 1f);
        public static Vec3 UnitX => new Vec3(1f, 0f, 0f);
        public static Vec3 UnitY => new Vec3(0f, 1f, 0f);
        public static Vec3 UnitZ => new Vec3(0f, 0f, 1f);

        /* ── Arithmetic operators ────────────────────────────────────────── */

        public static Vec3 operator +(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vec3 operator -(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vec3 operator -(Vec3 v)
        {
            return new Vec3(-v.X, -v.Y, -v.Z);
        }

        public static Vec3 operator *(Vec3 v, float scalar)
        {
            return new Vec3(v.X * scalar, v.Y * scalar, v.Z * scalar);
        }

        public static Vec3 operator *(float scalar, Vec3 v)
        {
            return new Vec3(v.X * scalar, v.Y * scalar, v.Z * scalar);
        }

        public static Vec3 operator /(Vec3 v, float scalar)
        {
            return new Vec3(v.X / scalar, v.Y / scalar, v.Z / scalar);
        }

        /* ── Vector operations ───────────────────────────────────────────── */

        public float LengthSquared()
        {
            return (X * X) + (Y * Y) + (Z * Z);
        }

        public float Length()
        {
            return MathF.Sqrt(LengthSquared());
        }

        public Vec3 Normalized()
        {
            float len = Length();
            if (len < 1e-6f)
            {
                return Zero;
            }
            return this / len;
        }

        public static float Dot(Vec3 a, Vec3 b)
        {
            return (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
        }

        public static Vec3 Cross(Vec3 a, Vec3 b)
        {
            return new Vec3(
                (a.Y * b.Z) - (a.Z * b.Y),
                (a.Z * b.X) - (a.X * b.Z),
                (a.X * b.Y) - (a.Y * b.X)
            );
        }

        /* ── Utility ─────────────────────────────────────────────────────── */

        public override string ToString()
        {
            return $"Vec3({X}, {Y}, {Z})";
        }
    }
}