namespace Scop.Math
{
    public struct Vec4
    {
        /* ── Fields ──────────────────────────────────────────────────────── */

        public float X;
        public float Y;
        public float Z;
        public float W;

        /* ── Constructors ────────────────────────────────────────────────── */

        public Vec4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vec4(Vec3 v, float w)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
            W = w;
        }

        /* ── Properties ──────────────────────────────────────────────────── */

        public Vec3 Xyz => new Vec3(X, Y, Z);

        /* ── Arithmetic operators ────────────────────────────────────────── */

        public static Vec4 operator +(Vec4 a, Vec4 b)
        {
            return new Vec4(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
        }

        public static Vec4 operator *(Vec4 v, float scalar)
        {
            return new Vec4(v.X * scalar, v.Y * scalar, v.Z * scalar, v.W * scalar);
        }

        public static Vec4 operator *(float scalar, Vec4 v)
        {
            return new Vec4(v.X * scalar, v.Y * scalar, v.Z * scalar, v.W * scalar);
        }

        /* ── Utility ─────────────────────────────────────────────────────── */

        public override string ToString()
        {
            return $"Vec4({X}, {Y}, {Z}, {W})";
        }
    }
}